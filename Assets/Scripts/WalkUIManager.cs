using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class WalkUIManager : MonoBehaviour
{
    [Header("UI")]
    public PanelProgressBar walkBar;  // only used to seed global once (if needed)

    // (Keep these visible so you can find them in the Inspector; leave at 0 so global owns decay)
    [Header("Drain (local scene – leave 0)")]
    public float walkDrainPerMinute = 0f;
    public float extraDrainPerMinute = 0f;
    public float fullPauseMinutes = 20f; // legacy; global handles 100% pause

    [Header("Pet celebration (double jump only)")]
    public Transform pet;
    public Animator  petAnimator;
    public string    happyTrigger = "Happy";
    public float     jumpHeight = 1.1f;
    public float     jumpUpDuration = 0.28f;
    public float     jumpDownDuration = 0.22f;
    public float     secondJumpDelay = 0.08f;

    [Header("50% pop settings")]
    [Tooltip("If true, this script will show a toast when WALK crosses down to 50%.\n" +
             "If GlobalNotifier already auto-subscribes to PetNeedsManager, we auto-skip to avoid double pops.")]
    public bool enableLocalWalk50Pop = true;

    bool _happyPlaying;
    UnityAction _onWalk50;   // subscription handle so we can cleanly unsubscribe
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

    void OnEnable()  { TrySubscribeWalk50(); }
    void OnDisable() { UnsubscribeWalk50();  }
    void OnDestroy() { UnsubscribeWalk50();  }

    void TrySubscribeWalk50()
    {
        if (!enableLocalWalk50Pop || _listening) return;

        var needs = PetNeedsManager.Instance;
        if (needs == null) return;

        // If a GlobalNotifier exists AND it is already auto-subscribing to 50% events,
        // skip local subscription to avoid duplicate toasts.
        var notifier = GlobalNotifier.Instance;
        if (notifier != null && notifier.autoSubscribe) return;

        if (_onWalk50 == null)
        {
            _onWalk50 = () =>
            {
                var gn = GlobalNotifier.Instance;
                if (gn != null) gn.ShowToast("Time to walk your pet!", gn.toastHoldSeconds);
                else Debug.Log("[WalkUIManager] Time to walk your pet! (50%)");
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

    /// Call this from flower/chest/etc. amount = percent points (e.g., 5 = +5%)
    public void AddXPPercent(float amount)
    {
        var mgr = PetNeedsManager.Instance;
        if (mgr != null)
        {
            if (mgr.IsWalkFull()) return;  // respect 100% lockout (20-min pause handled by manager)
            mgr.AddWalkPercent(amount);
            TriggerPetHappy();
        }
        else if (walkBar) // fallback if no global manager is present
        {
            float next = Mathf.Min(100f, walkBar.value + amount);
            walkBar.SetValue(next);
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

    // Handy right-click tests in Inspector
    [ContextMenu("Test: +10% Walk XP")]
    void _TestAdd10() => AddXPPercent(10f);

    [ContextMenu("Test: Trigger Happy")]
    void _TestHappy() => TriggerPetHappy();
}
