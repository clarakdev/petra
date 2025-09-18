using UnityEngine;

public class BuildingEntrance : MonoBehaviour
{
    public string targetScene;
    private SceneSwapper sceneSwapper;
    private bool isPlayerNearby = false;
    private SpriteRenderer spriteRenderer;
    private Color originalColor;

    [Header("UI Prompt")]
    public GameObject interactPrompt; // assign a TMP text in inspector

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
        if (isPlayerNearby && Input.GetKeyDown(KeyCode.E))
        {
            sceneSwapper.LoadScene(targetScene);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerNearby = true;

            // Glow
            if (spriteRenderer != null)
                //light blue glow
                spriteRenderer.color = new Color(0.8f, 0.9f, 1f); 


            //Show "Press E" prompt
            if (interactPrompt != null)
                interactPrompt.SetActive(true);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerNearby = false;

            // Reset glow
            if (spriteRenderer != null)
                spriteRenderer.color = originalColor;

            // Hide "Press E" prompt
            if (interactPrompt != null)
                interactPrompt.SetActive(false);
        }
    }
}
