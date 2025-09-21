using UnityEngine;
using UnityEngine.InputSystem;

public class BattleLadyNPC : MonoBehaviour
{
    private bool playerNearby = false;
    private RandomBattleQueueManager queueManager;

    void Start()
    {
        queueManager = FindObjectOfType<RandomBattleQueueManager>();
        if (queueManager == null)
            Debug.LogError("BattleLadyNPC: No RandomBattleQueueManager found in the scene.");
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player")) playerNearby = true;
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player")) playerNearby = false;
    }

    void Update()
    {
        if (!playerNearby || Mouse.current == null) return;

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            Vector2 mousePos = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
            Collider2D hit = Physics2D.OverlapPoint(mousePos);
            if (hit != null && hit.gameObject == gameObject)
            {
                if (queueManager != null) queueManager.JoinRandomBattleQueue();
                else Debug.LogError("queueManager is null!");
            }
        }
    }
}

