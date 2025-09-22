using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class CleanUIManager : MonoBehaviour
{
    [Header("UI Refs")]
    public Canvas canvas;
    public PanelProgressBar cleanBar;    // seeds global once
    public RectTransform petRect;

    [Header("FX (optional)")]
    public PetEmotionFX petFX;
    public Sprite bubbleSprite; // optional; fallback is 1x1 white created in code

    [Header("Popup at exactly 50%")]
    [Tooltip("Listens to PetNeedsManager.OnCleanHit50 (fires only when Clean becomes exactly 50).\n" +
             "If GlobalNotifier is auto-subscribing, we do NOT also subscribe here.")]
    public bool ensure50Popup = true;
    public string clean50Message = "Time to clean your pet!";

    bool _subscribed50;

    void Awake()
    {
        if (!canvas) canvas = GetComponentInParent<Canvas>();

        // Seed global once from the bar (safe no-op if already persisted/seeded)
        var mgr = PetNeedsManager.Instance;
        if (mgr != null && cleanBar != null)
            mgr.InitializeCleanIfUnset(cleanBar.value);
    }

    void OnEnable()  { TrySubscribe50(); }

    void Start()
    {
        // Bind pet image if available
        if (petRect == null)
        {
            var petImage = FindFirstObjectByType<PetImage>();
            if (petImage != null)
            {
                petRect = petImage.RectTransform;

                var sel = PetSelectionManager.instance;
                if (sel != null && sel.currentPet != null && sel.currentPet.cardImage != null)
                    petImage.SetPet(sel.currentPet.cardImage);
            }
        }
        else
        {
            var petImage = petRect.GetComponent<PetImage>();
            var sel = PetSelectionManager.instance;
            if (petImage != null && sel != null && sel.currentPet != null && sel.currentPet.cardImage != null)
                petImage.SetPet(sel.currentPet.cardImage);
        }
    }

    void OnDisable() { Unsubscribe50(); }
    void OnDestroy() { Unsubscribe50(); }

    // ---- 50% popup wiring (without duplicates) ----
    void TrySubscribe50()
    {
        if (_subscribed50 || !ensure50Popup) return;

        var needs = PetNeedsManager.Instance;
        if (needs == null) return;

        // Avoid duplicate toasts if a global notifier is already handling subscriptions
        var notifier = GlobalNotifier.Instance;
        if (notifier != null && notifier.autoSubscribe) return;

        needs.OnCleanHit50.AddListener(OnCleanHit50);
        _subscribed50 = true;
    }

    void Unsubscribe50()
    {
        if (!_subscribed50) return;
        var needs = PetNeedsManager.Instance;
        if (needs != null) needs.OnCleanHit50.RemoveListener(OnCleanHit50);
        _subscribed50 = false;
    }

    void OnCleanHit50()
    {
        var gn = GlobalNotifier.Instance;
        if (gn != null) gn.ShowToast(clean50Message, gn.toastHoldSeconds);
    }
    // -----------------------------------------------

    public bool CanCleanNow()
    {
        var mgr = PetNeedsManager.Instance;
        // If global exists and is full, cannot clean. If global missing, allow (fallback path)
        return !(mgr != null && mgr.IsCleanFull());
    }

    public void Clean(DraggableCleanItem item)
    {
        if (item == null || canvas == null || petRect == null) return;
        if (!CanCleanNow()) return;

        StartCoroutine(CleanRoutine(item));
    }

    IEnumerator CleanRoutine(DraggableCleanItem item)
    {
        var itemRT = item.GetComponent<RectTransform>();
        if (!itemRT) yield break;

        // ghost fly-in
        var ghost  = new GameObject("CleanGhost", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
        var gRT    = ghost.GetComponent<RectTransform>();
        var gImg   = ghost.GetComponent<Image>();
        gRT.SetParent(canvas.transform, false);
        gRT.position  = itemRT.position;
        gRT.sizeDelta = itemRT.sizeDelta;
        gImg.sprite   = (item.image != null) ? item.image.sprite : null;
        gImg.preserveAspect = true;

        float t = 0f, dur = 0.35f;
        Vector3 a = gRT.position;
        Vector3 b = petRect.position + new Vector3(0, petRect.rect.height * 0.1f, 0);
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float k = t / dur;
            gRT.position   = Vector3.Lerp(a, b, k);
            gRT.localScale = Vector3.one * (1f - 0.2f * k);
            yield return null;
        }

        yield return StartCoroutine(FoamBurst(gRT.position, gRT.sizeDelta * 0.25f));
        Destroy(ghost);
        item.gameObject.SetActive(false);

        // Global-first award (percent points)
        var mgr = PetNeedsManager.Instance;
        if (mgr != null)
        {
            if (!mgr.IsCleanFull())
                mgr.AddCleanPercent(item.cleanPower);   // cleanPower is % points
        }
        else if (cleanBar != null) // fallback if no global
        {
            cleanBar.SetValue(Mathf.Min(100f, cleanBar.value + item.cleanPower));
        }

        if (petFX) petFX.PlayHappy();
    }

    IEnumerator FoamBurst(Vector3 center, Vector2 size)
    {
        int n = 6;
        var sprite = bubbleSprite != null ? bubbleSprite : DefaultUISprite();
        for (int i = 0; i < n; i++)
        {
            var bubble = new GameObject("bubble", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
            var rt  = bubble.GetComponent<RectTransform>();
            var img = bubble.GetComponent<Image>();
            var cg  = bubble.GetComponent<CanvasGroup>();
            rt.SetParent(canvas.transform, false);
            rt.position  = center + (Vector3)Random.insideUnitCircle * 10f;
            rt.sizeDelta = size * Random.Range(0.7f, 1.2f);
            img.sprite   = sprite;
            img.type     = Image.Type.Sliced;
            img.color    = new Color(1f,1f,1f,0.75f);
            StartCoroutine(RiseFade(rt, cg));
        }
        yield return new WaitForSecondsRealtime(0.25f);
    }

    static Sprite DefaultUISprite()
    {
        var tex = Texture2D.whiteTexture; // 1x1
        return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
    }

    IEnumerator RiseFade(RectTransform rt, CanvasGroup cg)
    {
        float t = 0f, dur = 0.45f;
        Vector3 a = rt.position, b = a + new Vector3(Random.Range(-15f, 15f), 60f, 0);
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float k = t / dur;
            rt.position   = Vector3.Lerp(a, b, k);
            rt.localScale = Vector3.one * (1f + 0.3f * k);
            if (cg) cg.alpha = 1f - k;
            yield return null;
        }
        Destroy(rt.gameObject);
    }
}
