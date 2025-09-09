using UnityEngine;
using UnityEngine.SceneManagement;

public class ShopNavigator : MonoBehaviour
{
    [Header("Scene Names (must match Build Settings)")]
    [SerializeField] private string openShopSceneName = "StoreScene";
    [SerializeField] private string shopSceneName     = "ShopScene";

    [Header("Optional")]
    [SerializeField] private bool dontDestroyOnLoad = true;

    private static ShopNavigator _instance; // guard against duplicates

    private void Awake()
    {
        if (dontDestroyOnLoad)
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject); // a second one appeared—remove it
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }

    public void GoToShop()     => LoadByName(shopSceneName);
    public void GoToOpenShop() => LoadByName(openShopSceneName);

    private static void LoadByName(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogError("[ShopNavigator] Scene name is empty.");
            return;
        }

#if UNITY_EDITOR
        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogError($"[ShopNavigator] Scene \"{sceneName}\" is not in Build Settings.");
            return;
        }
#endif
        SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
    }
}
