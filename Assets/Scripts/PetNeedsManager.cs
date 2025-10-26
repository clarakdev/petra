using System;
using UnityEngine;
using UnityEngine.Events;

public class PetNeedsManager : MonoBehaviour
{
    public static PetNeedsManager Instance { get; private set; }

    [Header("Starting values (0..100)")]
    [Range(0, 100)] public float walk = 0f;
    [Range(0, 100)] public float clean = 0f;
    [Range(0, 100)] public float feed = 0f;

    [Header("Decay per minute")]
    public float walkDrainPerMinute = 0.6f;
    public float cleanDrainPerMinute = 0.8f;
    public float feedDrainPerMinute = 1.5f;

    [Header("Pause at 100% (minutes)")]
    public float walkFullPauseMinutes = 20f;
    public float cleanFullPauseMinutes = 20f;
    public float feedFullPauseMinutes = 20f;

    // ---- Events (per-need) ----
    [Serializable]
    public class FloatEvent : UnityEvent<float> { }

    public FloatEvent OnWalkChanged = new FloatEvent();
    public FloatEvent OnCleanChanged = new FloatEvent();
    public FloatEvent OnFeedChanged = new FloatEvent();

    public UnityEvent OnWalkHit50 = new UnityEvent();
    public UnityEvent OnCleanHit50 = new UnityEvent();
    public UnityEvent OnFeedHit50 = new UnityEvent();

    // ---- persistence & timers ----
    DateTime _lastTickUtc;
    DateTime _walkPauseUntilUtc = DateTime.MinValue;
    DateTime _cleanPauseUntilUtc = DateTime.MinValue;
    DateTime _feedPauseUntilUtc = DateTime.MinValue;

    // exact-50 edge flags (true = we've already fired at/below 50; re-arm when rising above 50)
    bool _walkNotified50, _cleanNotified50, _feedNotified50;

    // did we load persisted values?
    bool _hasWalkPersist, _hasCleanPersist, _hasFeedPersist;

    const string KEY_LAST = "petneeds_lastTickUtc";
    const string KEY_WALK = "petneeds_walk";
    const string KEY_CLEAN = "petneeds_clean";
    const string KEY_FEED = "petneeds_feed";
    const float EPS = 0.0001f;

    void Awake()
    {
        if (Instance && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Load persisted values (if present)
        _hasWalkPersist = PlayerPrefs.HasKey(KEY_WALK);
        _hasCleanPersist = PlayerPrefs.HasKey(KEY_CLEAN);
        _hasFeedPersist = PlayerPrefs.HasKey(KEY_FEED);

        if (_hasWalkPersist)
            walk = PlayerPrefs.GetFloat(KEY_WALK, walk);
        if (_hasCleanPersist)
            clean = PlayerPrefs.GetFloat(KEY_CLEAN, clean);
        if (_hasFeedPersist)
            feed = PlayerPrefs.GetFloat(KEY_FEED, feed);

        if (PlayerPrefs.HasKey(KEY_LAST) && DateTime.TryParse(PlayerPrefs.GetString(KEY_LAST), out var saved))
            _lastTickUtc = saved;
        else
            _lastTickUtc = DateTime.UtcNow;

        // Arm so we only fire again after rising above 50
        _walkNotified50 = (walk <= 50f);
        _cleanNotified50 = (clean <= 50f);
        _feedNotified50 = (feed <= 50f);

        Tick(false); // catch-up once
    }

    void OnApplicationPause(bool paused)
    {
        if (paused)
            Persist();
        else
        {
            _lastTickUtc = DateTime.UtcNow;
            Tick(false);
        }
    }

    void OnApplicationQuit() => Persist();
    void Update() => Tick(true);

    // ---- Public helpers ----
    public bool IsWalkFull() => walk >= 99.999f;
    public bool IsCleanFull() => clean >= 99.999f;
    public bool IsFeedFull() => feed >= 99.999f;

    // Seed once from scene bars (only if we *didn't* load persisted value)
    public bool InitializeWalkIfUnset(float v)
    {
        if (_hasWalkPersist) return false;
        walk = Clamp100(v);
        OnWalkChanged.Invoke(walk);
        PersistLight();
        _hasWalkPersist = true;
        return true;
    }

    public bool InitializeCleanIfUnset(float v)
    {
        if (_hasCleanPersist) return false;
        clean = Clamp100(v);
        OnCleanChanged.Invoke(clean);
        PersistLight();
        _hasCleanPersist = true;
        return true;
    }

    public bool InitializeFeedIfUnset(float v)
    {
        if (_hasFeedPersist) return false;
        feed = Clamp100(v);
        OnFeedChanged.Invoke(feed);
        PersistLight();
        _hasFeedPersist = true;
        return true;
    }

    public void AddWalkPercent(float percent)
    {
        AddPercent(ref walk, percent, walkFullPauseMinutes, OnWalkChanged,
            ref _walkPauseUntilUtc, ref _walkNotified50, OnWalkHit50);
    }

    public void AddCleanPercent(float percent)
    {
        AddPercent(ref clean, percent, cleanFullPauseMinutes, OnCleanChanged,
            ref _cleanPauseUntilUtc, ref _cleanNotified50, OnCleanHit50);
    }

    public void AddFeedPercent(float percent)
    {
        AddPercent(ref feed, percent, feedFullPauseMinutes, OnFeedChanged,
            ref _feedPauseUntilUtc, ref _feedNotified50, OnFeedHit50);
    }

    // ---- Core ticking/decay ----
    void Tick(bool continuous)
    {
        var now = DateTime.UtcNow;
        var dt = (float)(now - _lastTickUtc).TotalSeconds;
        if (dt <= 0f)
        {
            _lastTickUtc = now;
            return;
        }
        _lastTickUtc = now;

        // Respect "full pause" windows at 100
        if (!(walk >= 100f - EPS && now < _walkPauseUntilUtc))
            Decay(ref walk, walkDrainPerMinute, dt, OnWalkChanged, ref _walkNotified50, OnWalkHit50);

        if (!(clean >= 100f - EPS && now < _cleanPauseUntilUtc))
            Decay(ref clean, cleanDrainPerMinute, dt, OnCleanChanged, ref _cleanNotified50, OnCleanHit50);

        if (!(feed >= 100f - EPS && now < _feedPauseUntilUtc))
            Decay(ref feed, feedDrainPerMinute, dt, OnFeedChanged, ref _feedNotified50, OnFeedHit50);

        if (continuous)
            PersistLight();
    }

    // Decay with "snap-to-50" so an update that jumps 51 -> 49 still fires the exact 50 event.
    void Decay(ref float value, float perMinute, float dt,
               FloatEvent changedEvt, ref bool notified50, UnityEvent hit50Evt)
    {
        if (perMinute <= 0f || value <= 0f) return;

        float before = value;
        float delta = (perMinute / 60f) * dt;
        if (delta <= 0f) return;

        float after = Mathf.Max(0f, before - delta);

        // If we skipped over 50 in one tick, snap to 50 exactly and fire once.
        if (!notified50 && before > 50f && after < 50f)
        {
            value = 50f;
            changedEvt.Invoke(value);
            notified50 = true;
            hit50Evt.Invoke();
            return;
        }

        // Normal path
        value = after;
        if (Mathf.Abs(value - before) >= EPS)
        {
            changedEvt.Invoke(value);

            // Re-arm if we went above 50
            if (notified50 && value > 50f)
                notified50 = false;

            // If we land exactly on 50 (rare without snap), fire once.
            if (!notified50 && before > 50f && Mathf.Abs(value - 50f) < EPS)
            {
                notified50 = true;
                hit50Evt.Invoke();
            }
        }
    }

    // Add percent (healing). Re-arms when we rise above 50. (Adds won’t create a 50-from-above case.)
    void AddPercent(ref float value, float percent, float pauseMinutes,
                    FloatEvent changedEvt, ref DateTime pauseUntilUtc,
                    ref bool notified50, UnityEvent hit50Evt)
    {
        value = Clamp100(value + percent);
        changedEvt.Invoke(value);

        if (value >= 100f)
        {
            pauseUntilUtc = DateTime.UtcNow.AddMinutes(pauseMinutes);
        }

        if (value > 50f)
            notified50 = false;
    }

    float Clamp100(float v) => Mathf.Clamp(v, 0f, 100f);

    void PersistLight()
    {
        PlayerPrefs.SetFloat(KEY_WALK, walk);
        PlayerPrefs.SetFloat(KEY_CLEAN, clean);
        PlayerPrefs.SetFloat(KEY_FEED, feed);
        PlayerPrefs.Save();
    }

    void Persist()
    {
        PlayerPrefs.SetString(KEY_LAST, DateTime.UtcNow.ToString());
        PersistLight();
    }
}
