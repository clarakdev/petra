using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

[RequireComponent(typeof(RectTransform))]
public class ChestOpenUI : MonoBehaviour, IPointerClickHandler
{
    // ---------- Progress ----------
    [Header("Progress")]
    [Range(0,100f)] public float progressPercent = 40f; // +40%

    // ---------- Click to Open ----------
    [Header("Click to Open")]
    [Tooltip("If true, clicking the chest opens it and grants XP.")]
    public bool openOnClick = true;

    // ---------- Visuals: Single Image mode ----------
    [Header("Single Image Mode")]
    [Tooltip("Assign if you want to swap sprites on ONE Image.")]
    public Image chestImage;           // optional; auto-grab if null
    public Sprite closedSprite;        // closed art
    public Sprite openSprite;          // open art

    // ---------- Visuals: Dual Object mode ----------
    [Header("Dual Object Mode (use this OR Single Image)")]
    [Tooltip("If you have two separate objects (one with closed art, one with open art), assign them here.")]
    public GameObject closedObject;
    public GameObject openObject;

    // ---------- Proximity Open ----------
    [Header("Open by Proximity (no click)")]
    public bool autoOpenOnProximity = true;
    public float playerPixelRange   = 140f;     // try 140–220 on high DPI
    public bool  requirePetNear     = false;
    public float petPixelRange      = 140f;

    [Header("References (optional – auto if empty)")]
    public Transform player;                    // Player (Transform)
    public Transform pet;                       // Pet (Transform, optional)
    public Canvas    uiCanvas;                  // HUD canvas for toasts/hearts
    public PanelProgressBar progressBar;        // drag a bar, or it will FindObjectOfType

    // ---------- Hover Arrow (optional hint) ----------
    [Header("Hover Arrow (optional hint)")]
    public bool   showHoverArrow = false;       // set true if you still want a hint arrow
    public Sprite arrowSprite;                  // optional; auto-generated if empty
    public Vector2 arrowSize   = new Vector2(34, 34);
    public Vector2 arrowOffset = new Vector2(0f, 60f);
    public float  arrowBobAmplitude = 10f;
    public float  arrowBobSpeed     = 2.2f;
    public Color  arrowColor        = new Color(1f,1f,1f,0.95f);

    // ---------- Hearts (UI burst) ----------
    [Header("Hearts (UI burst when opened)")]
    public Sprite heartSprite;                  // optional PNG heart; if null uses "♥"
    public int    heartCount = 10;
    public Vector2 heartSizeRange = new Vector2(28, 46);
    public float  heartDuration = 0.9f;
    public Vector2 heartRise    = new Vector2(60f, 110f);
    public Vector2 heartDriftX  = new Vector2(-35f, 35f);
    public Color  heartColor    = new Color(1f, 0.5f, 0.7f, 1f);

    // ---------- XP Toast ----------
    [Header("XP Toast Message")]
    public bool showXPToast = true;
    [Range(0, 100000)] public int xpAmount = 40;
    public float toastDuration = 1.3f;
    public int   toastFontSize = 35;
    public Color toastColor = new Color(1f, 1f, 1f, 1f);
    public Vector2 toastOffset = new Vector2(0f, 90f);
    public Vector2 toastRise   = new Vector2(0f, 40f);

    // ---------- Behaviour ----------
    [Header("Behaviour")]
    public bool oneShot = true;                 // only open once
    public float cooldownSeconds = 0f;          // if >0, can re-open after cooldown
    public bool disableRaycastAfterOpen = true; // makes round-managers detect "consumed"

    // ---------- internals ----------
    RectTransform self, canvasRect;
    Canvas rootCanvas;
    Image  arrowImg;
    Camera cam;
    bool   opened;
    float  cooldownUntil = -1f;

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

        rootCanvas = GetComponentInParent<Canvas>(); if (!rootCanvas) rootCanvas = FindOne<Canvas>();
        if (rootCanvas) canvasRect = rootCanvas.transform as RectTransform;
        cam = Camera.main;

        if (!arrowSprite) arrowSprite = MakeArrowSprite();

        if (!progressBar) progressBar = FindOne<PanelProgressBar>();
        if (!uiCanvas)    uiCanvas    = FindOne<Canvas>();

        // Auto-find player/pet if not wired
        if (!player)
        {
            var pc = FindOne<PlayerController>();
            player = pc ? pc.transform : GameObject.Find("Player")?.transform;
        }
        if (!pet && requirePetNear)
        {
            var pf = FindOne<PetFollower>();
            pet = pf ? pf.transform : GameObject.Find("Pet")?.transform;
        }

        // ensure clicks hit us when openOnClick is true
        if (openOnClick && chestImage) chestImage.raycastTarget = true;

        SetClosedVisual();
    }

    void OnEnable()
    {
        if (!opened && showHoverArrow) CreateHoverArrow();
    }

    void OnDisable()
    {
        if (arrowImg) Destroy(arrowImg.gameObject);
    }

    void Update()
    {
        if (!autoOpenOnProximity || opened) return;
        if (oneShot && cooldownUntil > 0f) return;           // already opened (oneshot)
        if (Time.unscaledTime < cooldownUntil) return;       // waiting cooldown
        if (!player || !cam) return;

        // chest center in screen space
        Vector2 chestScreen = RectTransformUtility.WorldToScreenPoint(
            rootCanvas && rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : cam, self.position);

        // player distance
        Vector2 playerScreen = cam.WorldToScreenPoint(player.position);
        bool playerNear = Vector2.Distance(chestScreen, playerScreen) <= playerPixelRange;

        // pet distance (optional)
        bool petNear = true;
        if (requirePetNear)
        {
            if (!pet) return;
            Vector2 petScreen = cam.WorldToScreenPoint(pet.position);
            petNear = Vector2.Distance(chestScreen, petScreen) <= petPixelRange;
        }

        if (playerNear && petNear)
        {
            OpenChest();
            if (oneShot) cooldownUntil = float.PositiveInfinity;     // never again
            else if (cooldownSeconds > 0f) cooldownUntil = Time.unscaledTime + cooldownSeconds;
        }
    }

    // CLICK support
    public void OnPointerClick(PointerEventData eventData)
    {
        if (!openOnClick) return;
        OpenChest();
    }

    // ---- Main open entry (also used by optional arrow click) ----
    void OpenChest()
    {
        if (opened) return;
        opened = true;

        if (arrowImg) Destroy(arrowImg.gameObject);
        SetOpenVisual();

        // Award progress to WALK (global → manager → fallback to bar)
        bool awarded = false;

        var needs = PetNeedsManager.Instance;
        if (needs != null && !needs.IsWalkFull())
        {
            needs.AddWalkPercent(progressPercent); // respects 100% lockout
            awarded = true;
        }

        if (!awarded)
        {
            var walkMgr = FindOne<WalkUIManager>();
            if (walkMgr != null)
            {
                walkMgr.AddXPPercent(progressPercent);      // manager handles lockout
                awarded = true;
            }
        }

        if (!awarded && progressBar != null)
        {
            // last-resort: push raw UI bar (no lockout logic here)
            float add = progressBar.max * (progressPercent / 100f);
            progressBar.SetValue(Mathf.Min(progressBar.value + add, progressBar.max));
        }

        // Hearts burst (UI)
        if (uiCanvas) StartCoroutine(HeartsBurstUI());

        // XP Toast
        if (showXPToast) ShowXPToast();

        // mark as consumed for round managers
        if (disableRaycastAfterOpen && chestImage) chestImage.raycastTarget = false;
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

    // ---- Hovering arrow (optional hint) ----
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
        img.raycastTarget = true;

        rt.sizeDelta = arrowSize;
        rt.anchoredPosition = arrowOffset;

        var wobble = go.GetComponent<ArrowWobble>();
        wobble.amplitude = arrowBobAmplitude;
        wobble.speed     = arrowBobSpeed;

        var clickable = go.GetComponent<ArrowClickable>();
        clickable.owner = this;

        arrowImg = img;
    }

    // ---- Hearts (UI) ----
    System.Collections.IEnumerator HeartsBurstUI()
    {
        if (!uiCanvas) yield break;

        Vector2 anchored = GetAnchoredInCanvas();

        for (int i = 0; i < heartCount; i++)
        {
            var go = new GameObject("HeartUI", typeof(RectTransform), typeof(CanvasRenderer));
            go.transform.SetParent(uiCanvas.transform, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchoredPosition = anchored + new Vector2(Random.Range(-18f, 18f), Random.Range(-6f, 6f));

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
        Vector2 start = rt.anchoredPosition, end = start + move;
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

    // ---- XP Toast ----
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
        rt.sizeDelta = new Vector2(360f, 80f);
        rt.anchoredPosition = GetAnchoredInCanvas() + toastOffset;

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

    // ---- Helpers ----
    Vector2 GetAnchoredInCanvas()
    {
        var cameraForUI = (rootCanvas && rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay) ? null : cam;
        Vector2 screen = RectTransformUtility.WorldToScreenPoint(cameraForUI, self.position);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect, screen,
            rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : cam,
            out var anchored);
        return anchored;
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

    // ---- tiny nested helpers for the optional arrow ----
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
        public void OnPointerClick(PointerEventData e){ if (owner && !owner.opened) owner.OpenChest(); }
    }
}
