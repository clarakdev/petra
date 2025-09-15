using UnityEngine;

public class PetSpawner : MonoBehaviour
{
    [SerializeField] private Canvas uiCanvas;

    private void Start()
    {
        var pet = PetSelectionManager.instance?.currentPet;
        if (pet == null || pet.prefab == null)
        {
            return;
        }

        // Find the player and spawn the pet next to them
        var player = GameObject.Find("Player");
        Vector3 spawnPosition = player != null ? player.transform.position + new Vector3(1, 0, 0) : Vector3.zero;

        Instantiate(pet.prefab, spawnPosition, Quaternion.identity);
    }
}
