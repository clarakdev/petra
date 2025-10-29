using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class AutoLoadManager : MonoBehaviour
{
    [Header("Scene Settings")]
    [Tooltip("The scene to load when a save file exists")]
    public string targetSceneName = "main";

    [Header("Delay Settings")]
    [Tooltip("Delay before checking for save (in seconds)")]
    public float initialDelay = 0.3f;

    private void Start()
    {
        string currentScene = SceneManager.GetActiveScene().name;
        Debug.Log($"[AutoLoadManager] Current scene: {currentScene}");
        
        // Only auto-load if we're in the pet selection scene (case insensitive)
        if (currentScene.ToLower() == "petselection")
        {
            Debug.Log("[AutoLoadManager] In pet selection scene. Starting auto-load check...");
            StartCoroutine(CheckAndAutoLoad());
        }
        else
        {
            Debug.Log($"[AutoLoadManager] Not in pet selection scene. Skipping auto-load.");
        }
    }

    private IEnumerator CheckAndAutoLoad()
    {
        // Wait a moment for all managers to initialize
        yield return new WaitForSeconds(initialDelay);

        // Wait for SaveSystem to initialize
        int attempts = 0;
        while (SaveSystem.Instance == null && attempts < 50)
        {
            Debug.Log("[AutoLoadManager] Waiting for SaveSystem to initialize...");
            yield return new WaitForSeconds(0.1f);
            attempts++;
        }

        if (SaveSystem.Instance == null)
        {
            Debug.LogError("[AutoLoadManager] SaveSystem never initialized! Make sure SaveSystem exists in the scene.");
            yield break;
        }

        // Wait for PetSelectionManager
        attempts = 0;
        while (PetSelectionManager.instance == null && attempts < 50)
        {
            Debug.Log("[AutoLoadManager] Waiting for PetSelectionManager to initialize...");
            yield return new WaitForSeconds(0.1f);
            attempts++;
        }

        if (PetSelectionManager.instance == null)
        {
            Debug.LogError("[AutoLoadManager] PetSelectionManager never initialized!");
            yield break;
        }

        // Check if save file exists
        bool hasSave = SaveSystem.Instance.HasSaveFile();
        Debug.Log($"[AutoLoadManager] Save file check result: {hasSave}");

        if (hasSave)
        {
            Debug.Log("[AutoLoadManager] Save file detected! Loading game data...");
            
            // Load the game data into managers
            bool loadSuccess = SaveSystem.Instance.LoadGame();
            
            if (loadSuccess)
            {
                Debug.Log("[AutoLoadManager] Game data loaded successfully. Transitioning to main scene...");
                yield return new WaitForSeconds(0.5f);
                SceneManager.LoadScene(targetSceneName);
            }
            else
            {
                Debug.LogError("[AutoLoadManager] Failed to load game data.");
            }
        }
        else
        {
            Debug.Log("[AutoLoadManager] No save file found. Player must select a pet.");
        }
    }

    // Optional: Call this if you want to manually skip the auto-load (e.g., "New Game" button)
    public void DisableAutoLoad()
    {
        StopAllCoroutines();
        Debug.Log("[AutoLoadManager] Auto-load cancelled.");
    }
}