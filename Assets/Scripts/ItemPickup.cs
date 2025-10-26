using UnityEngine;
using UnityEngine.InputSystem;

public class ItemPickup : MonoBehaviour
{
    [Header("Item to Give")]
    [SerializeField] private ShopItem item;
    [SerializeField] private int quantity = 1;

    [Header("Pickup Settings")]
    [SerializeField] private bool oneTimePickup = true;
    [SerializeField] private string pickupKey; // Unique ID for saving pickup state
    [SerializeField] private bool requirePlayerNearby = true;

    [Header("Visual Feedback")]
    [SerializeField] private GameObject visualObject; // The object that appears on the map
    [SerializeField] private ParticleSystem pickupEffect;

    private bool isPickedUp = false;
    private bool isProcessing = false; // Prevent multiple pickups
    private bool playerNearby = false; // Track if player is nearby

    void Awake()
    {
        // Generate unique key if not set
        if (string.IsNullOrEmpty(pickupKey))
        {
            Vector3 pos = transform.position;
            pickupKey = $"Pickup_{gameObject.scene.name}_{gameObject.name}_{pos.x:F1}_{pos.y:F1}";
        }

        // IMMEDIATELY check and hide BEFORE any rendering can happen
        CheckAndHideIfPickedUp();
    }

    private void CheckAndHideIfPickedUp()
    {
        // Check if already picked up using SessionPickupTracker
        if (oneTimePickup && SessionPickupTracker.Instance.WasPickedUp(pickupKey))
        {
            isPickedUp = true;

            // Hide immediately - don't wait
            if (visualObject != null && visualObject != gameObject)
            {
                visualObject.SetActive(false);
            }
            else
            {
                gameObject.SetActive(false);
            }
            return;
        }
    }

    void Start()
    {
        // Only setup collider if not already picked up
        if (!isPickedUp)
        {
            EnsureColliderSetup();
        }
    }

    private void EnsureColliderSetup()
    {
        // Check if we have BoxCollider2D
        BoxCollider2D col2D = GetComponent<BoxCollider2D>();
        if (col2D == null)
        {
            col2D = gameObject.AddComponent<BoxCollider2D>();
        }

        // Set as trigger for proximity detection
        col2D.isTrigger = true;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerNearby = true;
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerNearby = false;
        }
    }

    void Update()
    {
        // Skip if already picked up or not ready
        if (isPickedUp || isProcessing) return;

        // If require nearby and player isn't nearby, skip
        if (requirePlayerNearby && !playerNearby) return;

        // Check for mouse click
        if (Mouse.current == null) return;

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            Vector2 mousePos = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
            Collider2D hit = Physics2D.OverlapPoint(mousePos);

            if (hit != null && hit.gameObject == gameObject)
            {
                TryPickup();
            }
        }
    }

    private void TryPickup()
    {
        if (isProcessing || isPickedUp || item == null)
            return;

        try
        {
            InventoryManager inventoryManager = InventoryManager.Instance;

            if (inventoryManager == null)
            {
                inventoryManager = FindObjectOfType<InventoryManager>();

                if (inventoryManager == null)
                {
                    ShowPickupMessage("Inventory system not available!");
                    return;
                }
            }

            isProcessing = true;

            int leftover = inventoryManager.AddItem(item, quantity);

            if (leftover == 0)
            {
                isPickedUp = true;

                if (oneTimePickup)
                {
                    SessionPickupTracker.Instance.MarkAsPickedUp(pickupKey);
                }

                if (pickupEffect != null)
                {
                    pickupEffect.Play();
                }

                ShowPickupMessage($"Found {item.Name}!");

                float destroyDelay = pickupEffect != null ? 1f : 0f;
                Invoke(nameof(HideOrDestroy), destroyDelay);
            }
            else
            {
                ShowPickupMessage("Inventory full!");
                isProcessing = false;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[ItemPickup] Exception during pickup: {e.Message}\n{e.StackTrace}");
            isProcessing = false;
        }
    }

    private void HideOrDestroy()
    {
        if (visualObject != null && visualObject != gameObject)
        {
            visualObject.SetActive(false);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void ShowPickupMessage(string message)
    {
        Debug.Log($"[ItemPickup] {message}");
    }

    void OnDrawGizmos()
    {
        if (!isPickedUp)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, 0.5f);
        }
    }
}
