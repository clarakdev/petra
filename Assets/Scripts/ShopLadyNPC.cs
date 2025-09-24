using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class ShopLadyNPC : MonoBehaviour
{
    private bool playerNearby = false;

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
                SceneManager.LoadScene("Shop", LoadSceneMode.Single);
            }
        }
    }
}
