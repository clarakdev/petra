using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

public class FeedUIManager : MonoBehaviour
{
    [Header("UI Refs")]
    public Canvas canvas;
    public PanelProgressBar hungerBar;    // only seeds global once
    public RectTransform petRect;

    [Header("Drain (local scene – leave 0)")]
    public float hungerDecayPerMinute = 0f; // not used; global owns decay
    public float fullPauseMinutes     = 20f; // legacy; global handles 100% pause

    [Header("FX (optional)")]
    public PetEmotionFX petFX;

    [Header("Popup at 50%")]
    [Tooltip("If true, this script ensures a toast when Feed crosses down to 50%. " +
             "It will only subscribe if GlobalNotifier is missing OR its autoSubscribe is OFF.")]
    public bool ensure50Popup = true;
    public string feed50Message = "Time to feed your pet!";

    bool _subscribed50;

    void Awake()
    {
        if (!canvas) canvas = GetComponentInParent<Canvas>();

        // Seed global once from the bar (safe no-op if already persisted/seeded)
        var mgr = PetNeedsManager.Instance;
        if (mgr != null && hungerBar != null)
            mgr.InitializeFeedIfUnset(hungerBar.value);
    }

    void OnEnable()  { TrySubscribe50(); }
    void Start()
    {
        // Auto-bind the pet image/rect and show selected pet sprite
        if (petRect == null)
        {
            var petImg = FindFirstObjectByType<PetImage>();
            if (petImg != null)
            {
                petRect = petImg.RectTransform;

                var sel = PetSelectionManager.instance;
                if (petImg != null && sel != null && sel.currentPet != null && sel.currentPet.cardImage != null)
                    petImg.SetPet(sel.currentPet.cardImage);
            }
        }
        else
        {
            var petImg = petRect.GetComponent<PetImage>();
            var sel = PetSelectionManager.instance;
            if (petImg != null && sel != null && sel.currentPet != null && sel.currentPet.cardImage != null)
                petImg.SetPet(sel.currentPet.cardImage);
        }
    }
    void OnDisable() { Unsubscribe50(); }
    void OnDestroy() { Unsubscribe50(); }

    // --- Ensure popup at 50% (without double-subscribing) ---
    void TrySubscribe50()
    {
        if (_subscribed50 || !ensure50Popup) return;

        var needs = PetNeedsManager.Instance;
        if (needs == null) return;

        // If GlobalNotifier is present and already auto-subscribing, do nothing (avoid double toasts)
        var notifier = GlobalNotifier.Instance;
        if (notifier != null && notifier.autoSubscribe) return;

        needs.OnFeedHit50.AddListener(OnFeedHit50);
        _subscribed50 = true;
    }

    void Unsubscribe50()
    {
        if (!_subscribed50) return;
        var needs = PetNeedsManager.Instance;
        if (needs != null) needs.OnFeedHit50.RemoveListener(OnFeedHit50);
        _subscribed50 = false;
    }

    void OnFeedHit50()
    {
        GlobalNotifier.Instance?.ShowToast(feed50Message, GlobalNotifier.Instance ? GlobalNotifier.Instance.toastHoldSeconds : 3f);
    }
    // --------------------------------------------------------

    public bool CanFeedNow()
    {
        var mgr = PetNeedsManager.Instance;
        // If global exists and is full, cannot feed. If global missing, allow (fallback path)
        return !(mgr != null && mgr.IsFeedFull());
    }

    public void Feed(DraggableFood food)
    {
        if (food == null || canvas == null || petRect == null) return;
        if (!CanFeedNow()) return;

        StartCoroutine(EatRoutine(food));
    }

    IEnumerator EatRoutine(DraggableFood food)
    {
        var foodRT = food.GetComponent<RectTransform>();
        if (foodRT == null) yield break;

        // ghost flies to pet
        var ghost = new GameObject("FoodGhost", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
        var gRT  = ghost.GetComponent<RectTransform>();
        var gImg = ghost.GetComponent<Image>();
        gRT.SetParent(canvas.transform, false);
        gRT.SetAsLastSibling();
        gRT.position  = foodRT.position;
        gRT.sizeDelta = foodRT.sizeDelta;
        gImg.sprite   = (food.image != null) ? food.image.sprite : null;
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

        yield return StartCoroutine(BiteBurst(gImg, gRT));
        Destroy(ghost);
        food.gameObject.SetActive(false);

        // Global-first award (percent points)
        var mgr = PetNeedsManager.Instance;
        if (mgr != null)
        {
            if (!mgr.IsFeedFull())
                mgr.AddFeedPercent(food.nutrition);   // nutrition is % points
        }
        else if (hungerBar != null) // fallback if no global
        {
            hungerBar.SetValue(Mathf.Min(100f, hungerBar.value + food.nutrition));
        }

        if (petFX) petFX.PlayHappy();
    }

    IEnumerator BiteBurst(Image srcImg, RectTransform at)
    {
        const int n = 5;
        for (int i = 0; i < n; i++)
        {
            var bit = new GameObject("bite", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
            var rt  = bit.GetComponent<RectTransform>();
            var img = bit.GetComponent<Image>();
            var cg  = bit.GetComponent<CanvasGroup>();
            rt.SetParent(canvas.transform, false);
            rt.SetAsLastSibling();
            rt.position  = at.position + (Vector3)Random.insideUnitCircle * 10f;
            rt.sizeDelta = at.sizeDelta * 0.2f;
            img.sprite   = srcImg ? srcImg.sprite : null;
            img.preserveAspect = true;
            StartCoroutine(FallAndFade(rt, cg));
        }
        yield return new WaitForSecondsRealtime(0.25f);
    }

    IEnumerator FallAndFade(RectTransform rt, CanvasGroup cg)
    {
        float t = 0f, dur = 0.35f;
        Vector3 a = rt.position, b = a + new Vector3(Random.Range(-20f, 20f), -60f, 0);
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float k = t / dur;
            rt.position   = Vector3.Lerp(a, b, k);
            rt.localScale = Vector3.one * (1f - 0.7f * k);
            if (cg) cg.alpha = 1f - k;
            yield return null;
        }
        Destroy(rt.gameObject);
    }
}
