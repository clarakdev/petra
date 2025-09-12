using UnityEngine;
using UnityEngine.SceneManagement;

public class ShopNavigator : MonoBehaviour
{
    public static ShopNavigator Instance { get; private set; }

    [SerializeField] private string openShopSceneName = "StoreScene";
    [SerializeField] private string shopSceneName = "ShopScene";
    [SerializeField] private bool dontDestroyOnLoad = true;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        if (dontDestroyOnLoad) DontDestroyOnLoad(gameObject);
    }

    public void GoToShop() => LoadByName(shopSceneName);
    public void GoToOpenShop() => LoadByName(openShopSceneName);

    static void LoadByName(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName)) return;
        SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
    }
}
