using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

[DisallowMultipleComponent]
public class GlobalNotifier : MonoBehaviour
{
    public static GlobalNotifier Instance { get; private set; }

    [Header("Behavior")]
    [Tooltip("If true, auto-subscribes to 50% events in code. Turn OFF if you wire events in the Inspector.")]
    public bool autoSubscribe = true;

    [Header("Toast Settings")]
    public float   toastHoldSeconds = 4.0f;
    public float   fadeInSeconds    = 0.15f;
    public float   fadeOutSeconds   = 0.35f;
    public Vector2 toastSize        = new Vector2(720, 90);
    public Vector2 toastRise        = new Vector2(0, 70);
    [Tooltip("If true, auto-subscribes to PetNeedsManager 50% events in code. " +
             "If you wire events in the Inspector instead, turn this OFF to avoid double toasts.")]
    public bool autoSubscribe = true;

    [Header("Toast Settings")]
    [Tooltip("How long the toast stays fully visible (not counting fade in/out).")]
    public float   toastHoldSeconds = 4.0f;
    [Tooltip("Fade-in time for the toast.")]
    public float   fadeInSeconds    = 0.15f;
    [Tooltip("Fade-out time for the toast.")]
    public float   fadeOutSeconds   = 0.35f;
    [Tooltip("Toast panel size (px).")]
    public Vector2 toastSize        = new Vector2(720, 90);
    [Tooltip("How far the toast rises during fade-in (px).")]
    public Vector2 toastRise        = new Vector2(0, 70);
    [Tooltip("Toast font size.")]
    public int     toastFontSize    = 30;

    const float EXACT_EPS = 0.0001f;

    Canvas _canvas;
    RectTransform _root;

    // Track what we’re bound to so we can safely unsubscribe/rebind
    bool _subscribedCore;
    PetNeedsManager  _boundCore;

    bool _subscribedFetch;
    FetchNeedManager _boundFetch;

    // one-time exact-50 on startup (don’t reset per scene)
    bool _didInitialCheck;
    bool _initialWalkShown, _initialCleanShown, _initialFeedShown, _initialFetchShown;
    bool _subscribed;
    PetNeedsManager _boundMgr; // track which manager we’re listening to

    // one-time “exactly 50 at startup” guards (do NOT reset on scene change)
    bool _initialWalkShown, _initialCleanShown, _initialFeedShown;
    bool _didInitialCheck;

    void Awake()
    {
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        BuildCanvas();
        TrySubscribeCore(true);
        TrySubscribeFetch(true);
        TrySubscribe(force:true);
        MaybeShowInitialIfExactly50();

        SceneManager.activeSceneChanged += OnSceneChanged;
    }

    void OnDestroy()
    {
        SceneManager.activeSceneChanged -= OnSceneChanged;
        UnsubscribeCore();
        UnsubscribeFetch();
        Unsubscribe();
    }

    void Update()
    {
        // Rebind if managers are recreated/swapped
        TrySubscribeCore(false);
        TrySubscribeFetch(false);
        // If manager got recreated or we lost binding, rebind.
        TrySubscribe(force:false);
    }

    void OnSceneChanged(Scene _, Scene __)
    {
        if (_canvas == null) BuildCanvas();
        _canvas.sortingOrder = 5000;

        TrySubscribeCore(false);
        TrySubscribeFetch(false);
        // no per-scene initial toasts
    }

    // -------- Core (walk/clean/feed) --------
    void TrySubscribeCore(bool force)
    {
        if (!autoSubscribe) return;
        var mgr = PetNeedsManager.Instance;
        if (mgr == null) return;

        if (force || !_subscribedCore || _boundCore != mgr)
        {
            UnsubscribeCore();
        // Rebind if manager instance changed on this scene
        TrySubscribe(force:false);
        // No initial per-scene toasts; only once at app start.
    }

    // -------- Subscriptions --------

    void TrySubscribe(bool force)
    {
        if (!autoSubscribe) return;

        var mgr = PetNeedsManager.Instance;
        if (mgr == null) return;

        // If bound manager changed (or we were never bound), rebind
        if (force || !_subscribed || _boundMgr != mgr)
        {
            Unsubscribe(); // safe no-op if nothing bound

            mgr.OnWalkHit50 .AddListener(OnWalkHit50);
            mgr.OnCleanHit50.AddListener(OnCleanHit50);
            mgr.OnFeedHit50 .AddListener(OnFeedHit50);

            _boundCore     = mgr;
            _subscribedCore = true;
        }
    }
    void UnsubscribeCore()
    {
        if (!_subscribedCore) return;
        if (_boundCore != null)
        {
            _boundCore.OnWalkHit50 .RemoveListener(OnWalkHit50);
            _boundCore.OnCleanHit50.RemoveListener(OnCleanHit50);
            _boundCore.OnFeedHit50 .RemoveListener(OnFeedHit50);
        }
        _boundCore = null;
        _subscribedCore = false;
    }

    // -------- Fetch --------
    void TrySubscribeFetch(bool force)
    {
        if (!autoSubscribe) return;
        var fm = FetchNeedManager.Instance;
        if (fm == null) return;

        if (force || !_subscribedFetch || _boundFetch != fm)
        {
            UnsubscribeFetch();

            fm.OnFetchHit50.AddListener(OnFetchHit50);

            _boundFetch     = fm;
            _subscribedFetch = true;
        }
    }
    void UnsubscribeFetch()
    {
        if (!_subscribedFetch) return;
        if (_boundFetch != null)
            _boundFetch.OnFetchHit50.RemoveListener(OnFetchHit50);
        _boundFetch = null;
        _subscribedFetch = false;
    }

    // -------- Handlers --------
    void OnWalkHit50 () => ShowToast("Time to walk your pet!",  toastHoldSeconds);
    void OnCleanHit50() => ShowToast("Time to clean your pet!", toastHoldSeconds);
    void OnFeedHit50 () => ShowToast("Time to feed your pet!",  toastHoldSeconds);
    void OnFetchHit50() => ShowToast("Time to play fetch!",    toastHoldSeconds);

    // -------- UI build / toast --------
            _boundMgr   = mgr;
            _subscribed = true;
        }
    }

    void Unsubscribe()
    {
        if (!_subscribed) return;

        if (_boundMgr != null)
        {
            _boundMgr.OnWalkHit50 .RemoveListener(OnWalkHit50);
            _boundMgr.OnCleanHit50.RemoveListener(OnCleanHit50);
            _boundMgr.OnFeedHit50 .RemoveListener(OnFeedHit50);
        }

        _boundMgr   = null;
        _subscribed = false;
    }

    void OnWalkHit50 () => ShowToast("Time to walk your pet!",  toastHoldSeconds);
    void OnCleanHit50() => ShowToast("Time to clean your pet!", toastHoldSeconds);
    void OnFeedHit50 () => ShowToast("Time to feed your pet!",  toastHoldSeconds);

    // -------- UI build / toast --------

    void BuildCanvas()
    {
        var go = new GameObject("GlobalNotifierCanvas",
            typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        go.transform.SetParent(transform, false);

        _canvas = go.GetComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 5000;

        var scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);

        _root = new GameObject("Root", typeof(RectTransform)).GetComponent<RectTransform>();
        _root.SetParent(go.transform, false);
        _root.anchorMin = new Vector2(0.5f, 1f);
        _root.anchorMax = new Vector2(0.5f, 1f);
        _root.pivot     = new Vector2(0.5f, 1f);
        _root.anchoredPosition = new Vector2(0, -80);
        _root.sizeDelta = new Vector2(1080, 200);
    }

    public void ShowToast(string msg, float holdSeconds) => StartCoroutine(ToastPhased(msg, holdSeconds));

    IEnumerator ToastPhased(string msg, float holdSeconds)
    {
        var bg = new GameObject("ToastBG", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
        bg.SetParent(_root, false);
        bg.sizeDelta = toastSize;
        bg.anchoredPosition = Vector2.zero;

        var bgImg = bg.GetComponent<Image>();
        bgImg.color = new Color(0, 0, 0, 0f);
        bgImg.raycastTarget = false;

        var text = new GameObject("ToastText", typeof(RectTransform), typeof(TextMeshProUGUI))
            .GetComponent<TextMeshProUGUI>();
        text.transform.SetParent(bg, false);
        var r = text.rectTransform;
        r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one;
        r.offsetMin = Vector2.zero; r.offsetMax = Vector2.zero;

        text.text = msg;
        text.alignment = TextAlignmentOptions.Center;
        text.fontSize = toastFontSize;
        text.color = new Color(1, 1, 1, 0f);
        text.raycastTarget = false;

        float fin  = Mathf.Max(0.01f, fadeInSeconds);
        float hold = Mathf.Max(0f,    holdSeconds);
        float fout = Mathf.Max(0.01f, fadeOutSeconds);

        // Fade In + Rise
        {
            float t = 0f;
            Vector2 start = Vector2.zero;
            Vector2 end   = toastRise;
            while (t < fin)
            {
                t += Time.unscaledDeltaTime;
                float a = Mathf.Clamp01(t / fin);
                bg.anchoredPosition = Vector2.Lerp(start, end, a);
                bgImg.color = new Color(0, 0, 0, 0.85f * a);
                text.color  = new Color(1, 1, 1, a);
                yield return null;
            }
            bg.anchoredPosition = end;
            bgImg.color = new Color(0, 0, 0, 0.85f);
            text.color  = Color.white;
        }

        // Hold
        if (hold > 0f) yield return new WaitForSecondsRealtime(hold);

        // Fade Out
        {
            float t = 0f;
            while (t < fout)
            {
                t += Time.unscaledDeltaTime;
                float a = 1f - Mathf.Clamp01(t / fout);
                bgImg.color = new Color(0, 0, 0, 0.85f * a);
                text.color  = new Color(1, 1, 1, a);
                yield return null;
            }
        }

        if (bg) Destroy(bg.gameObject);
    }
    // -------- Startup-only exact-50 check (includes Fetch) --------
    // -------- Initial (startup-only) exact-50 check --------
    void MaybeShowInitialIfExactly50()
    {
        if (_didInitialCheck) return;
        _didInitialCheck = true;

        var needs = PetNeedsManager.Instance;
        if (needs != null)
        {
            if (!_initialWalkShown  && Mathf.Abs(needs.walk  - 50f) < EXACT_EPS) { _initialWalkShown  = true; ShowToast("Time to walk your pet!",  toastHoldSeconds); }
            if (!_initialCleanShown && Mathf.Abs(needs.clean - 50f) < EXACT_EPS) { _initialCleanShown = true; ShowToast("Time to clean your pet!", toastHoldSeconds); }
            if (!_initialFeedShown  && Mathf.Abs(needs.feed  - 50f) < EXACT_EPS) { _initialFeedShown  = true; ShowToast("Time to feed your pet!",  toastHoldSeconds); }
        }

        var fetch = FetchNeedManager.Instance;
        if (fetch != null && !_initialFetchShown && Mathf.Abs(fetch.fetch - 50f) < EXACT_EPS)
        {
            _initialFetchShown = true;
            ShowToast("Time to play fetch!", toastHoldSeconds);
        }
        if (needs == null) return;

        if (!_initialWalkShown  && Mathf.Abs(needs.walk  - 50f) < EXACT_EPS) { _initialWalkShown  = true; ShowToast("Time to walk your pet!",  toastHoldSeconds); }
        if (!_initialCleanShown && Mathf.Abs(needs.clean - 50f) < EXACT_EPS) { _initialCleanShown = true; ShowToast("Time to clean your pet!", toastHoldSeconds); }
        if (!_initialFeedShown  && Mathf.Abs(needs.feed  - 50f) < EXACT_EPS) { _initialFeedShown  = true; ShowToast("Time to feed your pet!",  toastHoldSeconds); }
    }
}
