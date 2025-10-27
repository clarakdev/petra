using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class WalkUIManager : MonoBehaviour
{
    [Header("UI (optional)")]
    public PanelProgressBar walkBar;   // seeds global once if needed
    public Canvas canvas;              // only for optional confetti FX

    [Header("Pet celebration")]
    public Transform pet;
    public Animator  petAnimator;
    public string    happyTrigger    = "Happy";
    public float     jumpHeight      = 1.1f;
    public float     jumpUpDuration  = 0.28f;
    public float     jumpDownDuration= 0.22f;
    public float     secondJumpDelay = 0.08f;

    [Header("Exactly 50% popup")]
    [Tooltip("Listens to PetNeedsManager.OnWalkHit50 (fires ONLY when walk becomes exactly 50). " +
             "If GlobalNotifier.autoSubscribe is true, this script will not also subscribe.")]
    public bool ensure50Popup = true;                         // <— renamed to match Clean
    public string walk50Message = "Time to walk your pet!";   // <— editable message
    public bool enableLocalWalk50Pop = true;

    [Header("Optional confetti FX at 50%")]
    public bool   playConfettiAt50 = false;
    public Sprite confettiSprite; // optional; falls back to 1x1 white if null
    public Vector2 confettiSize = new Vector2(40, 40);
    public int     confettiCount = 6;

    // 🪙 Reward system
    [Header("Rewards")]
    public int coinsForFullWalk = 75;   // coins awarded when walk hits 100%
    private bool _paidForThisFull = false;

    bool _happyPlaying;
    UnityAction _onWalk50;
    bool _listening;

    void Awake()
    {
        // Find pet if not wired
        if (!pet)
        {
            var follower = FindObjectOfType<PetFollower>();
            pet = follower ? follower.transform : GameObject.Find("Pet")?.transform;
        }
        if (pet && !petAnimator) petAnimator = pet.GetComponent<Animator>();

        // Seed global once from the bar (safe no-op if already persisted/seeded)
        var mgr = PetNeedsManager.Instance;
        if (mgr != null && walkBar != null)
            mgr.InitializeWalkIfUnset(walkBar.value);
    }

    void OnEnable()
    {
        TrySubscribeWalk50();

        var needs = PetNeedsManager.Instance;
        if (needs != null)
            needs.OnWalkChanged.AddListener(OnWalkValueChanged);
    }

    void OnDisable()
    {
        UnsubscribeWalk50();

        var needs = PetNeedsManager.Instance;
        if (needs != null)
            needs.OnWalkChanged.RemoveListener(OnWalkValueChanged);
    }

    void OnDestroy() { UnsubscribeWalk50(); }

    // Subscribe to the EXACT-50 event from PetNeedsManager.
    void TrySubscribeWalk50()
    {
        if (!ensure50Popup || _listening) return;
        if (!enableLocalWalk50Pop || _listening) return;

        var needs = PetNeedsManager.Instance;
        if (needs == null) return;

        // If GlobalNotifier already subscribes globally, avoid double toasts
        var notifier = GlobalNotifier.Instance;
        if (notifier != null && notifier.autoSubscribe) return;

        if (_onWalk50 == null)
        {
            _onWalk50 = () =>
            {
                var gn = GlobalNotifier.Instance;
                if (gn != null) gn.ShowToast(walk50Message, gn.toastHoldSeconds);
                TriggerPetHappy();
                if (playConfettiAt50) StartCoroutine(ConfettiBurst());
            };
        }

        needs.OnWalkHit50.AddListener(_onWalk50);
        _listening = true;
    }

    void UnsubscribeWalk50()
    {
        if (!_listening) return;
        var needs = PetNeedsManager.Instance;
        if (needs != null && _onWalk50 != null)
            needs.OnWalkHit50.RemoveListener(_onWalk50);
        _listening = false;
    }

    /// Call this from buttons / pickups; amount is percent points (e.g., 5 = +5%)
    public void AddXPPercent(float amount)
    {
        var mgr = PetNeedsManager.Instance;
        if (mgr != null)
        {
            if (!mgr.IsWalkFull())
            {
                mgr.AddWalkPercent(amount); // PetNeedsManager handles exact-50 triggering
                TriggerPetHappy();
            }
        }
        else if (walkBar)
        {
            walkBar.SetValue(Mathf.Min(100f, walkBar.value + amount));
            TriggerPetHappy();
        }
    }

    public void TriggerPetHappy()
    {
        if (_happyPlaying || !pet) return;
        StartCoroutine(HappyRoutine());
    }

    IEnumerator HappyRoutine()
    {
        _happyPlaying = true;

        if (petAnimator && !string.IsNullOrEmpty(happyTrigger))
        {
            petAnimator.ResetTrigger(happyTrigger);
            petAnimator.SetTrigger(happyTrigger);
            yield return new WaitForSecondsRealtime(jumpUpDuration + jumpDownDuration + secondJumpDelay);
            petAnimator.ResetTrigger(happyTrigger);
            petAnimator.SetTrigger(happyTrigger);
            yield return new WaitForSecondsRealtime(jumpUpDuration + jumpDownDuration);
        }
        else
        {
            yield return StartCoroutine(JumpOnce());
            yield return new WaitForSecondsRealtime(secondJumpDelay);
            yield return StartCoroutine(JumpOnce());
        }

        _happyPlaying = false;
    }

    IEnumerator JumpOnce()
    {
        Vector3 basePos = pet.position;

        float t = 0f;
        while (t < jumpUpDuration)
        {
            t += Time.unscaledDeltaTime;
            float a = Mathf.Clamp01(t / jumpUpDuration);
            float y = Mathf.Sin(a * Mathf.PI * 0.5f) * jumpHeight;
            pet.position = new Vector3(basePos.x, basePos.y + y, basePos.z);
            yield return null;
        }

        t = 0f;
        while (t < jumpDownDuration)
        {
            t += Time.unscaledDeltaTime;
            float a = Mathf.Clamp01(t / jumpDownDuration);
            float y = Mathf.Cos(a * Mathf.PI * 0.5f) * jumpHeight;
            pet.position = new Vector3(basePos.x, basePos.y + y, basePos.z);
            yield return null;
        }

        pet.position = basePos;
    }

    // ===== Optional confetti FX (temporary UI; auto-destroy) =====
    IEnumerator ConfettiBurst()
    {
        if (canvas == null) yield break;

        var sprite = confettiSprite != null ? confettiSprite : DefaultWhiteSprite();
        var center = (pet != null) ? pet.position : canvas.transform.position;

        for (int i = 0; i < Mathf.Max(1, confettiCount); i++)
        {
            var bit = new GameObject("walk_confetti", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
            var rt  = bit.GetComponent<RectTransform>();
            var cg  = bit.GetComponent<CanvasGroup>();
            var img = bit.GetComponent<Image>();

            rt.SetParent(canvas.transform, false);
            rt.SetAsLastSibling();
            rt.position  = center + (Vector3)Random.insideUnitCircle * 10f;
            rt.sizeDelta = confettiSize;
            img.sprite   = sprite;
            img.preserveAspect = true;

            StartCoroutine(ConfettiRiseFade(rt, cg));
        }

        yield return new WaitForSecondsRealtime(0.3f);
    }

    IEnumerator ConfettiRiseFade(RectTransform rt, CanvasGroup cg)
    {
        float t = 0f, dur = 0.45f;
        Vector3 a = rt.position;
        Vector3 b = a + new Vector3(Random.Range(-20f, 20f), 60f, 0);
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

    static Sprite DefaultWhiteSprite()
    {
        var tex = Texture2D.whiteTexture;
        return Sprite.Create(tex, new Rect(0,0,tex.width,tex.height), new Vector2(0.5f,0.5f), 100f);
    }

    //Reward logic
    private void OnWalkValueChanged(float walkPercent)
    {
        // Re-arm the reward if bar drops below full
        if (walkPercent < 100f - 0.01f)
        {
            _paidForThisFull = false;
            return;
        }

        // Award coins once when reaching 100%
        if (!_paidForThisFull && walkPercent >= 100f - 0.01f)
        {
            var wallet = FindFirstObjectByType<PlayerCurrency>();
            if (wallet != null)
            {
                wallet.EarnCurrency(coinsForFullWalk);
                Debug.Log($"[WalkUIManager] Walk reached 100%. Awarded {coinsForFullWalk} coins. Total: {wallet.currency}");
            }

            _paidForThisFull = true;
        }
    }
}
