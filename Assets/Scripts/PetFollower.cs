using Photon.Pun;
using UnityEngine;

public class PetFollower : MonoBehaviourPun, IPunObservable
{
    [SerializeField] public float moveSpeed = 5f;
    public float followDistance = 1.5f; // Minimum distance to maintain from the player
    Rigidbody2D rb;
    Transform target;
    Vector2 moveDirection;
    SpriteRenderer spriteRenderer;

    [Header("Directional Sprites")]
    [SerializeField] private Sprite frontSprite;
    [SerializeField] private Sprite backSprite;
    [SerializeField] private Sprite leftSprite;
    [SerializeField] private Sprite rightSprite;
    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        target = GameObject.Find("Player").transform;
    }

    // Update is called once per frame

    void Update()
    {
        if (!PhotonNetwork.IsConnected || photonView.IsMine)
        {
            if (target)
            {
                Vector3 offset = target.position - transform.position;
                float distance = offset.magnitude;

                if (distance > followDistance)
                {
                    moveDirection = offset.normalized;
                    UpdateSpriteDirection(moveDirection);
                }
                else
                {
                    moveDirection = Vector2.zero;
                }
            }
        }
    }

    void FixedUpdate()
    {
        if (!PhotonNetwork.IsConnected || photonView.IsMine)
        {
            if (target)
            {
                rb.linearVelocity = moveDirection * moveSpeed;
            }
        }
    }

    // Change sprite based on movement direction
    private void UpdateSpriteDirection(Vector2 direction)
    {
        // Check if horizontal movement is greater than vertical movement
        if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y))
        {
            if (direction.x < 0)
                spriteRenderer.sprite = leftSprite;
            else if (direction.x > 0)
                spriteRenderer.sprite = rightSprite;
        }
        else
        {
            if (direction.y > 0)
                spriteRenderer.sprite = backSprite;
            else if (direction.y < 0)
                spriteRenderer.sprite = frontSprite;
        }
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(rb.position);
            stream.SendNext(rb.linearVelocity);
            stream.SendNext(spriteRenderer.sprite.name); // Send sprite direction by name
        }
        else
        {
            rb.position = (Vector2)stream.ReceiveNext();
            rb.linearVelocity = (Vector2)stream.ReceiveNext();
            string spriteName = (string)stream.ReceiveNext();

            // Set sprite based on name
            if (spriteName == leftSprite.name)
            {
                spriteRenderer.sprite = leftSprite;
            }
            else if (spriteName == rightSprite.name)
            {
                spriteRenderer.sprite = rightSprite;
            }
            else if (spriteName == backSprite.name)
            {
                spriteRenderer.sprite = backSprite;
            }
            else
            {
                spriteRenderer.sprite = frontSprite;
            }
        }
    }
}
