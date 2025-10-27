using Photon.Pun;
using UnityEngine;

public class PetFollower : MonoBehaviourPun, IPunObservable
{
    [Header("Movement Settings")]
    [SerializeField] public float moveSpeed = 5f;
    public float followDistance = 1.5f; // Minimum distance to maintain from the player
    public float followDistance = 1.5f;

    private Rigidbody2D rb;
    private Transform target;
    private Vector2 moveDirection;
    private SpriteRenderer spriteRenderer;

    [Header("Directional Sprites")]
    [SerializeField] private Sprite frontSprite;
    [SerializeField] private Sprite backSprite;
    [SerializeField] private Sprite leftSprite;
    [SerializeField] private Sprite rightSprite;
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

    private void Start()
    {
        // Find the player object this pet should follow
        target = GameObject.Find("Player")?.transform;
    }

    private void Update()
    void Start()
    {
        target = GameObject.Find("Player")?.transform;

        // Initialize pet facing direction
        if (accessoryManager != null)
            accessoryManager.SetFacing("Front");
    }

    void Update()
    {
        // Only update movement logic for the local player's pet
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

    private void FixedUpdate()
    {
        // Apply movement only for the local instance
        if (!PhotonNetwork.IsConnected || photonView.IsMine)
        {
            if (target)
            {
                rb.linearVelocity = moveDirection * moveSpeed;
            }
        }
    }
    /// <summary>
    /// Updates the pet's sprite based on its movement direction.
    /// </summary>
    private void UpdateSpriteDirection(Vector2 direction)
    {
        // Check if horizontal movement is greater than vertical
    private void UpdateSpriteDirection(Vector2 direction)
    {
        string facing = "Front";

        if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y))
        {
            // Horizontal direction
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
            // Vertical direction
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

    /// <summary>
    /// Synchronizes position, velocity, and sprite direction over the network.
    /// </summary>
    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            // Send data to others
            stream.SendNext(rb.position);
            stream.SendNext(rb.linearVelocity);
            stream.SendNext(spriteRenderer.sprite.name); // Sync sprite direction
        }
        else
        {
            // Receive data from owner
            rb.position = (Vector2)stream.ReceiveNext();
            rb.linearVelocity = (Vector2)stream.ReceiveNext();
            string spriteName = (string)stream.ReceiveNext();

            // Update sprite based on received name
            if (spriteName == leftSprite.name)
                spriteRenderer.sprite = leftSprite;
            else if (spriteName == rightSprite.name)
                spriteRenderer.sprite = rightSprite;
            else if (spriteName == backSprite.name)
                spriteRenderer.sprite = backSprite;
            else
                spriteRenderer.sprite = frontSprite;
        }
    }
}
