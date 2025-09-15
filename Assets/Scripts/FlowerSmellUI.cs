using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections;

[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public class FlowerSmellUI : MonoBehaviour, IPointerClickHandler
{
    // ---------- Progress (explicit hookup) ----------
    [Header("Progress")]
    [Range(0,100f)] public float progressPercent = 5f;

    [Header("Progress Target")]
    [SerializeField] private PanelProgressBar progressBar;             // ← drag WalkProgressBar here
    [SerializeField] private string progressBarName = "WalkProgressBar"; // optional exact name fallback

    // ---------- XP Toast ----------
    [Header("XP Toast Message")]
    public bool showXPToast = true;
    [Range(0,100000)] public int xpAmount = 5;
    public float toastDuration = 1.1f;
    public int toastFontSize = 26;
    public Color toastColor = new Color(1f,1f,1f,1f);
    public Vector2 toastOffset = new Vector2(0f, 80f);
    public Vector2 toastRise   = new Vector2(0f, 36f);

    // ---------- Timing / Movement ----------
    [Header("Timing")]
    public float approachSeconds = 0.2f;
    public float sniffSeconds    = 0.5f;

    [Header("World Offsets")]
    public Vector2 playerOffset = new Vector2(-0.2f, 0f);
    public Vector2 petOffset    = new Vector2( 0.5f, 0f);

    // ---------- Pet Hearts ----------
    [Header("Hearts (on pet)")]
    public Sprite heartSprite;
    public int    heartCount       = 5;
    public float  heartDuration    = 0.7f;
    public float  heartRise        = 1.2f;
    public Vector2 heartSpread     = new Vector2(0.4f, 0.6f);
    public int    heartSortingOrder = 1000;

    // ---------- Hover Arrow ----------
    [Header("Hover Arrow (prompt)")]
    public bool   showHoverArrow = true;
    public Sprite arrowSprite; // auto if null
    public Vector2 hoverArrowSize   = new Vector2(34, 34);
    public Vector2 hoverArrowOffset = new Vector2(0f, 60f);
    public float  hoverBobAmplitude = 10f;
    public float  hoverBobSpeed     = 2.2f;
    public Color  hoverArrowColor   = new Color(1f,1f,1f,0.95f);

    // ---------- Triangle Wind FX ----------
    [Header("Triangle Wind (plays when clicked)")]
    public Sprite triangleSprite; // auto if null
    public int    trianglesCount        = 10;
    public float  trianglesDuration     = 1.0f;
    public Vector2 trianglesRiseRange   = new Vector2(45f, 80f);
    public Vector2 trianglesDriftX      = new Vector2(-30f, 30f);
    public Vector2 trianglesSpinDegPerSec = new Vector2(-180f, 180f);
    public Color  trianglesColorA       = Color.white;
    public Color  trianglesColorB       = new Color(1f,1f,1f,0.8f);
    public Vector2 trianglesScaleRange  = new Vector2(0.6f, 1.0f);

    // ---------- internals ----------
    RectTransform self, canvasRect;
    Canvas rootCanvas;
    Image hoverArrowImg;
    bool busy, consumed;

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
        rootCanvas = GetComponentInParent<Canvas>(); if (!rootCanvas) rootCanvas = FindOne<Canvas>();
        if (rootCanvas) canvasRect = rootCanvas.transform as RectTransform;

        if (!arrowSprite)    arrowSprite    = MakeArrowSprite();
        if (!triangleSprite) triangleSprite = MakeTriangleSprite();

        // Optional: auto bind by name if not assigned
        if (!progressBar && !string.IsNullOrEmpty(progressBarName))
        {
            var go = GameObject.Find(progressBarName);
            if (go) progressBar = go.GetComponent<PanelProgressBar>();
        }
    }

    void OnEnable(){ if (showHoverArrow && !consumed) CreateHoverArrow(); }
    void OnDisable(){ if (hoverArrowImg) Destroy(hoverArrowImg.gameObject); }

    // ---------- Click on the flower image ----------
    public void OnPointerClick(PointerEventData e)
    {
        if (busy || consumed) return;

        var cam = Camera.main;
        var worldPos = cam.ScreenToWorldPoint(new Vector3(e.position.x, e.position.y, -cam.transform.position.z));
        worldPos.z = 0f;

        TriggerOnce(worldPos);
    }

    // ---------- Click via the hover arrow ----------
    void OnArrowClicked()
    {
        if (busy || consumed) return;

        var cam = Camera.main;
        Vector2 screenCenter = RectTransformUtility.WorldToScreenPoint(
            rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : cam, self.position);
        var worldPos = cam.ScreenToWorldPoint(new Vector3(screenCenter.x, screenCenter.y, -cam.transform.position.z));
        worldPos.z = 0f;

        TriggerOnce(worldPos);
    }

    // ---------- Single entry after click ----------
    void TriggerOnce(Vector3 worldPos)
    {
        consumed = true; // one-shot
        if (hoverArrowImg) Destroy(hoverArrowImg.gameObject);

        var img = GetComponent<Image>();
        if (img) img.raycastTarget = false;

        PlayTriangleWindBurst();
        StartCoroutine(SniffRoutine(worldPos));
    }

    IEnumerator SniffRoutine(Vector3 worldPos)
    {
        busy = true;

        // No markers/tags: find by component or name
        Transform pTr   = FindPlayerNoTags();
        Transform petTr = FindPetNoTags(); // optional
        if (!pTr) { busy = false; yield break; }

        Vector3 pStart   = pTr.position,   pTarget   = worldPos + (Vector3)playerOffset;
        Vector3 petStart = petTr ? petTr.position : Vector3.zero;
        Vector3 petTarget= worldPos + (Vector3)petOffset;

        float t = 0f;
        while (t < approachSeconds)
        {
            t += Time.deltaTime; float a = Mathf.Clamp01(t / approachSeconds);
            pTr.position = Vector3.Lerp(pStart, pTarget, a);
            if (petTr) petTr.position = Vector3.Lerp(petStart, petTarget, a);
            yield return null;
        }

        if (petTr && heartSprite) StartCoroutine(BurstHearts(petTr.position + Vector3.up * 0.3f));

        // ---- Progress +% (explicit bar first, then fallback) ----
        var bar = ResolveProgressBar();
        if (bar)
        {
            float add = bar.max * (progressPercent / 100f);
            bar.SetValue(Mathf.Min(bar.value + add, bar.max));
        }

        // XP toast
        if (showXPToast) ShowXPToast();

        yield return new WaitForSeconds(sniffSeconds);
        busy = false;
    }

    PanelProgressBar ResolveProgressBar()
    {
        if (progressBar) return progressBar;

        if (!string.IsNullOrEmpty(progressBarName))
        {
            var go = GameObject.Find(progressBarName);
            if (go)
            {
                progressBar = go.GetComponent<PanelProgressBar>();
                if (progressBar) return progressBar;
            }
        }

        // Prefer a bar under the same canvas
        if (canvasRect)
        {
            var localBars = canvasRect.GetComponentsInChildren<PanelProgressBar>(true);
            if (localBars != null && localBars.Length > 0)
            {
                // if a name is given, try to match it
                if (!string.IsNullOrEmpty(progressBarName))
                {
                    foreach (var b in localBars) if (b.name == progressBarName) { progressBar = b; return b; }
                }
                progressBar = localBars[0];
                return progressBar;
            }
        }

        // Last resort: any bar
        progressBar = FindOne<PanelProgressBar>();
        return progressBar;
    }

    // ---------- XP Toast ----------
    void ShowXPToast()
    {
        if (!canvasRect) return;

        var go = new GameObject("FlowerXPToast", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        go.transform.SetParent(canvasRect, false);
        go.transform.SetAsLastSibling();

        var rt  = go.GetComponent<RectTransform>();
        var tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.text = $"You collected {xpAmount} XP!";
        tmp.fontSize = toastFontSize;
        tmp.color = toastColor;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;

        var cam = Camera.main;
        Vector2 screen = RectTransformUtility.WorldToScreenPoint(
            rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : cam, self.position);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect, screen,
            rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : cam,
            out var anchored);

        rt.sizeDelta = new Vector2(320f, 72f);
        rt.anchoredPosition = anchored + toastOffset;
        StartCoroutine(RiseAndFadeTMP(rt, tmp, toastDuration, toastRise));
    }

    IEnumerator RiseAndFadeTMP(RectTransform rt, TextMeshProUGUI tmp, float dur, Vector2 rise)
    {
        float t = 0f; Vector2 start = rt.anchoredPosition; Vector2 end = start + rise; Color c0 = tmp.color;
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

    // ---------- Hearts over pet ----------
    IEnumerator BurstHearts(Vector3 at)
    {
        for (int i = 0; i < heartCount; i++)
        {
            var go = new GameObject("heart");
            go.transform.position = at + (Vector3)new Vector2(
                Random.Range(-heartSpread.x, heartSpread.x),
                Random.Range(-0.1f, 0.1f));
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = heartSprite; sr.sortingOrder = heartSortingOrder;

            Vector3 s = go.transform.position, e = s + Vector3.up * heartRise; float t = 0f;
            while (t < heartDuration)
            {
                t += Time.deltaTime;
                float a = Mathf.Clamp01(t / heartDuration);
                go.transform.position = Vector3.Lerp(s, e, a);
                var c = sr.color; c.a = 1f - a; sr.color = c;
                yield return null;
            }
            Destroy(go);
        }
    }

    // ---------- Triangle wind (UI) ----------
    void PlayTriangleWindBurst()
    {
        if (!canvasRect) return;

        var cam = Camera.main;
        Vector2 screenCenter = RectTransformUtility.WorldToScreenPoint(
            rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : cam, self.position);

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect, screenCenter,
            rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : cam,
            out var baseAnchored);

        for (int i = 0; i < trianglesCount; i++)
        {
            var go = new GameObject("TriangleWind",
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(canvasRect, false);
            go.transform.SetAsLastSibling();

            var rt  = go.GetComponent<RectTransform>();
            var img = go.GetComponent<Image>();
            img.sprite = triangleSprite;
            img.color  = Color.Lerp(trianglesColorA, trianglesColorB, Random.value);
            img.raycastTarget = false;

            float scale = Random.Range(trianglesScaleRange.x, trianglesScaleRange.y);
            rt.localScale = new Vector3(scale, scale, 1f);
            rt.sizeDelta = new Vector2(16, 16);

            Vector2 start = baseAnchored + new Vector2(Random.Range(-10f,10f), Random.Range(10f,25f));
            rt.anchoredPosition = start;

            float rise  = Random.Range(trianglesRiseRange.x, trianglesRiseRange.y);
            float drift = Random.Range(trianglesDriftX.x, trianglesDriftX.y);
            float spin  = Random.Range(trianglesSpinDegPerSec.x, trianglesSpinDegPerSec.y);

            StartCoroutine(TriangleFloat(rt, img, rise, drift, spin));
        }
    }

    IEnumerator TriangleFloat(RectTransform rt, Graphic g, float rise, float driftX, float spinDegPerSec)
    {
        float t = 0f; Vector2 start = rt.anchoredPosition; Vector2 end = start + new Vector2(driftX, rise); Color c0 = g.color;
        while (t < trianglesDuration)
        {
            t += Time.unscaledDeltaTime;
            float a = Mathf.Clamp01(t / trianglesDuration);
            rt.anchoredPosition = Vector2.Lerp(start, end, a);
            g.color = new Color(c0.r, c0.g, c0.b, 1f - a);
            rt.rotation = Quaternion.Euler(0,0, rt.rotation.eulerAngles.z + spinDegPerSec * Time.unscaledDeltaTime);
            yield return null;
        }
        if (rt) Destroy(rt.gameObject);
    }

    // ---------- Hover arrow ----------
    void CreateHoverArrow()
    {
        if (!canvasRect) return;

        var go = new GameObject("HoverArrow",
            typeof(RectTransform), typeof(CanvasRenderer), typeof(Image),
            typeof(ArrowWobble), typeof(ArrowClickable));
        go.transform.SetParent(self, false);
        go.transform.SetAsLastSibling();

        var rt  = go.GetComponent<RectTransform>();
        var img = go.GetComponent<Image>();
        img.sprite = arrowSprite; img.color = hoverArrowColor; img.raycastTarget = true;
        rt.sizeDelta = hoverArrowSize; rt.anchoredPosition = hoverArrowOffset;

        var wobble = go.GetComponent<ArrowWobble>(); wobble.amplitude = hoverBobAmplitude; wobble.speed = hoverBobSpeed;
        var click  = go.GetComponent<ArrowClickable>(); click.owner = this;

        hoverArrowImg = img;
    }

    // ---------- No-tag, no-marker lookups ----------
    Transform FindPlayerNoTags()
    {
        var pc = FindOne<PlayerController>();
        if (pc) return pc.transform;
        var go = GameObject.Find("Player");
        return go ? go.transform : null;
    }

    Transform FindPetNoTags()
    {
        var f = FindOne<PetFollower>();
        if (f) return f.transform;
        var go = GameObject.Find("Pet");
        return go ? go.transform : null;
    }

    // ---------- Procedural sprites ----------
    Sprite MakeArrowSprite(int w=48, int h=48, float ppu=64f)
    {
        var tex = new Texture2D(w,h,TextureFormat.ARGB32,false);
        tex.wrapMode = TextureWrapMode.Clamp; tex.filterMode = FilterMode.Bilinear;
        for (int y=0; y<h; y++)
        {
            float t = (float)y/(h-1); int half = Mathf.RoundToInt((w*(1f-t))*0.5f); int cx=w/2;
            for (int x=0; x<w; x++)
            {
                bool inside = x>=cx-half && x<=cx+half;
                float a = inside ? Mathf.SmoothStep(0,1,1f - Mathf.Abs((x-cx)/(float)Mathf.Max(1,half))) : 0f;
                tex.SetPixel(x,y, new Color(1,1,1, a));
            }
        }
        tex.Apply(false,false);
        return Sprite.Create(tex, new Rect(0,0,w,h), new Vector2(0.5f,0.2f), ppu);
    }

    Sprite MakeTriangleSprite(int w=24, int h=24, float ppu=64f)
    {
        var tex = new Texture2D(w,h,TextureFormat.ARGB32,false);
        tex.wrapMode = TextureWrapMode.Clamp; tex.filterMode = FilterMode.Bilinear;
        for (int y=0; y<h; y++)
        {
            float t = (float)y/(h-1); int half = Mathf.RoundToInt((w*(1f-t))*0.5f); int cx=w/2;
            for (int x=0; x<w; x++)
            {
                bool inside = x>=cx-half && x<=cx+half;
                if (!inside){ tex.SetPixel(x,y, Color.clear); continue; }
                float dx = Mathf.InverseLerp(cx-half, cx+half, x);
                float edge = Mathf.Min(dx, 1f-dx);
                float alpha = Mathf.SmoothStep(0f, 1f, Mathf.Lerp(0.0f, 1.0f, edge*2f)) * Mathf.Lerp(0.6f, 1f, t);
                tex.SetPixel(x,y, new Color(1f,1f,1f, alpha));
            }
        }
        tex.Apply(false,false);
        return Sprite.Create(tex, new Rect(0,0,w,h), new Vector2(0.5f,0.1f), ppu);
    }

    // ---------- tiny nested helpers ----------
    private class ArrowWobble : MonoBehaviour
    {
        public float amplitude=10f, speed=2.2f; RectTransform rt; Vector2 basePos; float t;
        void Awake(){ rt = GetComponent<RectTransform>(); basePos = rt.anchoredPosition; }
        void Update(){ t += Time.unscaledDeltaTime;
            rt.anchoredPosition = basePos + new Vector2(0, Mathf.Sin(t*speed*2f*Mathf.PI)*amplitude); }
    }
    private class ArrowClickable : MonoBehaviour, IPointerClickHandler
    {
        public FlowerSmellUI owner;
        public void OnPointerClick(PointerEventData e){ if (owner) owner.OnArrowClicked(); }
    }
}
