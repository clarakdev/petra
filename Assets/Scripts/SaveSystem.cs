using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SaveSystem : MonoBehaviour
{
    public static SaveSystem Instance { get; private set; }

    private const string SAVE_FILE_NAME = "savefile.json";
    private const string HAS_SAVE_KEY = "HasSaveFile";
    private string SavePath => Path.Combine(Application.persistentDataPath, SAVE_FILE_NAME);

    // Cache the loaded data so it can be applied later
    private SaveData cachedSaveData;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        
        // Subscribe to scene loaded event to apply inventory after scene loads
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // If we have cached save data and inventory exists now, load the inventory
        if (cachedSaveData != null && InventoryManager.Instance != null)
        {
            Debug.Log($"[SaveSystem] Scene loaded: {scene.name}. Applying cached inventory data...");
            LoadInventoryFromCache();
        }
    }

    // Call this from your Save button
    public void SaveGame()
    {
        try
        {
            SaveData data = new SaveData();

            // Save current scene
            data.sceneName = SceneManager.GetActiveScene().name;

            // Save pet data
            if (PetSelectionManager.instance != null && PetSelectionManager.instance.currentPet != null)
            {
                data.currentPetName = PetSelectionManager.instance.currentPet.petName;
                data.selectedPetIndex = PetSelectionManager.SelectedPetIndex;
            }

            // Save coins
            if (PlayerCurrency.Instance != null)
            {
                data.coins = PlayerCurrency.Instance.currency;
            }

            // Save inventory
            if (InventoryManager.Instance != null)
            {
                foreach (var entry in InventoryManager.Instance.Stacks)
                {
                    if (entry.item != null)
                    {
                        string itemId = InventoryManager.Instance.GetItemId(entry.item);
                        data.inventoryItems.Add(new SaveData.InventoryItemData(itemId, entry.quantity));
                    }
                }
            }

            // Write to file
            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(SavePath, json);

            // Mark that a save file exists
            PlayerPrefs.SetInt(HAS_SAVE_KEY, 1);
            PlayerPrefs.Save();

            Debug.Log($"[SaveSystem] Game saved successfully to {SavePath}");
            Debug.Log($"[SaveSystem] Saved data: Pet={data.currentPetName}, Coins={data.coins}, Items={data.inventoryItems.Count}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[SaveSystem] Failed to save game: {e.Message}");
        }
    }

    // Call this on game start to auto-load
    public bool LoadGame()
    {
        if (!HasSaveFile())
        {
            Debug.Log("[SaveSystem] No save file found.");
            return false;
        }

        try
        {
            string json = File.ReadAllText(SavePath);
            SaveData data = JsonUtility.FromJson<SaveData>(json);

            if (data == null)
            {
                Debug.LogError("[SaveSystem] Failed to parse save file.");
                return false;
            }

            // Cache the save data for later use
            cachedSaveData = data;

            // Load pet
            if (PetSelectionManager.instance != null && !string.IsNullOrEmpty(data.currentPetName))
            {
                Pet selectedPet = null;
                for (int i = 0; i < PetSelectionManager.instance.pets.Length; i++)
                {
                    if (PetSelectionManager.instance.pets[i].petName == data.currentPetName)
                    {
                        selectedPet = PetSelectionManager.instance.pets[i];
                        PetSelectionManager.SelectedPetIndex = i;
                        break;
                    }
                }

                if (selectedPet != null)
                {
                    PetSelectionManager.instance.SetPet(selectedPet);
                    Debug.Log($"[SaveSystem] Loaded pet: {data.currentPetName}");
                }
            }

            // Load coins
            if (PlayerCurrency.Instance != null)
            {
                PlayerCurrency.Instance.currency = data.coins;
                PlayerCurrency.Instance.OnCurrencyChanged?.Invoke(data.coins);
                Debug.Log($"[SaveSystem] Loaded coins: {data.coins}");
            }

            // Try to load inventory immediately if it exists
            if (InventoryManager.Instance != null)
            {
                LoadInventoryFromCache();
            }
            else
            {
                Debug.Log($"[SaveSystem] InventoryManager not found yet. Will load {data.inventoryItems.Count} items when available.");
            }

            Debug.Log($"[SaveSystem] Game loaded successfully from {SavePath}");
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[SaveSystem] Failed to load game: {e.Message}");
            return false;
        }
    }

    private void LoadInventoryFromCache()
    {
        if (cachedSaveData == null || InventoryManager.Instance == null)
        {
            return;
        }

        InventoryManager.Instance.Clear();
        foreach (var itemData in cachedSaveData.inventoryItems)
        {
            InventoryManager.Instance.AddItemById(itemData.id, itemData.qty);
        }
        Debug.Log($"[SaveSystem] Loaded {cachedSaveData.inventoryItems.Count} inventory items");
        
        // Clear cache after successful load
        cachedSaveData = null;
    }

    public bool HasSaveFile()
    {
        return PlayerPrefs.GetInt(HAS_SAVE_KEY, 0) == 1 && File.Exists(SavePath);
    }

    public void DeleteSave()
    {
        if (File.Exists(SavePath))
        {
            File.Delete(SavePath);
        }
        PlayerPrefs.DeleteKey(HAS_SAVE_KEY);
        PlayerPrefs.Save();
        cachedSaveData = null;
        Debug.Log("[SaveSystem] Save file deleted.");
    }

    // Helper method to get save file info (optional, for debugging)
    public string GetSaveFilePath()
    {
        return SavePath;
    }
}