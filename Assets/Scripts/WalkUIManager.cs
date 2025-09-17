using System.Collections;
using UnityEngine;

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

    bool _happyPlaying;

    void Awake()
    {
        // Try find pet if not wired
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

    /// Call this from flower/chest/etc. amount = percent points (e.g., 5 = +5%)
    public void AddXPPercent(float amount)
    {
        var mgr = PetNeedsManager.Instance;
        if (mgr != null)
        {
            if (mgr.IsWalkFull()) return;           // respect 100% lockout
            mgr.AddWalkPercent(amount);
            TriggerPetHappy();
        }
        else if (walkBar) // fallback if global not present
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
}
