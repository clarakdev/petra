using UnityEngine;
using UnityEngine.SceneManagement;

public class GameState : MonoBehaviour
{
    public static GameState Instance { get; private set; }

    private PlayerCurrency playerCurrency;
    private PetSelectionManager petSelectionManager; // replace with your actual pet selection class name later

    private void Awake()
    {
        // Singleton setup
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        SceneManager.sceneLoaded += OnSceneLoaded;
        CacheReferences();
    }

    private void Start()
    {
        //LoadNow(); // Attempt to load save data on game start
    }   

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        CacheReferences();
    }

private void CacheReferences()
{
    if (playerCurrency == null)
    {
        playerCurrency = FindObjectOfType<PlayerCurrency>();

        //Create one automatically if missing
        if (playerCurrency == null)
        {
            Debug.LogWarning("[GameState] No PlayerCurrency found, creating one...");
            GameObject obj = new GameObject("PlayerCurrency");
            playerCurrency = obj.AddComponent<PlayerCurrency>();
        }
    }

    if (petSelectionManager == null)
        petSelectionManager = FindObjectOfType<PetSelectionManager>();
}



    // ----- SAVE / LOAD -----

    public void SaveNow()
    {
        CacheReferences();

        if (playerCurrency == null)
        {
            Debug.LogWarning("[GameState] PlayerCurrency not found — retrying in 0.1s...");
            Invoke(nameof(SaveNow), 0.1f);
            return;
        }

        var data = new SaveData
        {
            version = 1,
            currentScene = SceneManager.GetActiveScene().name,
            currency = playerCurrency.currency,
            selectedPetId = petSelectionManager != null && petSelectionManager.currentPet != null
                ? petSelectionManager.currentPet.name
                : null,
            lastSavedUtc = System.DateTime.Now.ToString("dd MMM yyyy, hh:mm tt")
        };

    SaveSystem.Save(data);
}


    public void LoadNow()
    {
        var data = SaveSystem.Load();
        if (data == null)
        {
            Debug.Log("[GameState] No save data found.");
            return;
        }

        // Apply loaded data
        if (playerCurrency != null)
            playerCurrency.currency = data.currency;

        if (petSelectionManager != null && !string.IsNullOrEmpty(data.selectedPetId))
        {
            foreach (var pet in petSelectionManager.pets)
            {
                if (pet.name == data.selectedPetId)
                {
                    petSelectionManager.SetPet(pet);
                    break;
                }
            }
        }


        /*// Optional: automatically load the saved scene
        string current = SceneManager.GetActiveScene().name;
        if (!string.IsNullOrEmpty(data.currentScene) && data.currentScene != current)
        {
            SceneManager.LoadScene(data.currentScene);
        }
        */
    }

    private void OnApplicationQuit()
    {
        SaveNow(); // autosave when quitting
    }
}
