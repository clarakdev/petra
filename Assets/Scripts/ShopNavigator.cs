using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;

public class ShopNavigator : MonoBehaviour
{
    public static ShopNavigator Instance { get; private set; }
    [Header("Scene Names (must match Build Settings)")]
    [SerializeField] private string storeSceneName = "StoreScene";
    [SerializeField] private string shopSceneName  = "ShopScene";

    [Header("Store UI (assign ONE)")]
    [SerializeField] private TMP_Text infoTMP;  // drag child "Text (TMP)" here
    [SerializeField] private Text     infoUGUI; // legacy Text

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
    private static ShopNavigator _instance;
    private const string CameFromShopKey = "CameFromShop";
    private static bool cameFromShop; // in-memory (works when navigator persists)

    private void Awake()
    {
        if (dontDestroyOnLoad)
        {
            if (_instance != null && _instance != this) { Destroy(gameObject); return; }
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }

    private void Start()
    {
        if (SceneManager.GetActiveScene().name != storeSceneName) return;

        // Fallback to PlayerPrefs (works even if navigator didn't persist)
        if (PlayerPrefs.GetInt(CameFromShopKey, 0) == 1)
        {
            cameFromShop = true;
            PlayerPrefs.SetInt(CameFromShopKey, 0);
            PlayerPrefs.Save();
        }

        string msg = cameFromShop ? "Thank you! Come again soon." : "Welcome! What would you like to buy?";
        if      (infoTMP  != null) infoTMP.text  = msg;
        else if (infoUGUI != null) infoUGUI.text = msg;

        cameFromShop = false; // reset in-memory flag
    }

    public void OpenShop()
    {
        LoadByName(shopSceneName);
    }

    public void ReturnToStore()
    {
        cameFromShop = true; // in-memory
        PlayerPrefs.SetInt(CameFromShopKey, 1); // persistent fallback
        PlayerPrefs.Save();
        LoadByName(storeSceneName);
    }
    static void LoadByName(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName)) return;
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
