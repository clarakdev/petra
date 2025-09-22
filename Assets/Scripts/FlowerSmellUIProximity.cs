// FlowerSmellUIProximity.cs
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;

[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(Image))]
public class FlowerSmellUIProximity : MonoBehaviour
{
    public float playerPixelRange = 140f;
    public bool requirePetNear = false;
    public float petPixelRange = 140f;

    public Transform player;
    public Transform pet;
    public Canvas uiCanvas;

    [Range(0,100f)] public float progressPercent = 5f;
    public PanelProgressBar progressBar;
    public string progressBarName = "WalkProgressBar";

    public int xpAmount = 5;
    public UnityEvent<int> OnAwardXP;

    [Header("XP Toast (match Chest UI)")]
    public bool showToast = true;
    public float toastDuration = 1.1f;
    public int toastFontSize = 28;              // <- same as Chest UI
    public Color toastColor = Color.white;
    public Vector2 toastRise = new Vector2(0, 36);

    public Sprite heartSprite;
    public int heartCount = 8;
    public Vector2 heartSizeRange = new Vector2(28, 46);
    public float heartDuration = 0.8f;
    public Vector2 heartRise = new Vector2(60f, 110f);
    public Vector2 heartDriftX = new Vector2(-35f, 35f);
    public Color heartColor = new Color(1f, 0.5f, 0.7f, 1f);

    public bool oneShot = true;
    public float cooldownSeconds = 0f;

    RectTransform _rt;
    Canvas _rootCanvas;
    Camera _cam;
    float _cooldownUntil = -1f;
    bool _consumed;

    void Awake()
    {
        _rt = GetComponent<RectTransform>();
        _rootCanvas = uiCanvas ? uiCanvas : GetComponentInParent<Canvas>();
        _cam = Camera.main;

        if (!progressBar && !string.IsNullOrEmpty(progressBarName))
        {
            var go = GameObject.Find(progressBarName);
            if (go) progressBar = go.GetComponent<PanelProgressBar>();
        }
        if (!progressBar) progressBar = FindObjectOfType<PanelProgressBar>();
        if (!uiCanvas) uiCanvas = FindObjectOfType<Canvas>();

        if (!player)
        {
            var pc = FindObjectOfType<PlayerController>();
            player = pc ? pc.transform : GameObject.Find("Player")?.transform;
        }
        if (!pet && requirePetNear)
        {
            var pf = FindObjectOfType<PetFollower>();
            pet = pf ? pf.transform : GameObject.Find("Pet")?.transform;
        }
    }

    void Update()
    {
        if (_cam == null) _cam = Camera.main;
        if (_cam == null || player == null) return;
        if (oneShot && _consumed) return;
        if (Time.unscaledTime < _cooldownUntil) return;

        Vector2 flowerScreen = RectTransformUtility.WorldToScreenPoint(
            _rootCanvas && _rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _cam,
            _rt.position);

        Vector2 playerScreen = _cam.WorldToScreenPoint(player.position);
        bool playerNear = Vector2.Distance(flowerScreen, playerScreen) <= playerPixelRange;

        bool petNear = true;
        if (requirePetNear)
        {
            if (!pet) return;
            Vector2 petScreen = _cam.WorldToScreenPoint(pet.position);
            petNear = Vector2.Distance(flowerScreen, petScreen) <= petPixelRange;
        }

        if (playerNear && petNear)
        {
            Grant();
            if (oneShot) _consumed = true;
            if (cooldownSeconds > 0f) _cooldownUntil = Time.unscaledTime + cooldownSeconds;
            var img = GetComponent<Image>();
            if (img) img.raycastTarget = false;
        }
    }

    void Grant()
    {
        if (progressBar)
        {
            float add = progressBar.max * (progressPercent / 100f);
            progressBar.SetValue(Mathf.Min(progressBar.value + add, progressBar.max));
        }

        OnAwardXP?.Invoke(xpAmount);

        if (uiCanvas) StartCoroutine(HeartsBurstUI());
        if (showToast && uiCanvas) ShowToast($"You collected {xpAmount} XP!");

        var mgr = FindObjectOfType<WalkUIManager>();
        if (mgr) mgr.TriggerPetHappy();
    }

    System.Collections.IEnumerator HeartsBurstUI()
    {
        if (!uiCanvas) yield break;
        var anchored = GetFlowerAnchoredInCanvas();

        for (int i = 0; i < heartCount; i++)
        {
            var go = new GameObject("HeartUI", typeof(RectTransform), typeof(CanvasRenderer));
            go.transform.SetParent(uiCanvas.transform, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchoredPosition = anchored + new Vector2(Random.Range(-20f, 20f), Random.Range(-6f, 6f));

            float size = Random.Range(heartSizeRange.x, heartSizeRange.y);
            rt.sizeDelta = new Vector2(size, size);
            rt.localScale = Vector3.one;

            Graphic g;
            if (heartSprite)
            {
                var img = go.AddComponent<Image>();
                img.sprite = heartSprite;
                img.preserveAspect = true;
                img.color = heartColor;
                img.raycastTarget = false;
                g = img;
            }
            else
            {
                var tmp = go.AddComponent<TextMeshProUGUI>();
                tmp.text = "♥";
                tmp.fontSize = size;
                tmp.color = heartColor;
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.raycastTarget = false;
                g = tmp;
            }

            float dur = heartDuration;
            float rise = Random.Range(heartRise.x, heartRise.y);
            float drift = Random.Range(heartDriftX.x, heartDriftX.y);
            StartCoroutine(HeartRiseFade(rt, g, dur, new Vector2(drift, rise)));
        }
        yield return null;
    }

    System.Collections.IEnumerator HeartRiseFade(RectTransform rt, Graphic g, float dur, Vector2 move)
    {
        float t = 0f;
        Vector2 start = rt.anchoredPosition;
        Vector2 end = start + move;
        Color c0 = g.color;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float a = Mathf.Clamp01(t / dur);
            rt.anchoredPosition = Vector2.Lerp(start, end, a);
            rt.localScale = Vector3.one * (1f + 0.15f * a);
            g.color = new Color(c0.r, c0.g, c0.b, 1f - a);
            yield return null;
        }
        if (rt) Destroy(rt.gameObject);
    }

    void ShowToast(string msg)
    {
        var go = new GameObject("XPToast", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        go.transform.SetParent(uiCanvas.transform, false);

        var rt = go.GetComponent<RectTransform>();
        var tmp = go.GetComponent<TextMeshProUGUI>();

        tmp.text = msg;
        tmp.fontSize = toastFontSize;           // <- 28 (matches chest)
        tmp.color = toastColor;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;

        // match chest’s general footprint so bigger font isn’t clipped
        rt.sizeDelta = new Vector2(360, 80);
        rt.anchoredPosition = GetFlowerAnchoredInCanvas() + new Vector2(0, 80);

        StartCoroutine(RiseFadeTMP(rt, tmp, toastDuration, toastRise));
    }

    System.Collections.IEnumerator RiseFadeTMP(RectTransform rt, TextMeshProUGUI tmp, float dur, Vector2 rise)
    {
        float t = 0f;
        Vector2 start = rt.anchoredPosition;
        Vector2 end = start + rise;
        Color c0 = tmp.color;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float a = Mathf.Clamp01(t / dur);
            rt.anchoredPosition = Vector2.Lerp(start, end, a);
            tmp.color = new Color(c0.r, c0.g, c0.b, 1f - a);
            yield return null;
        }
        if (rt) Destroy(rt.gameObject);
    }

    Vector2 GetFlowerAnchoredInCanvas()
    {
        var cam = (_rootCanvas && _rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay) ? null : _cam;
        Vector2 screen = RectTransformUtility.WorldToScreenPoint(cam, _rt.position);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            uiCanvas.transform as RectTransform, screen,
            uiCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _cam,
            out var anchored);
        return anchored;
    }
}
