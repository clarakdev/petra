using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro; // << NEW

[RequireComponent(typeof(RectTransform))]
public class ChestOpenUI : MonoBehaviour
{
    [Header("Progress")]
    [Range(0,100f)] public float progressPercent = 40f; // +40%

    [Header("Single Image Mode")]
    [Tooltip("Assign if you want to swap sprites on ONE Image.")]
    public Image chestImage;           // optional; if left empty, will try GetComponent<Image>()
    public Sprite closedSprite;        // your 'closed' sprite
    public Sprite openSprite;          // your 'open' sprite

    [Header("Dual Object Mode (use this OR Single Image)")]
    [Tooltip("If you have two separate objects (one with closed art, one with open art), assign them here.")]
    public GameObject closedObject;
    public GameObject openObject;

    [Header("Hover Arrow (click this to open)")]
    public Sprite arrowSprite;         // optional; auto-generated if empty
    public Vector2 arrowSize   = new Vector2(34, 34);
    public Vector2 arrowOffset = new Vector2(0f, 60f);
    public float arrowBobAmplitude = 10f;
    public float arrowBobSpeed     = 2.2f;
    public Color arrowColor        = new Color(1f,1f,1f,0.95f);

    [Header("After Open")]
    public bool disableRaycastAfterOpen = true;

    // ---------- XP Toast Message (NEW) ----------
    [Header("XP Toast Message")]
    public bool showXPToast = true;
    [Range(0, 100000)] public int xpAmount = 40;    // set to 40 by default
    public float toastDuration = 1.3f;
    public int toastFontSize = 28;
    public Color toastColor = new Color(1f, 1f, 1f, 1f);
    public Vector2 toastOffset = new Vector2(0f, 90f); // px above chest
    public Vector2 toastRise   = new Vector2(0f, 40f); // px rise while fading
    // -------------------------------------------

    // internals
    RectTransform self, canvasRect;
    Canvas rootCanvas;
    Image arrowImg;
    bool opened;

    static T FindOne<T>() where T : Object
    {
    #if UNITY_2022_2_OR_NEWER
        return Object.FindFirstObjectByType<T>();
    #else
        return Object.FindObjectOfType<T>();
    #endif
    }

    void Awake()
    {
        self = GetComponent<RectTransform>();
        if (!chestImage) chestImage = GetComponent<Image>();

        rootCanvas = GetComponentInParent<Canvas>();
        if (!rootCanvas) rootCanvas = FindOne<Canvas>();
        if (rootCanvas) canvasRect = rootCanvas.transform as RectTransform;

        if (!arrowSprite) arrowSprite = MakeArrowSprite();

        SetClosedVisual();
    }

    void OnEnable()
    {
        if (!opened) CreateHoverArrow();
    }

    void OnDisable()
    {
        if (arrowImg) Destroy(arrowImg.gameObject);
    }

    // ---- Arrow click entry (one-shot) ----
    void OnArrowClicked()
    {
        if (opened) return;
        opened = true;

        if (arrowImg) Destroy(arrowImg.gameObject);

        SetOpenVisual();

        // Progress +%
        var bar = FindOne<PanelProgressBar>();
        if (bar)
        {
            float add = bar.max * (progressPercent / 100f);
            bar.SetValue(Mathf.Min(bar.value + add, bar.max));
        }

        // XP Toast (NEW)
        if (showXPToast) ShowXPToast();

        if (disableRaycastAfterOpen && chestImage)
        {
            chestImage.raycastTarget = false;
        }
    }

    // ---- Visual helpers ----
    void SetClosedVisual()
    {
        if (chestImage && closedSprite) chestImage.sprite = closedSprite;
        if (closedObject) closedObject.SetActive(true);
        if (openObject)   openObject.SetActive(false);
    }

    void SetOpenVisual()
    {
        if (chestImage && openSprite) chestImage.sprite = openSprite;
        if (closedObject) closedObject.SetActive(false);
        if (openObject)   openObject.SetActive(true);
    }

    // ---- Hovering arrow (clickable) ----
    void CreateHoverArrow()
    {
        if (!canvasRect) return;

        var go = new GameObject("ChestHoverArrow",
            typeof(RectTransform), typeof(CanvasRenderer), typeof(Image),
            typeof(ArrowWobble), typeof(ArrowClickable));

        go.transform.SetParent(self, false);
        go.transform.SetAsLastSibling();

        var rt  = go.GetComponent<RectTransform>();
        var img = go.GetComponent<Image>();
        img.sprite = arrowSprite;
        img.color  = arrowColor;
        img.raycastTarget = true; // clickable

        rt.sizeDelta = arrowSize;
        rt.anchoredPosition = arrowOffset;

        var wobble = go.GetComponent<ArrowWobble>();
        wobble.amplitude = arrowBobAmplitude;
        wobble.speed     = arrowBobSpeed;

        var clickable = go.GetComponent<ArrowClickable>();
        clickable.owner = this;

        arrowImg = img;
    }

    // ---- XP Toast (NEW) ----
    void ShowXPToast()
    {
        if (!canvasRect) return;

        var go = new GameObject("ChestXPToast", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        go.transform.SetParent(canvasRect, false);
        go.transform.SetAsLastSibling();

        var rt  = go.GetComponent<RectTransform>();
        var tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.text = $"You collected {xpAmount} XP!";
        tmp.fontSize = toastFontSize;
        tmp.color = toastColor;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;

        // position above chest
        var cam = Camera.main;
        Vector2 screen = RectTransformUtility.WorldToScreenPoint(rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : cam, self.position);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screen,
            rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : cam, out var anchored);

        rt.sizeDelta = new Vector2(360f, 80f);
        rt.anchoredPosition = anchored + toastOffset;

        StartCoroutine(RiseAndFadeTMP(rt, tmp, toastDuration, toastRise));
    }

    System.Collections.IEnumerator RiseAndFadeTMP(RectTransform rt, TextMeshProUGUI tmp, float dur, Vector2 rise)
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

    // ---- Procedural ▲ arrow if none assigned ----
    Sprite MakeArrowSprite(int w=48, int h=48, float ppu=64f)
    {
        var tex = new Texture2D(w, h, TextureFormat.ARGB32, false);
        tex.wrapMode = TextureWrapMode.Clamp; tex.filterMode = FilterMode.Bilinear;

        for (int y = 0; y < h; y++)
        {
            float t = (float)y / (h - 1);
            int half = Mathf.RoundToInt((w * (1f - t)) * 0.5f);
            int cx = w / 2;
            for (int x = 0; x < w; x++)
            {
                bool inside = x >= cx - half && x <= cx + half;
                tex.SetPixel(x, y, inside
                    ? new Color(1,1,1, Mathf.SmoothStep(0,1, 1f - Mathf.Abs((x - cx)/(float)Mathf.Max(1,half))))
                    : Color.clear);
            }
        }
        tex.Apply(false, false);
        return Sprite.Create(tex, new Rect(0,0,w,h), new Vector2(0.5f,0.2f), ppu);
    }

    // ---- tiny helpers (nested) ----
    private class ArrowWobble : MonoBehaviour
    {
        public float amplitude = 10f, speed = 2.2f;
        RectTransform rt; Vector2 basePos; float t;
        void Awake(){ rt = GetComponent<RectTransform>(); basePos = rt.anchoredPosition; }
        void Update(){ t += Time.unscaledDeltaTime;
            rt.anchoredPosition = basePos + new Vector2(0, Mathf.Sin(t*speed*2f*Mathf.PI)*amplitude); }
    }

    private class ArrowClickable : MonoBehaviour, IPointerClickHandler
    {
        public ChestOpenUI owner;
        public void OnPointerClick(PointerEventData e){ if (owner) owner.OnArrowClicked(); }
    }
}
