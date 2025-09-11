using UnityEngine;

public class PetFollower : MonoBehaviour
{
    [SerializeField] public float moveSpeed = 5f;
    public float followDistance = 1.5f; // Minimum distance to maintain from the player
    Rigidbody2D rb;
    Transform target;
    Vector2 moveDirection;
    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        target = GameObject.Find("Player").transform;
    }

    // Update is called once per frame
    void Update()
    {
        if (target)
        {
            Vector3 offset = target.position - transform.position;
            float distance = offset.magnitude;

            // Only move if farther than followDistance
            if (distance > followDistance)
            {
                moveDirection = offset.normalized;
            }
            else
            {
                moveDirection = Vector2.zero; // Stop moving when close enough
            }
        }
    }

    private void FixedUpdate()
    {
        if (target)
        {
            rb.linearVelocity = moveDirection * moveSpeed;
        }
    }
}
