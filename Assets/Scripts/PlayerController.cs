using Photon.Pun;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviourPun
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

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        spriteRenderer.sprite = frontSprite; // Default facing front
    }

    // Update is called once per frame
    void Update()
    {
        if (!photonView.IsMine) return; // Only process input for local player
        rb.linearVelocity = moveInput * moveSpeed;
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
}
