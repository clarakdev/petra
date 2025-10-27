using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class GameState : MonoBehaviour
{
    public static GameState Instance { get; private set; }

    private PlayerCurrency playerCurrency;
    private PetSelectionManager petSelectionManager;

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
        // ✅ Automatically load save when game starts
        LoadNow();
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
            playerCurrency = FindFirstObjectByType<PlayerCurrency>();

            // Create one automatically if missing
            if (playerCurrency == null)
            {
                Debug.LogWarning("[GameState] No PlayerCurrency found, creating one...");
                GameObject obj = new GameObject("PlayerCurrency");
                playerCurrency = obj.AddComponent<PlayerCurrency>();
            }
        }

        if (petSelectionManager == null)
            petSelectionManager = FindFirstObjectByType<PetSelectionManager>();
    }

    // --------------------------------------------------
    // SAVE / LOAD
    // --------------------------------------------------

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
            lastSavedUtc = System.DateTime.Now.ToString("dd MMM yyyy, hh:mm tt"),

            // ✅ Include inventory if manager exists
            inventory = InventoryManager.Instance?.CaptureSave()
        };

        SaveSystem.Save(data);
        Debug.Log($"[GameState] Game saved successfully (scene={data.currentScene}, coins={data.currency}, pet={data.selectedPetId}).");
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

        // ✅ Restore inventory
        if (InventoryManager.Instance != null && data.inventory != null)
        {
            InventoryManager.Instance.RestoreFromSave(data.inventory);
        }

        // ✅ Restore pet safely with delay
        if (!string.IsNullOrEmpty(data.selectedPetId))
            StartCoroutine(DelayedPetRestore(data.selectedPetId));

        // ✅ Auto-load saved scene if different
        string current = SceneManager.GetActiveScene().name;
        if (!string.IsNullOrEmpty(data.currentScene) && data.currentScene != current)
        {
            Debug.Log($"[GameState] Auto-loading saved scene: {data.currentScene}");
            SceneManager.LoadScene(data.currentScene);
        }

        Debug.Log("[GameState] Game loaded successfully (including inventory & pet).");
    }

    private IEnumerator DelayedPetRestore(string petId)
    {
        // Wait one frame to ensure PetSelectionManager is ready
        yield return null;

        var petMgr = FindFirstObjectByType<PetSelectionManager>();
        if (petMgr == null)
        {
            Debug.LogWarning("[GameState] PetSelectionManager not found after scene load.");
            yield break;
        }

        foreach (var pet in petMgr.pets)
        {
            if (pet != null && pet.name == petId)
            {
                petMgr.SetPet(pet);
                Debug.Log($"[GameState] Pet restored: {pet.name}");
                yield break;
            }
        }

        Debug.LogWarning($"[GameState] Pet '{petId}' not found in list.");
    }

    private void OnApplicationQuit()
    {
        SaveNow(); // ✅ Auto-save when quitting
    }
}
