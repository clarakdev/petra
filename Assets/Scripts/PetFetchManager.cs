using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PetFetchManager : MonoBehaviour
{
    public Transform player;
    public Transform mouthPoint;
    public Transform playerHand;

    [Header("Movement")]
    public float moveSpeed      = 3.5f;
    public float arriveDistance = 0.20f;
    public float dropDistance   = 0.70f;

    [Header("Snap Back To Hand")]
    public float handSnapRadius = 0.55f;
    public float handSnapDelay  = 0.15f;

    private Rigidbody2D rb;
    private BallController ball;

    private enum State { Idle, GoToBall, CarryToPlayer }
    private State state = State.Idle;

    private float ignoreChaseUntil = 0f;

    public PlaySatisfaction playSatisfaction;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (!player) player = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (player && !playerHand) playerHand = player.Find("Hand");
    }

    void FixedUpdate()
    {
        if (!ball || !player) return;

        switch (state)
        {
            case State.GoToBall:
            {
                if (Time.time < ignoreChaseUntil)
                {
                    rb.linearVelocity = Vector2.zero;
                    state = State.Idle;
                    break;
                }

                if (ball.currentOwner != BallController.Owner.Free)
                {
                    rb.linearVelocity = Vector2.zero;
                    state = State.Idle;
                    break;
                }

                MoveTowards(ball.transform.position);

                if (Vector2.Distance(transform.position, ball.transform.position) <= arriveDistance)
                    PickUpBall();

                break;
            }

            case State.CarryToPlayer:
            {
                Vector3 target = playerHand ? (Vector3)playerHand.position : player.position;

                if (Vector2.Distance(transform.position, target) <= dropDistance)
                {
                    rb.linearVelocity = Vector2.zero;
                    DropBallAt(target);
                    break;
                }

                MoveTowards(target);
                break;
            }

            case State.Idle:
            {
                rb.linearVelocity = Vector2.zero;

                if (Time.time >= ignoreChaseUntil &&
                    ball && ball.currentOwner == BallController.Owner.Free)
                {
                    state = State.GoToBall;
                }
                break;
            }
        }
    }

    void MoveTowards(Vector3 targetPos)
    {
        Vector2 pos     = rb.position;
        Vector2 target  = (Vector2)targetPos;
        float   maxStep = moveSpeed * Time.fixedDeltaTime;
        rb.MovePosition(Vector2.MoveTowards(pos, target, maxStep));
    }

    void PickUpBall()
    {
        ball.AttachToMouth(mouthPoint);
        state = State.CarryToPlayer;
    }

    void DropBallAt(Vector3 near)
    {
        Vector2 dirToTarget = ((Vector2)near - (Vector2)transform.position).normalized;
        Vector3 dropPos = near - (Vector3)(dirToTarget * 0.12f);

        bool canSnapNow = playerHand &&
                          Vector2.Distance(dropPos, playerHand.position) <= handSnapRadius;

        if (canSnapNow) ball.AttachToHand(playerHand);
        else
        {
            ball.Release(dropPos);
            if (playerHand) StartCoroutine(SnapToHandIfClose());
        }

        ignoreChaseUntil = Time.time + 0.25f;
        state = State.Idle;

        if (playSatisfaction) playSatisfaction.AddFromFetch();
    }

    System.Collections.IEnumerator SnapToHandIfClose()
    {
        yield return new WaitForSeconds(handSnapDelay);
        if (!playerHand) yield break;

        if (Vector2.Distance(ball.transform.position, playerHand.position) <= handSnapRadius)
            ball.AttachToHand(playerHand);
    }

    // Single gate to allow starting a fetch (silent block if session is full)
    public bool CanStartFetch()
    {
        if (playSatisfaction && playSatisfaction.IsFull())
            return false; // silently block while full/locked
        return true;
    }

    // called by ThrowBall after the ball lands (or right away for looped fetch)
    public void StartFetch(BallController newBall)
    {
        if (state != State.Idle) return;
        if (!CanStartFetch())   return;

        ball = newBall;
        state = State.GoToBall;
    }
}
