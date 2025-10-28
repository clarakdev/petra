using System;
using UnityEngine;
using UnityEngine.Events;

public class PetNeedsManager : MonoBehaviour
{
    public static PetNeedsManager Instance { get; private set; }

    [Header("Starting values (0..100)")]
    [Range(0, 100)] public float walk = 100f;
    [Range(0, 100)] public float clean = 100f;
    [Range(0, 100)] public float feed = 100f;
    [Range(0, 100)] public float fetch = 100f;

    [Header("Decay per minute")]
    public float walkDrainPerMinute = 10f;
    public float cleanDrainPerMinute = 0.8f;
    public float feedDrainPerMinute = 10f;
    public float fetchDrainPerMinute = 10f;

    [Header("Pause at 100% (minutes)")]
    public float walkFullPauseMinutes = 0.02f;
    public float cleanFullPauseMinutes = 0.02f;
    public float feedFullPauseMinutes = 0.02f;
    public float fetchFullPauseMinutes = 0.02f;

    [Header("Rewards")]
    public int rewardCoinsPerFull = 50;

    [Serializable] public class FloatEvent : UnityEvent<float> { }
    public FloatEvent OnWalkChanged = new FloatEvent();
    public FloatEvent OnCleanChanged = new FloatEvent();
    public FloatEvent OnFeedChanged = new FloatEvent();
    public FloatEvent OnFetchChanged = new FloatEvent();

    public UnityEvent OnWalkHit50 = new UnityEvent();
    public UnityEvent OnCleanHit50 = new UnityEvent();
    public UnityEvent OnFeedHit50 = new UnityEvent();
    public UnityEvent OnFetchHit50 = new UnityEvent();

    DateTime _lastTickUtc;
    DateTime _walkPauseUntilUtc = DateTime.MinValue;
    DateTime _cleanPauseUntilUtc = DateTime.MinValue;
    DateTime _feedPauseUntilUtc = DateTime.MinValue;
    DateTime _fetchPauseUntilUtc = DateTime.MinValue;

    bool _walkNotified50, _cleanNotified50, _feedNotified50, _fetchNotified50;
    bool _hasWalkPersist, _hasCleanPersist, _hasFeedPersist, _hasFetchPersist;

    const string KEY_LAST = "petneeds_lastTickUtc";
    const string KEY_WALK = "petneeds_walk";
    const string KEY_CLEAN = "petneeds_clean";
    const string KEY_FEED = "petneeds_feed";
    const string KEY_FETCH = "petneeds_fetch";
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

        _hasWalkPersist = PlayerPrefs.HasKey(KEY_WALK);
        _hasCleanPersist = PlayerPrefs.HasKey(KEY_CLEAN);
        _hasFeedPersist = PlayerPrefs.HasKey(KEY_FEED);
        _hasFetchPersist = PlayerPrefs.HasKey(KEY_FETCH);

        if (_hasWalkPersist) walk = PlayerPrefs.GetFloat(KEY_WALK, walk);
        if (_hasCleanPersist) clean = PlayerPrefs.GetFloat(KEY_CLEAN, clean);
        if (_hasFeedPersist) feed = PlayerPrefs.GetFloat(KEY_FEED, feed);
        if (_hasFetchPersist) fetch = PlayerPrefs.GetFloat(KEY_FETCH, fetch);

        if (PlayerPrefs.HasKey(KEY_LAST) && DateTime.TryParse(PlayerPrefs.GetString(KEY_LAST), out var saved))
            _lastTickUtc = saved;
        else
            _lastTickUtc = DateTime.UtcNow;

        _walkNotified50 = (walk <= 50f);
        _cleanNotified50 = (clean <= 50f);
        _feedNotified50 = (feed <= 50f);
        _fetchNotified50 = (fetch <= 50f);

        Tick(false);
    }

    void OnApplicationPause(bool paused)
    {
        if (paused) Persist();
        else { _lastTickUtc = DateTime.UtcNow; Tick(false); }
    }

    void OnApplicationQuit() => Persist();
    void Update() => Tick(true);

    // === Initialization Helpers ===
    public bool InitializeWalkIfUnset(float v)
    {
        if (_hasWalkPersist) return false;
        walk = Clamp100(v);
        OnWalkChanged.Invoke(walk);
        PlayerPrefs.SetFloat(KEY_WALK, walk);
        PlayerPrefs.Save();
        _hasWalkPersist = true;
        return true;
    }

    public bool InitializeCleanIfUnset(float v)
    {
        if (_hasCleanPersist) return false;
        clean = Clamp100(v);
        OnCleanChanged.Invoke(clean);
        PlayerPrefs.SetFloat(KEY_CLEAN, clean);
        PlayerPrefs.Save();
        _hasCleanPersist = true;
        return true;
    }

    public bool InitializeFeedIfUnset(float v)
    {
        if (_hasFeedPersist) return false;
        feed = Clamp100(v);
        OnFeedChanged.Invoke(feed);
        PlayerPrefs.SetFloat(KEY_FEED, feed);
        PlayerPrefs.Save();
        _hasFeedPersist = true;
        return true;
    }

    public bool InitializeFetchIfUnset(float v)
    {
        if (_hasFetchPersist) return false;
        fetch = Clamp100(v);
        OnFetchChanged.Invoke(fetch);
        PlayerPrefs.SetFloat(KEY_FETCH, fetch);
        PlayerPrefs.Save();
        _hasFetchPersist = true;
        return true;
    }

    // === Add % and Reward ===
    public void AddWalkPercent(float percent)
    {
        float before = walk;
        walk = Mathf.Clamp(walk + percent, 0f, 100f);
        OnWalkChanged.Invoke(walk);
        PlayerPrefs.SetFloat(KEY_WALK, walk);

        if (before < 100f && walk >= 100f)
        {
            _walkPauseUntilUtc = DateTime.UtcNow.AddMinutes(walkFullPauseMinutes);
            GiveReward("walk");
        }

        PlayerPrefs.Save();
    }

    public void AddCleanPercent(float percent)
    {
        float before = clean;
        clean = Mathf.Clamp(clean + percent, 0f, 100f);
        OnCleanChanged.Invoke(clean);
        PlayerPrefs.SetFloat(KEY_CLEAN, clean);

        if (before < 100f && clean >= 100f)
        {
            _cleanPauseUntilUtc = DateTime.UtcNow.AddMinutes(cleanFullPauseMinutes);
            GiveReward("clean");
        }

        PlayerPrefs.Save();
    }

    public void AddFeedPercent(float percent)
    {
        float before = feed;
        feed = Mathf.Clamp(feed + percent, 0f, 100f);
        OnFeedChanged.Invoke(feed);
        PlayerPrefs.SetFloat(KEY_FEED, feed);

        if (before < 100f && feed >= 100f)
        {
            _feedPauseUntilUtc = DateTime.UtcNow.AddMinutes(feedFullPauseMinutes);
            GiveReward("feed");
        }

        PlayerPrefs.Save();
    }

    public void AddFetchPercent(float percent)
    {
        float before = fetch;
        fetch = Mathf.Clamp(fetch + percent, 0f, 100f);
        OnFetchChanged.Invoke(fetch);
        PlayerPrefs.SetFloat(KEY_FETCH, fetch);

        if (before < 100f && fetch >= 100f)
        {
            _fetchPauseUntilUtc = DateTime.UtcNow.AddMinutes(fetchFullPauseMinutes);
            GiveReward("fetch");
        }

        PlayerPrefs.Save();
    }

    // === Reward Logic ===
    void GiveReward(string type)
    {
        if (PlayerCurrency.Instance != null)
        {
            PlayerCurrency.Instance.EarnCurrency(rewardCoinsPerFull);
            Debug.Log($"[PetNeedsManager] {type} bar full! Rewarded {rewardCoinsPerFull} coins.");
        }
        else
        {
            Debug.LogWarning($"[PetNeedsManager] PlayerCurrency.Instance is NULL — {type} reward could not be given!");
        }
    }

    // === Decay ===
    void Tick(bool continuous)
    {
        var now = DateTime.UtcNow;
        var dt = (float)(now - _lastTickUtc).TotalSeconds;
        if (dt <= 0f) { _lastTickUtc = now; return; }
        _lastTickUtc = now;

        if (!(walk >= 100f - EPS && now < _walkPauseUntilUtc))
            Decay(ref walk, walkDrainPerMinute, dt, OnWalkChanged, ref _walkNotified50, OnWalkHit50);
        if (!(clean >= 100f - EPS && now < _cleanPauseUntilUtc))
            Decay(ref clean, cleanDrainPerMinute, dt, OnCleanChanged, ref _cleanNotified50, OnCleanHit50);
        if (!(feed >= 100f - EPS && now < _feedPauseUntilUtc))
            Decay(ref feed, feedDrainPerMinute, dt, OnFeedChanged, ref _feedNotified50, OnFeedHit50);
        if (!(fetch >= 100f - EPS && now < _fetchPauseUntilUtc))
            Decay(ref fetch, fetchDrainPerMinute, dt, OnFetchChanged, ref _fetchNotified50, OnFetchHit50);

        if (continuous) PersistLight();
    }

    void Decay(ref float value, float perMinute, float dt,
               FloatEvent changedEvt, ref bool notified50, UnityEvent hit50Evt)
    {
        if (perMinute <= 0f || value <= 0f) return;
        float before = value;
        float delta = (perMinute / 60f) * dt;
        if (delta <= 0f) return;

        float after = Mathf.Max(0f, before - delta);

        if (!notified50 && before > 50f && after < 50f)
        {
            value = 50f;
            changedEvt.Invoke(value);
            notified50 = true;
            hit50Evt.Invoke();
            return;
        }

        value = after;
        if (Mathf.Abs(value - before) >= EPS)
        {
            changedEvt.Invoke(value);
            if (notified50 && value > 50f)
                notified50 = false;

            if (!notified50 && before > 50f && Mathf.Abs(value - 50f) < EPS)
            {
                notified50 = true;
                hit50Evt.Invoke();
            }
        }
    }

    // === Persistence ===
    void PersistLight()
    {
        PlayerPrefs.SetFloat(KEY_WALK, walk);
        PlayerPrefs.SetFloat(KEY_CLEAN, clean);
        PlayerPrefs.SetFloat(KEY_FEED, feed);
        PlayerPrefs.SetFloat(KEY_FETCH, fetch);
        PlayerPrefs.Save();
    }

    void Persist()
    {
        PersistLight();
        PlayerPrefs.SetString(KEY_LAST, DateTime.UtcNow.ToString());
        PlayerPrefs.Save();
    }

    // === Utility ===
    float Clamp100(float v) => Mathf.Clamp(v, 0f, 100f);
    public bool IsWalkFull() => walk >= 99.999f;
    public bool IsCleanFull() => clean >= 99.999f;
    public bool IsFeedFull() => feed >= 99.999f;
    public bool IsFetchFull() => fetch >= 99.999f;
}
