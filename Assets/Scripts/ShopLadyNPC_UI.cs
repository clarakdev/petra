using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

public class ShopLadyNPC_UI: MonoBehaviour, IPointerClickHandler
{
    [Header("Open Shop")]
    [SerializeField] private bool loadShopScene = true;
    [SerializeField] private string shopSceneName = "ShopScene";
    [SerializeField] private GameObject shopCanvasToEnable;

    [Header("Optional proximity gate")]
    [SerializeField] private bool requireProximity = false;
    [SerializeField] private Collider2D proximityTrigger; // assign a world trigger near the lady
    [SerializeField] private string playerTag = "Player";

    public UnityEvent OnShopOpened;

    private bool playerNearby;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!requireProximity) return;
        if (other.CompareTag(playerTag) && other == proximityTrigger) playerNearby = true;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!requireProximity) return;
        if (other.CompareTag(playerTag) && other == proximityTrigger) playerNearby = false;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (requireProximity && !playerNearby) return;
        OpenShop();
    }

    private void OpenShop()
    {
        OnShopOpened?.Invoke();
        if (loadShopScene && !string.IsNullOrEmpty(shopSceneName))
            SceneManager.LoadScene(shopSceneName, LoadSceneMode.Single);
        else if (shopCanvasToEnable) 
            shopCanvasToEnable.SetActive(true);
    }
}
