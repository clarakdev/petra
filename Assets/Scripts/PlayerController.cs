using Photon.Pun;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviourPun, IPunObservable
{
    [SerializeField] private float moveSpeed = 5f;
    private Rigidbody2D rb;
    private Vector2 moveInput;
    private SpriteRenderer spriteRenderer;

    [Header("Directional Sprites")]
    [SerializeField] private Sprite frontSprite;
    [SerializeField] private Sprite backSprite;
    [SerializeField] private Sprite leftSprite;
    [SerializeField] private Sprite rightSprite;

    // For remote player interpolation
    private Vector2 networkPosition;
    private Vector2 networkVelocity;

    private PlayerInput playerInput;

    void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        // Disable PlayerInput for remote players
        if (playerInput != null && !photonView.IsMine)
        {
            playerInput.enabled = false;
        }
    }

    void Start()
    {
        spriteRenderer.sprite = frontSprite; // Default facing front
        networkPosition = rb.position;
        networkVelocity = Vector2.zero;
    }

    void Update()
    {
        if (photonView.IsMine)
        {
            rb.linearVelocity = moveInput * moveSpeed;
        }
        else
        {
            // Interpolate position for remote players
            rb.position = Vector2.Lerp(rb.position, networkPosition, Time.deltaTime * 10f);
            rb.linearVelocity = networkVelocity;
        }
    }

    public void Move(InputAction.CallbackContext context)
    {
        if (!photonView.IsMine) return; // Only process input for local player

        moveInput = context.ReadValue<Vector2>();

        // Change sprite based on movement direction
        if (moveInput.x < 0)
        {
            spriteRenderer.sprite = leftSprite;
        }
        else if (moveInput.x > 0)
        {
            spriteRenderer.sprite = rightSprite;
        }
        else if (moveInput.y > 0)
        {
            spriteRenderer.sprite = backSprite;
        }
        else if (moveInput.y < 0)
        {
            spriteRenderer.sprite = frontSprite;
        }
    }

    // Synchronise position and velocity over the network
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
            networkPosition = (Vector2)stream.ReceiveNext();
            networkVelocity = (Vector2)stream.ReceiveNext();
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