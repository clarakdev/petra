using UnityEngine;
using UnityEngine.InputSystem;

public class ThrowBall : MonoBehaviour
{
    public float travelTime = 0.35f;
    public AnimationCurve ease = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private BallController ball;
    private Transform player;
    private Transform hand;
    private Camera cam;

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
    }

    void Start()
    {
        if (hand != null) ball.AttachToHand(hand);
    }

    void Update()
    {
        bool throwPressed =
            (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame) ||
            (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame);

        if (throwPressed && ball.currentOwner == BallController.Owner.PlayerHand)
            Throw();
    }

    void Throw()
    {
        ball.SetFlying();

        Vector2 mousePos = Mouse.current.position.ReadValue();
        Vector3 target = cam.ScreenToWorldPoint(mousePos);
        target.z = hand.position.z;

        StopAllCoroutines();
        StartCoroutine(ThrowToPoint(target));

        var pet = FindFirstObjectByType<PetFetchManager>();
        if (pet) pet.StartFetch(ball);
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