using System;
using UnityEngine;
using UnityEngine.Events;

public class PetNeedsManager : MonoBehaviour
{
    public static PetNeedsManager Instance { get; private set; }

    [Range(0,100)] public float walk  = 100f;
    [Range(0,100)] public float clean = 100f;
    [Range(0,100)] public float feed  = 100f;

    public float walkDrainPerMinute  = 0.6f;
    public float cleanDrainPerMinute = 0.8f;
    public float feedDrainPerMinute  = 1.5f;

    public float walkFullPauseMinutes  = 20f;
    public float cleanFullPauseMinutes = 20f;
    public float feedFullPauseMinutes  = 20f;

    [Serializable] public class FloatEvent : UnityEvent<float> {}
    public FloatEvent OnWalkChanged  = new FloatEvent();
    public FloatEvent OnCleanChanged = new FloatEvent();
    public FloatEvent OnFeedChanged  = new FloatEvent();

    public UnityEvent OnWalkHit50  = new UnityEvent();
    public UnityEvent OnCleanHit50 = new UnityEvent();
    public UnityEvent OnFeedHit50  = new UnityEvent();

    DateTime _lastTickUtc;
    DateTime _walkPauseUntilUtc  = DateTime.MinValue;
    DateTime _cleanPauseUntilUtc = DateTime.MinValue;
    DateTime _feedPauseUntilUtc  = DateTime.MinValue;

    bool _walkNotified50, _cleanNotified50, _feedNotified50;
    bool _hasWalkPersist, _hasCleanPersist, _hasFeedPersist;

    const string KEY_LAST  = "petneeds_lastTickUtc";
    const string KEY_WALK  = "petneeds_walk";
    const string KEY_CLEAN = "petneeds_clean";
    const string KEY_FEED  = "petneeds_feed";

    void Awake()
    {
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        _hasWalkPersist  = PlayerPrefs.HasKey(KEY_WALK);
        _hasCleanPersist = PlayerPrefs.HasKey(KEY_CLEAN);
        _hasFeedPersist  = PlayerPrefs.HasKey(KEY_FEED);

        if (_hasWalkPersist)  walk  = PlayerPrefs.GetFloat(KEY_WALK,  walk);
        if (_hasCleanPersist) clean = PlayerPrefs.GetFloat(KEY_CLEAN, clean);
        if (_hasFeedPersist)  feed  = PlayerPrefs.GetFloat(KEY_FEED,  feed);

        if (PlayerPrefs.HasKey(KEY_LAST) && DateTime.TryParse(PlayerPrefs.GetString(KEY_LAST), out var saved))
            _lastTickUtc = saved;
        else
            _lastTickUtc = DateTime.UtcNow;

        _walkNotified50  = Mathf.RoundToInt(walk)  <= 50;
        _cleanNotified50 = Mathf.RoundToInt(clean) <= 50;
        _feedNotified50  = Mathf.RoundToInt(feed)  <= 50;

        Tick(false);
    }

    void OnApplicationPause(bool paused)
    {
        if (paused) Persist();
        else { _lastTickUtc = DateTime.UtcNow; Tick(false); }
    }

    void OnApplicationQuit() => Persist();
    void Update() => Tick(true);

    public bool IsWalkFull()  => walk  >= 99.999f;
    public bool IsCleanFull() => clean >= 99.999f;
    public bool IsFeedFull()  => feed  >= 99.999f;

    public bool InitializeWalkIfUnset (float v) { if (_hasWalkPersist)  return false; walk  = Clamp01(v); OnWalkChanged.Invoke(walk);  PersistLight(); _hasWalkPersist  = true; return true; }
    public bool InitializeCleanIfUnset(float v) { if (_hasCleanPersist) return false; clean = Clamp01(v); OnCleanChanged.Invoke(clean); PersistLight(); _hasCleanPersist = true; return true; }
    public bool InitializeFeedIfUnset (float v) { if (_hasFeedPersist)  return false; feed  = Clamp01(v); OnFeedChanged.Invoke(feed);  PersistLight(); _hasFeedPersist  = true; return true; }

    public void AddWalkPercent (float percent) { AddPercent(ref walk,  percent, walkFullPauseMinutes,  OnWalkChanged,  ref _walkPauseUntilUtc,  ref _walkNotified50,  OnWalkHit50 ); }
    public void AddCleanPercent(float percent) { AddPercent(ref clean, percent, cleanFullPauseMinutes, OnCleanChanged, ref _cleanPauseUntilUtc, ref _cleanNotified50, OnCleanHit50); }
    public void AddFeedPercent (float percent) { AddPercent(ref feed,  percent, feedFullPauseMinutes,  OnFeedChanged,  ref _feedPauseUntilUtc,  ref _feedNotified50,  OnFeedHit50 ); }

    void Tick(bool continuous)
    {
        var now = DateTime.UtcNow;
        var dt = (float)(now - _lastTickUtc).TotalSeconds;
        if (dt <= 0f) { _lastTickUtc = now; return; }
        _lastTickUtc = now;

        if (!(walk  >= 100f - 0.0001f && now < _walkPauseUntilUtc))  Decay(ref walk,  walkDrainPerMinute,  dt, OnWalkChanged,  ref _walkNotified50,  OnWalkHit50);
        if (!(clean >= 100f - 0.0001f && now < _cleanPauseUntilUtc)) Decay(ref clean, cleanDrainPerMinute, dt, OnCleanChanged, ref _cleanNotified50, OnCleanHit50);
        if (!(feed  >= 100f - 0.0001f && now < _feedPauseUntilUtc))  Decay(ref feed,  feedDrainPerMinute,  dt, OnFeedChanged,  ref _feedNotified50,  OnFeedHit50);

        if (continuous) PersistLight();
    }

    void Decay(ref float value, float perMinute, float dt, FloatEvent changedEvt, ref bool notified50, UnityEvent hit50Evt)
    {
        if (perMinute <= 0f || value <= 0f) return;
        float before = value;
        value = Mathf.Max(0f, value - (perMinute/60f) * dt);
        if (!Mathf.Approximately(value, before))
        {
            changedEvt.Invoke(value);
            Check50(before, value, ref notified50, hit50Evt);
        }
    }

    void AddPercent(ref float value, float percent, float pauseMinutes,
                    FloatEvent changedEvt, ref DateTime pauseUntil, ref bool notified50, UnityEvent hit50Evt)
    {
        if (percent <= 0f) return;

        float before = value;
        value = Mathf.Min(100f, value + percent);

        if (before < 100f && value >= 100f)
            pauseUntil = DateTime.UtcNow.AddMinutes(pauseMinutes);

        changedEvt.Invoke(value);
        Check50(before, value, ref notified50, hit50Evt);
        PersistLight();
    }

    void Check50(float before, float after, ref bool flagged, UnityEvent fire)
    {
        int prev = Mathf.RoundToInt(before);
        int curr = Mathf.RoundToInt(after);

        if (!flagged && prev > 50 && curr == 50)
        {
            flagged = true;
            fire.Invoke();
        }
        else if (flagged && prev <= 50 && curr > 50)
        {
            flagged = false;
        }
    }

    static float Clamp01(float v) => Mathf.Clamp(v, 0f, 100f);

    void PersistLight()
    {
        PlayerPrefs.SetFloat(KEY_WALK,  walk);
        PlayerPrefs.SetFloat(KEY_CLEAN, clean);
        PlayerPrefs.SetFloat(KEY_FEED,  feed);
        PlayerPrefs.SetString(KEY_LAST, DateTime.UtcNow.ToString("o"));
    }

    void Persist() => PersistLight();
}
