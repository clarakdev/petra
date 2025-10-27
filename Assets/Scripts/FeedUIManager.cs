using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

[DisallowMultipleComponent]
public class FeedUIManager : MonoBehaviour
{
    [Header("UI Refs")]
    public Canvas canvas;
    public PanelProgressBar hungerBar; // seeds global once
    public RectTransform petRect;

    [Header("Drain (local scene – leave 0)")]
    public float hungerDecayPerMinute = 0f; // not used; global owns decay
    public float fullPauseMinutes = 20f; // legacy; global handles 100% pause

    [Header("FX (optional)")]
    public PetEmotionFX petFX;

    [Header("Popup at exactly 50%")]
    [Tooltip("Listens to PetNeedsManager.OnFeedHit50 (fires only when feed becomes exactly 50).\n" +
             "If GlobalNotifier is auto-subscribing, we do NOT also subscribe here.")]
    public bool ensure50Popup = true;
    public string feed50Message = "Time to feed your pet!";

    bool _subscribed50;

    [Header("Rewards")]
    public int coinsForFullFeed = 50;  // coins given when Feed reaches 100%
    private bool _paidForThisFull = false;

    void Awake()
    {
        if (!canvas) canvas = GetComponentInParent<Canvas>();

        // Force feed to start empty (ignore existing bar values)
        var mgr = PetNeedsManager.Instance;
        if (mgr != null)
        {
            mgr.InitializeFeedIfUnset(0f);  // ✅ seed zero instead of hungerBar.value
            if (hungerBar != null)
                hungerBar.SetValue(0f);     // ✅ visually reset bar
        }
    }

    void OnEnable()
    {
        TrySubscribe50();

        var needs = PetNeedsManager.Instance;
        if (needs != null)
            needs.OnFeedChanged.AddListener(OnFeedValueChanged);
    }

    void OnDisable()
    {
        Unsubscribe50();

        var needs = PetNeedsManager.Instance;
        if (needs != null)
            needs.OnFeedChanged.RemoveListener(OnFeedValueChanged);
    }

    void OnDestroy()
    {
        Unsubscribe50();
    }

    void Start()
    {
        // Auto-bind pet rect & sprite
        if (petRect == null)
        {
            var petImg = FindFirstObjectByType<PetImage>();
            if (petImg != null)
            {
                petRect = petImg.RectTransform;

                var sel = PetSelectionManager.instance;
                if (sel != null && sel.currentPet != null && sel.currentPet.cardImage != null)
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

    // Subscribe only if GlobalNotifier isn't already doing it
    void TrySubscribe50()
    {
        if (_subscribed50 || !ensure50Popup)
            return;

        var needs = PetNeedsManager.Instance;
        if (needs == null)
            return;

        var notifier = GlobalNotifier.Instance;
        if (notifier != null && notifier.autoSubscribe)
            return; // avoid duplicate toasts

        needs.OnFeedHit50.AddListener(OnFeedHit50);
        _subscribed50 = true;
    }

    void Unsubscribe50()
    {
        if (!_subscribed50)
            return;

        var needs = PetNeedsManager.Instance;
        if (needs != null)
            needs.OnFeedHit50.RemoveListener(OnFeedHit50);

        _subscribed50 = false;
    }

    void OnFeedHit50()
    {
        var gn = GlobalNotifier.Instance;
        if (gn != null)
            gn.ShowToast(feed50Message, gn.toastHoldSeconds);
    }

    public bool CanFeedNow()
    {
        var mgr = PetNeedsManager.Instance;
        // If global exists and is full, cannot feed. If global missing, allow (fallback path)
        return !(mgr != null && mgr.IsFeedFull());
    }

    public void Feed(DraggableFood food)
    {
        if (food == null || canvas == null || petRect == null)
            return;

        if (!CanFeedNow())
            return;

        StartCoroutine(EatRoutine(food));
    }

    IEnumerator EatRoutine(DraggableFood food)
    {
        var foodRT = food.GetComponent<RectTransform>();
        if (foodRT == null)
            yield break;

        // Ghost flies to pet
        var ghost = new GameObject("FoodGhost", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
        var gRT = ghost.GetComponent<RectTransform>();
        var gImg = ghost.GetComponent<Image>();
        gRT.SetParent(canvas.transform, false);
        gRT.SetAsLastSibling();
        gRT.position = foodRT.position;
        gRT.sizeDelta = foodRT.sizeDelta;
        gImg.sprite = (food.image != null) ? food.image.sprite : null;
        gImg.preserveAspect = true;

        float t = 0f, dur = 0.35f;
        Vector3 a = gRT.position;
        Vector3 b = petRect.position + new Vector3(0, petRect.rect.height * 0.1f, 0);

        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float k = t / dur;
            gRT.position = Vector3.Lerp(a, b, k);
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
                mgr.AddFeedPercent(food.nutrition); // nutrition is % points
        }
        else if (hungerBar != null) // fallback if no global
        {
            hungerBar.SetValue(Mathf.Min(100f, hungerBar.value + food.nutrition));
        }

        if (petFX)
            petFX.PlayHappy();
    }

    IEnumerator BiteBurst(Image srcImg, RectTransform at)
    {
        const int n = 5;
        for (int i = 0; i < n; i++)
        {
            var bit = new GameObject("bite", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
            var rt = bit.GetComponent<RectTransform>();
            var img = bit.GetComponent<Image>();
            var cg = bit.GetComponent<CanvasGroup>();

            rt.SetParent(canvas.transform, false);
            rt.SetAsLastSibling();
            rt.position = at.position + (Vector3)Random.insideUnitCircle * 10f;
            rt.sizeDelta = at.sizeDelta * 0.2f;
            img.sprite = srcImg ? srcImg.sprite : null;
            img.preserveAspect = true;

            StartCoroutine(FallAndFade(rt, cg));
        }

        yield return new WaitForSecondsRealtime(0.25f);
    }

    IEnumerator FallAndFade(RectTransform rt, CanvasGroup cg)
    {
        float t = 0f, dur = 0.35f;
        Vector3 a = rt.position;
        Vector3 b = a + new Vector3(Random.Range(-20f, 20f), -60f, 0);

        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float k = t / dur;
            rt.position = Vector3.Lerp(a, b, k);
            rt.localScale = Vector3.one * (1f - 0.7f * k);
            if (cg)
                cg.alpha = 1f - k;

            yield return null;
        }

        Destroy(rt.gameObject);
    }

    //Reward coins when Feed reaches 100%
    private void OnFeedValueChanged(float feedPercent)
    {
        // Re-arm the reward if it dropped below 100 again
        if (feedPercent < 100f - 0.01f)
        {
            _paidForThisFull = false;
            return;
        }

        // Award coins once per full feed
        if (!_paidForThisFull && feedPercent >= 100f - 0.01f)
        {
            var wallet = FindFirstObjectByType<PlayerCurrency>();
            if (wallet != null)
            {
                wallet.EarnCurrency(coinsForFullFeed);
                Debug.Log($"[FeedUIManager] Feed reached 100%. Awarded {coinsForFullFeed} coins. Total: {wallet.currency}");
            }

            _paidForThisFull = true; // prevent duplicate payouts
        }
    }
}

using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

[DisallowMultipleComponent]
public class FeedUIManager : MonoBehaviour
{
    [Header("UI Refs")]
    public Canvas canvas;
    public PanelProgressBar hungerBar; // seeds global once
    public RectTransform petRect;

    [Header("Drain (local scene – leave 0)")]
    public float hungerDecayPerMinute = 0f; // not used; global owns decay
    public float fullPauseMinutes = 20f; // legacy; global handles 100% pause

    [Header("FX (optional)")]
    public PetEmotionFX petFX;

    [Header("Popup at exactly 50%")]
    [Tooltip("Listens to PetNeedsManager.OnFeedHit50 (fires only when feed becomes exactly 50).\n" +
             "If GlobalNotifier is auto-subscribing, we do NOT also subscribe here.")]
    public bool ensure50Popup = true;
    public string feed50Message = "Time to feed your pet!";

    bool _subscribed50;

    [Header("Rewards")]
    public int coinsForFullFeed = 50;  // coins given when Feed reaches 100%
    private bool _paidForThisFull = false;

    [Header("Audio")]
    [SerializeField] private AudioClip feedClip; // assign chomp / eating SFX in Inspector

    void Awake()
    {
        if (!canvas) canvas = GetComponentInParent<Canvas>();

        // Force feed to start empty (ignore existing bar values)
        var mgr = PetNeedsManager.Instance;
        if (mgr != null)
        {
            mgr.InitializeFeedIfUnset(0f);  // seed zero instead of hungerBar.value
            if (hungerBar != null)
                hungerBar.SetValue(0f);     // visually reset bar
        }
    }

    void OnEnable()
    {
        TrySubscribe50();

        var needs = PetNeedsManager.Instance;
        if (needs != null)
            needs.OnFeedChanged.AddListener(OnFeedValueChanged);
    }

    void OnDisable()
    {
        Unsubscribe50();

        var needs = PetNeedsManager.Instance;
        if (needs != null)
            needs.OnFeedChanged.RemoveListener(OnFeedValueChanged);
    }

    void OnDestroy()
    {
        Unsubscribe50();
    }

    void Start()
    {
        // Auto-bind pet rect & sprite
        if (petRect == null)
        {
            var petImg = FindFirstObjectByType<PetImage>();
            if (petImg != null)
            {
                petRect = petImg.RectTransform;

                var sel = PetSelectionManager.instance;
                if (sel != null && sel.currentPet != null && sel.currentPet.cardImage != null)
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

    // Subscribe only if GlobalNotifier isn't already doing it
    void TrySubscribe50()
    {
        if (_subscribed50 || !ensure50Popup)
            return;

        var needs = PetNeedsManager.Instance;
        if (needs == null)
            return;

        var notifier = GlobalNotifier.Instance;
        if (notifier != null && notifier.autoSubscribe)
            return; // avoid duplicate toasts

        needs.OnFeedHit50.AddListener(OnFeedHit50);
        _subscribed50 = true;
    }

    void Unsubscribe50()
    {
        if (!_subscribed50)
            return;

        var needs = PetNeedsManager.Instance;
        if (needs != null)
            needs.OnFeedHit50.RemoveListener(OnFeedHit50);

        _subscribed50 = false;
    }

    void OnFeedHit50()
    {
        var gn = GlobalNotifier.Instance;
        if (gn != null)
            gn.ShowToast(feed50Message, gn.toastHoldSeconds);
    }

    public bool CanFeedNow()
    {
        var mgr = PetNeedsManager.Instance;
        // If global exists and is full, cannot feed. If global missing, allow (fallback path)
        return !(mgr != null && mgr.IsFeedFull());
    }

    public void Feed(DraggableFood food)
    {
        if (food == null || canvas == null || petRect == null)
            return;

        if (!CanFeedNow())
            return;

        StartCoroutine(EatRoutine(food));
    }

    IEnumerator EatRoutine(DraggableFood food)
    {
        var foodRT = food.GetComponent<RectTransform>();
        if (foodRT == null)
            yield break;

        // Ghost flies to pet
        var ghost = new GameObject("FoodGhost", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
        var gRT = ghost.GetComponent<RectTransform>();
        var gImg = ghost.GetComponent<Image>();
        gRT.SetParent(canvas.transform, false);
        gRT.SetAsLastSibling();
        gRT.position = foodRT.position;
        gRT.sizeDelta = foodRT.sizeDelta;
        gImg.sprite = (food.image != null) ? food.image.sprite : null;
        gImg.preserveAspect = true;

        float t = 0f, dur = 0.35f;
        Vector3 a = gRT.position;
        Vector3 b = petRect.position + new Vector3(0, petRect.rect.height * 0.1f, 0);

        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float k = t / dur;
            gRT.position = Vector3.Lerp(a, b, k);
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
                mgr.AddFeedPercent(food.nutrition); // nutrition is % points
        }
        else if (hungerBar != null) // fallback if no global
        {
            hungerBar.SetValue(Mathf.Min(100f, hungerBar.value + food.nutrition));
        }

        if (petFX)
            petFX.PlayHappy();

        // 🔊 Play eating sound
        if (SoundManager.Instance != null && feedClip != null)
        {
            SoundManager.Instance.PlaySFX(feedClip);
        }
    }

    IEnumerator BiteBurst(Image srcImg, RectTransform at)
    {
        const int n = 5;
        for (int i = 0; i < n; i++)
        {
            var bit = new GameObject("bite", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
            var rt = bit.GetComponent<RectTransform>();
            var img = bit.GetComponent<Image>();
            var cg = bit.GetComponent<CanvasGroup>();

            rt.SetParent(canvas.transform, false);
            rt.SetAsLastSibling();
            rt.position = at.position + (Vector3)Random.insideUnitCircle * 10f;
            rt.sizeDelta = at.sizeDelta * 0.2f;
            img.sprite = srcImg ? srcImg.sprite : null;
            img.preserveAspect = true;

            StartCoroutine(FallAndFade(rt, cg));
        }

        yield return new WaitForSecondsRealtime(0.25f);
    }

    IEnumerator FallAndFade(RectTransform rt, CanvasGroup cg)
    {
        float t = 0f, dur = 0.35f;
        Vector3 a = rt.position;
        Vector3 b = a + new Vector3(Random.Range(-20f, 20f), -60f, 0);

        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float k = t / dur;
            rt.position = Vector3.Lerp(a, b, k);
            rt.localScale = Vector3.one * (1f - 0.7f * k);
            if (cg)
                cg.alpha = 1f - k;

            yield return null;
        }

        Destroy(rt.gameObject);
    }

    //Reward coins when Feed reaches 100%
    private void OnFeedValueChanged(float feedPercent)
    {
        // Re-arm the reward if it dropped below 100 again
        if (feedPercent < 100f - 0.01f)
        {
            _paidForThisFull = false;
            return;
        }

        // Award coins once per full feed
        if (!_paidForThisFull && feedPercent >= 100f - 0.01f)
        {
            var wallet = FindFirstObjectByType<PlayerCurrency>();
            if (wallet != null)
            {
                wallet.EarnCurrency(coinsForFullFeed);
                Debug.Log($"[FeedUIManager] Feed reached 100%. Awarded {coinsForFullFeed} coins. Total: {wallet.currency}");
            }

            _paidForThisFull = true; // prevent duplicate payouts
        }
    }
}

