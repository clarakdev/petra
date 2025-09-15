using UnityEngine;
using UnityEngine.SceneManagement;

public class ShopNavigator : MonoBehaviour
{
    [SerializeField] private string storeSceneName = "StoreScene";
    [SerializeField] private string shopSceneName  = "ShopScene";
    [SerializeField] private bool dontDestroyOnLoad = true;

    private static ShopNavigator _instance;
    public const string CameFromShopKey = "CameFromShop";

    private void Awake()
    {
        if (!dontDestroyOnLoad) return;
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void OpenShop()
    {
        SceneManager.LoadScene(shopSceneName, LoadSceneMode.Single);
    }

    public void ReturnToStore()
    {
        PlayerPrefs.SetInt(CameFromShopKey, 1);
        PlayerPrefs.Save();
        SceneManager.LoadScene(storeSceneName, LoadSceneMode.Single);
    }
}
