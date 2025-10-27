using UnityEngine;

public class MapController : MonoBehaviour
{
    [Header("Map Panel (UI)")]
    public GameObject mapPanel; // Main map UI panel with image + buttons

    [Header("Key Settings")]
    public KeyCode toggleKey = KeyCode.M; // Press 'M' to toggle map

    [HideInInspector] public bool isOpen = false;

    void Start()
    {
        if (mapPanel != null)
            mapPanel.SetActive(false);
        else
            Debug.LogWarning("[MapController] MapPanel not assigned in Inspector!");
    }

    void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            ToggleMap();
        }
    }

    public void ToggleMap()
    {
        if (mapPanel == null)
        {
            Debug.LogError("[MapController] MapPanel not assigned!");
            return;
        }

        if (isOpen)
            CloseMap();
        else
            OpenMap();
    }

    public void OpenMap()
    {
        mapPanel.SetActive(true);
        Time.timeScale = 1f; // Allow player movement even when map is open
        isOpen = true;
        Debug.Log("[MapController] Map opened!");
    }

    public void CloseMap()
    {
        mapPanel.SetActive(false);
        Time.timeScale = 1f;
        isOpen = false;
        Debug.Log("[MapController] Map closed!");
    }
}
