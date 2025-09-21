using System;
using UnityEngine;
using UnityEngine.Events;

public class FetchNeedManager : MonoBehaviour
{
    public static FetchNeedManager Instance { get; private set; }

    [Header("Fetch (0..100)")]
    [Range(0,100)] public float fetch = 100f;

    [Header("Decay per minute")]
    public float fetchDrainPerMinute = 1.0f;

    [Header("Pause at 100% (minutes)")]
    public float fetchFullPauseMinutes = 20f;

    [Serializable] public class FloatEvent : UnityEvent<float> {}
    public FloatEvent OnFetchChanged = new FloatEvent();
    public UnityEvent  OnFetchHit50  = new UnityEvent();

    const string KEY_FETCH  = "fetch_need_value";
    const string KEY_LAST   = "fetch_need_lastTickUtc";
    const float  EPS        = 0.0001f;

    DateTime _lastTickUtc;
    DateTime _pauseUntilUtc = DateTime.MinValue;

    bool _hasPersist;
    bool _notified50; // true once notified at/below 50; re-arms when rising >50

    void Awake()
    {
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        _hasPersist = PlayerPrefs.HasKey(KEY_FETCH);
        if (_hasPersist) fetch = PlayerPrefs.GetFloat(KEY_FETCH, fetch);

        if (PlayerPrefs.HasKey(KEY_LAST) && DateTime.TryParse(PlayerPrefs.GetString(KEY_LAST), out var saved))
            _lastTickUtc = saved;
        else
            _lastTickUtc = DateTime.UtcNow;

        _notified50 = (fetch <= 50f);
        Tick(false); // catch-up once
    }

    void Update() => Tick(true);
    void OnApplicationPause(bool p){ if (p) Persist(); else { _lastTickUtc = DateTime.UtcNow; Tick(false);} }
    void OnApplicationQuit() => Persist();

    public bool IsFetchFull() => fetch >= 99.999f;

    public bool InitializeFetchIfUnset(float v)
    {
        if (_hasPersist) return false;
        fetch = Mathf.Clamp(v, 0f, 100f);
        OnFetchChanged.Invoke(fetch);
        PersistLight();
        _hasPersist = true;
        return true;
    }

    /// External award (e.g., from PlaySatisfaction when slider hits full)
    public void AddFetchPercent(float percent)
    {
        if (Mathf.Approximately(percent, 0f)) return;

        float before = fetch;
        float after  = Mathf.Clamp(before + percent, 0f, 100f);

        if (before < 100f && after >= 100f)
            _pauseUntilUtc = DateTime.UtcNow.AddMinutes(fetchFullPauseMinutes);

        fetch = after;
        OnFetchChanged.Invoke(fetch);

        if (fetch > 50f) _notified50 = false;
        PersistLight();
    }

    void Tick(bool continuous)
    {
        var now = DateTime.UtcNow;
        var dt  = (float)(now - _lastTickUtc).TotalSeconds;
        if (dt <= 0f) { _lastTickUtc = now; return; }
        _lastTickUtc = now;

        if (!(fetch >= 100f - EPS && now < _pauseUntilUtc))
            Decay(dt);

        if (continuous) PersistLight();
    }

    void Decay(float dt)
    {
        if (fetchDrainPerMinute <= 0f || fetch <= 0f) return;

        float before = fetch;
        float delta  = (fetchDrainPerMinute / 60f) * dt;
        float after  = Mathf.Max(0f, before - delta);

        // snap-over logic so >50 → <50 still fires exactly at 50
        if (!_notified50 && before > 50f && after < 50f)
        {
            fetch = 50f;
            OnFetchChanged.Invoke(fetch);
            _notified50 = true;
            OnFetchHit50.Invoke();
            return;
        }

        fetch = after;
        if (!Mathf.Approximately(fetch, before))
        {
            OnFetchChanged.Invoke(fetch);

            if (!_notified50 && before > 50f && Mathf.Abs(fetch - 50f) < EPS)
            {
                _notified50 = true;
                OnFetchHit50.Invoke();
            }

            if (_notified50 && fetch > 50f)
                _notified50 = false;
        }
    }

    void PersistLight()
    {
        PlayerPrefs.SetFloat(KEY_FETCH, fetch);
        PlayerPrefs.SetString(KEY_LAST, DateTime.UtcNow.ToString("o"));
    }
    void Persist() => PersistLight();
}
