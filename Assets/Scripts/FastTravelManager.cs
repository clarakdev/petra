using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class FastTravelManager : MonoBehaviour
{
    [Header("References")]
    public Transform player;
    public Transform pet;
    private MapController mapController;

    [Header("Teleport Locations")]
    public Transform shopSpawn;
    public Transform socialSpawn;
    public Transform battleSpawn;
    public Transform homeSpawn;

    [Header("Travel Settings")]
    public float travelSpeed = 25f;   // 🚀 Both player and pet move at speed 25
    public float followOffset = 1f;   // Distance between player & pet

    private bool isTravelling = false;

    void Awake()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void Start()
    {
        mapController = FindObjectOfType<MapController>();
        FindEntities();
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log("[FastTravelManager] Scene loaded: " + scene.name);

        mapController = FindObjectOfType<MapController>();
        FindEntities();

        if (mapController != null && mapController.mapPanel != null)
        {
            mapController.CloseMap();
            Debug.Log("[FastTravelManager] MapController re-linked and map closed.");
        }
    }

    void FindEntities()
    {
        if (player == null)
        {
            var p = GameObject.FindWithTag("Player");
            if (p != null) player = p.transform;
        }

        if (pet == null)
        {
            var petObj = GameObject.FindWithTag("Pet");
            if (petObj == null) petObj = GameObject.Find("Pet_2(Clone)");
            if (petObj != null) pet = petObj.transform;
        }
    }

    // === BUTTON FUNCTIONS ===
    public void GoToShop() => StartTravel(shopSpawn, "Pet Shop");
    public void GoToSocialRoom() => StartTravel(socialSpawn, "Social Room");
    public void GoToBattleRoom() => StartTravel(battleSpawn, "Battle Room");
    public void GoHome() => StartTravel(homeSpawn, "Home");

    // === TRAVEL COROUTINE ===
    private void StartTravel(Transform destination, string locationName)
    {
        if (isTravelling) return;
        if (destination == null)
        {
            Debug.LogWarning($"[FastTravelManager] Destination for {locationName} not set!");
            return;
        }

        FindEntities();

        if (player == null)
        {
            Debug.LogError("[FastTravelManager] Player not found!");
            return;
        }

        StartCoroutine(MoveToLocation(destination.position, locationName));
    }

    private IEnumerator MoveToLocation(Vector3 destination, string locationName)
    {
        isTravelling = true;

        Vector3 petOffset = new Vector3(followOffset, 0, 0);
        Rigidbody2D playerRb = player.GetComponent<Rigidbody2D>();
        Rigidbody2D petRb = pet != null ? pet.GetComponent<Rigidbody2D>() : null;
        Collider2D playerCol = player.GetComponent<Collider2D>();
        Collider2D petCol = pet != null ? pet.GetComponent<Collider2D>() : null;

        // 🚫 Temporarily disable collisions and physics
        if (playerRb != null) playerRb.simulated = false;
        if (playerCol != null) playerCol.enabled = false;
        if (petRb != null) petRb.simulated = false;
        if (petCol != null) petCol.enabled = false;

        Debug.Log($"[FastTravelManager] Travelling to {locationName} at speed {travelSpeed}...");

        // Move both smoothly to the destination
        while (Vector3.Distance(player.position, destination) > 0.05f)
        {
            player.position = Vector3.MoveTowards(player.position, destination, travelSpeed * Time.unscaledDeltaTime);

            if (pet != null)
            {
                Vector3 petTarget = player.position - petOffset;
                pet.position = Vector3.MoveTowards(pet.position, petTarget, travelSpeed * Time.unscaledDeltaTime);
            }

            yield return null;
        }

        // Snap both exactly to destination
        player.position = destination;
        if (pet != null)
            pet.position = destination - petOffset;

        // ✅ Re-enable collisions and physics
        if (playerRb != null) playerRb.simulated = true;
        if (playerCol != null) playerCol.enabled = true;
        if (petRb != null) petRb.simulated = true;
        if (petCol != null) petCol.enabled = true;

        Debug.Log($"[FastTravelManager] Arrived at {locationName}");

        mapController?.CloseMap();
        isTravelling = false;
    }
}
