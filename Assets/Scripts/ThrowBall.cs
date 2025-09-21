using UnityEngine;
using UnityEngine.InputSystem;

public class ThrowBall : MonoBehaviour
{
    [Header("Throw")]
    public float travelTime = 0.35f;
    public AnimationCurve ease = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Per-throw award")]
    [Tooltip("Percent points added to the Fetch need each time you throw.")]
    public float awardPerThrowPercent = 10f;   // +10% per throw

    private BallController ball;
    private Transform player;
    private Transform hand;
    private Camera cam;

    // Session/Slider gate (block throwing when full)
    [SerializeField] private PlaySatisfaction playSatisfaction;

    void Awake()
    {
        ball = GetComponent<BallController>();
        cam = Camera.main;

        var p = GameObject.FindGameObjectWithTag("Player");
        if (p)
        {
            player = p.transform;
            hand = player.Find("Hand");
        }

        // auto-find if not set in Inspector
        if (!playSatisfaction)
            playSatisfaction = FindFirstObjectByType<PlaySatisfaction>();
    }

    void Start()
    {
        if (hand != null) ball.AttachToHand(hand);
    }

    void Update()
    {
        // Hard block throwing while either meter is full/locked
        var fm = FetchNeedManager.Instance;
        if ((playSatisfaction && playSatisfaction.IsFull()) ||
            (fm != null && fm.IsFetchFull()))
            return;

        if (Mouse.current == null && Keyboard.current == null) return;

        bool throwPressed =
            (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame) ||
            (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame);

        if (throwPressed && ball.currentOwner == BallController.Owner.PlayerHand)
            Throw();
    }

    void Throw()
    {
        // 1) Award +10% to Fetch immediately on throw (if not full)
        var fm = FetchNeedManager.Instance;
        if (fm != null && !fm.IsFetchFull())
        {
            fm.AddFetchPercent(awardPerThrowPercent);

            // Tiny toast each throw
            GlobalNotifier.Instance?.ShowToast(
                $"+{awardPerThrowPercent:0.#}% fetch XP",
                GlobalNotifier.Instance ? GlobalNotifier.Instance.toastHoldSeconds * 0.6f : 2f
            );
        }

        // 2) Normal throw behaviour
        ball.SetFlying();

        Vector3 target = transform.position;
        if (Mouse.current != null && cam != null)
        {
            Vector2 mousePos = Mouse.current.position.ReadValue();
            target = cam.ScreenToWorldPoint(mousePos);
        }
        target.z = hand ? hand.position.z : 0f;

        StopAllCoroutines();
        StartCoroutine(ThrowToPoint(target));

        var pet = FindFirstObjectByType<PetFetchManager>();
        if (pet) pet.StartFetch(ball); // respects your PetFetchManager gating
    }

    System.Collections.IEnumerator ThrowToPoint(Vector3 target)
    {
        Vector3 start = transform.position;
        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime / Mathf.Max(0.0001f, travelTime);
            float k = ease.Evaluate(Mathf.Clamp01(t));
            transform.position = Vector3.Lerp(start, target, k);
            yield return null;
        }

        ball.Release(target);
    }
}
