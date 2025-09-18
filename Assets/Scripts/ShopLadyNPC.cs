using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Collider2D))]
public class ShopLadyNPC : MonoBehaviour
{
    [Header("Open Shop")]
    [SerializeField] private bool loadShopScene = true;
    [SerializeField] private string shopSceneName = "ShopScene";
    [SerializeField] private GameObject shopCanvasToEnable; // set a Canvas/Panel if not using scene load

    [Header("Events")]
    public UnityEvent OnShopOpened;

    private bool playerNearby = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player")) playerNearby = true;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player")) playerNearby = false;
    }

    private void Update()
    {
        if (!playerNearby || Mouse.current == null) return;

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            Vector2 mousePos = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
            Collider2D hit = Physics2D.OverlapPoint(mousePos);
            if (hit != null && hit.gameObject == gameObject)
            {
                OpenShop();
            }
        }
    }

    private void OpenShop()
    {
        OnShopOpened?.Invoke();

        if (loadShopScene && !string.IsNullOrEmpty(shopSceneName))
        {
            SceneManager.LoadScene(shopSceneName, LoadSceneMode.Single);
        }
        else if (shopCanvasToEnable != null)
        {
            shopCanvasToEnable.SetActive(true);
        }
    }
}
