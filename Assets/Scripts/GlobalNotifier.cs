using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.Events;

public class GlobalNotifier : MonoBehaviour
{
    public static GlobalNotifier Instance { get; private set; }

    [Header("Behavior")]
    [Tooltip("If true, auto-subscribes to PetNeedsManager 50% events in code. " +
             "If you wire events in the Inspector instead, turn this OFF to avoid double toasts.")]
    public bool autoSubscribe = true;

    [Header("Toast Settings")]
    [Tooltip("How long the toast stays fully visible (not counting fade in/out).")]
    public float toastHoldSeconds = 4.0f;
    [Tooltip("Fade-in time for the toast.")]
    public float fadeInSeconds = 0.15f;
    [Tooltip("Fade-out time for the toast.")]
    public float fadeOutSeconds = 0.35f;
    [Tooltip("Toast panel size (px).")]
    public Vector2 toastSize = new Vector2(720, 90);
    [Tooltip("How far the toast rises during fade-in (px).")]
    public Vector2 toastRise = new Vector2(0, 70);
    [Tooltip("Toast font size.")]
    public int toastFontSize = 30;

    Canvas _canvas;
    RectTransform _root;

    bool _subscribed;
    UnityAction _onWalk50, _onClean50, _onFeed50;

    // one-time “already low” guard so we don’t spam on each scene load
    bool _initialChecked;
    bool _initialWalkShown, _initialCleanShown, _initialFeedShown;

    void Awake()
    {
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        BuildCanvas();
        TrySubscribe();
        MaybeShowInitialIfAlreadyLow();
    }

    void Start()
    {
        TrySubscribe();
        MaybeShowInitialIfAlreadyLow();
    }

    void Update()
    {
        if (!_subscribed) TrySubscribe();
    }

    void OnDestroy() => Unsubscribe();

    void TrySubscribe()
    {
        if (_subscribed || !autoSubscribe) return;

        var needs = PetNeedsManager.Instance;
        if (needs == null) return;

        // 🔔 Fire when each stat crosses downward to ≤ 50%
        _onWalk50  = () => ShowToast("Time to walk your pet!",  toastHoldSeconds);
        _onClean50 = () => ShowToast("Time to clean your pet!", toastHoldSeconds);
        _onFeed50  = () => ShowToast("Time to feed your pet!",  toastHoldSeconds);

        needs.OnWalkHit50 .AddListener(_onWalk50);
        needs.OnCleanHit50.AddListener(_onClean50);
        needs.OnFeedHit50 .AddListener(_onFeed50);

        _subscribed = true;
    }

    void Unsubscribe()
    {
        if (!_subscribed) return;

        var needs = PetNeedsManager.Instance;
        if (needs != null)
        {
            if (_onWalk50  != null) needs.OnWalkHit50 .RemoveListener(_onWalk50);
            if (_onClean50 != null) needs.OnCleanHit50.RemoveListener(_onClean50);
            if (_onFeed50  != null) needs.OnFeedHit50 .RemoveListener(_onFeed50);
        }

        _subscribed = false;
        _onWalk50 = _onClean50 = _onFeed50 = null;
    }

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

    // ---------- Public API (also handy for Inspector wiring) ----------

    public void SendMessageToast(string msg) => ShowToast(msg, toastHoldSeconds);
    public void NotifyWalk50()  => ShowToast("Time to walk your pet!",  toastHoldSeconds);
    public void NotifyClean50() => ShowToast("Time to clean your pet!", toastHoldSeconds);
    public void NotifyFeed50()  => ShowToast("Time to feed your pet!",  toastHoldSeconds);

    [ContextMenu("Test Toast")]
    void _TestToast() => ShowToast("Test toast – notifier works!", toastHoldSeconds);

    public void ShowToast(string msg, float holdSeconds) => StartCoroutine(ToastPhased(msg, holdSeconds));

    IEnumerator ToastPhased(string msg, float holdSeconds)
    {
        // Create UI
        var bg = new GameObject("ToastBG", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
        bg.SetParent(_root, false);
        bg.sizeDelta = toastSize;
        bg.anchoredPosition = Vector2.zero;

        var bgImg = bg.GetComponent<Image>();
        bgImg.color = new Color(0, 0, 0, 0f); // start invisible
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
        text.color = new Color(1, 1, 1, 0f); // start invisible
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

        // Fade Out (stay at end position)
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

    // ---------- One-time check so you get a toast if you enter while already low ----------
    void MaybeShowInitialIfAlreadyLow()
    {
        if (_initialChecked) return;
        var needs = PetNeedsManager.Instance;
        if (needs == null) return;

        // We only show once per session if already ≤50 on load.
        if (!_initialWalkShown  && needs.walk  <= 50f) { _initialWalkShown  = true; ShowToast("Time to walk your pet!",  toastHoldSeconds); }
        if (!_initialCleanShown && needs.clean <= 50f) { _initialCleanShown = true; ShowToast("Time to clean your pet!", toastHoldSeconds); }
        if (!_initialFeedShown  && needs.feed  <= 50f) { _initialFeedShown  = true; ShowToast("Time to feed your pet!",  toastHoldSeconds); }

        _initialChecked = true;
    }
}
