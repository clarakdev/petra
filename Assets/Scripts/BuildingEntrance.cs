using UnityEngine;
using UnityEngine.InputSystem; // ✅ New Input System

public class BuildingEntrance : MonoBehaviour
{
    [Header("Scene Settings")]
    public string targetScene;   // name of the scene to load
    private SceneSwapper sceneSwapper;

    [Header("Visuals")]
    private SpriteRenderer spriteRenderer;
    private Color originalColor;

    [Header("UI Prompt")]
    public GameObject interactPrompt; // assign TMP Text or UI object in inspector

    private bool isPlayerNearby = false;

    void Start()
    {
        sceneSwapper = FindObjectOfType<SceneSwapper>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer != null)
            originalColor = spriteRenderer.color;

        if (interactPrompt != null)
            interactPrompt.SetActive(false);
    }

    void Update()
    {
        // ✅ Use New Input System
        if (isPlayerNearby && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
        {
            if (sceneSwapper != null && !string.IsNullOrEmpty(targetScene))
                sceneSwapper.LoadScene(targetScene);
            else
                Debug.LogWarning("SceneSwapper or TargetScene not set on BuildingEntrance.");
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerNearby = true;

            // Glow effect
            if (spriteRenderer != null)
                spriteRenderer.color = new Color(0.8f, 0.9f, 1f); // light blue glow

            if (interactPrompt != null)
                interactPrompt.SetActive(true);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerNearby = false;

            if (spriteRenderer != null)
                spriteRenderer.color = originalColor;

            if (interactPrompt != null)
                interactPrompt.SetActive(false);
        }
    }
}
