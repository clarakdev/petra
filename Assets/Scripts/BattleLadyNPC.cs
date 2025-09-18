using UnityEngine;
using UnityEngine.InputSystem;

public class BattleLadyNPC : MonoBehaviour
{
    private bool playerNearby = false;
    private RandomBattleQueueManager queueManager;

    private void Start()
    {
        // Find the RandomBattleQueueManager in the scene (or assign via inspector)
        queueManager = FindObjectOfType<RandomBattleQueueManager>();
        if (queueManager == null)
        {
            Debug.LogError("BattleLadyNPC: No RandomBattleQueueManager found in the scene.");
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerNearby = true;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerNearby = false;
        }
    }

    private void Update()
    {
        if (playerNearby &&
            Mouse.current != null &&
            Mouse.current.leftButton.wasPressedThisFrame)
        {
            Vector2 mousePos = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
            Collider2D hit = Physics2D.OverlapPoint(mousePos);
            if (hit != null && hit.gameObject == this.gameObject)
            {
                Debug.Log("Battle Lady NPC: Clicked!");

                if (queueManager != null)
                {
                    Debug.Log("Calling JoinRandomBattleQueue()");
                    queueManager.JoinRandomBattleQueue();
                }
                else
                {
                    Debug.LogError("queueManager is null!");
                }
            }
        }
    }
}