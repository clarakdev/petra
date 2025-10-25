using Photon.Pun;
using UnityEngine;

public class PetFollower : MonoBehaviourPun, IPunObservable
{
    [SerializeField] public float moveSpeed = 5f;
    public float followDistance = 1.5f;

    private Rigidbody2D rb;
    private Transform target;
    private Vector2 moveDirection;
    private SpriteRenderer spriteRenderer;
    private PetAccessoryManager accessoryManager;

    [Header("Directional Sprites")]
    public Sprite frontSprite;
    public Sprite backSprite;
    public Sprite leftSprite;
    public Sprite rightSprite;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        accessoryManager = GetComponent<PetAccessoryManager>();
    }

    void Start()
    {
        target = GameObject.Find("Player")?.transform;

        // Initialize pet facing direction
        if (accessoryManager != null)
            accessoryManager.SetFacing("Front");
    }

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

    private void UpdateSpriteDirection(Vector2 direction)
    {
        string facing = "Front";

        if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y))
        {
            if (direction.x < 0)
            {
                spriteRenderer.sprite = leftSprite;
                facing = "Left";
            }
            else
            {
                spriteRenderer.sprite = rightSprite;
                facing = "Right";
            }
        }
        else
        {
            if (direction.y > 0)
            {
                spriteRenderer.sprite = backSprite;
                facing = "Back";
            }
            else if (direction.y < 0)
            {
                spriteRenderer.sprite = frontSprite;
                facing = "Front";
            }
        }

        // Tell accessory manager to sync sprite state
        accessoryManager?.SetFacing(facing);
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(rb.position);
            stream.SendNext(rb.linearVelocity);
        }
        else
        {
            rb.position = (Vector2)stream.ReceiveNext();
            rb.linearVelocity = (Vector2)stream.ReceiveNext();
        }
    }
}
