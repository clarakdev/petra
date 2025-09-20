using Photon.Pun;
using UnityEngine;

public class PetSpawner : MonoBehaviour
{
    [SerializeField] private Canvas uiCanvas;

    private void Start()
    {
        Debug.Log("[PetSpawner] Spawned at: " + transform.position);

        var pet = PetSelectionManager.instance?.currentPet;
        if (pet == null || pet.prefab == null)
        {
            return;
        }

        // Find the player and spawn the pet next to them
        var player = GameObject.Find("Player");
        Vector3 spawnPosition = player != null ? player.transform.position + new Vector3(1, 0, 0) : Vector3.zero;

        if (PhotonNetwork.IsConnected && PhotonNetwork.InRoom)
        {
             PhotonNetwork.Instantiate(pet.prefab.name, spawnPosition, Quaternion.identity);
        }
        else
        {
            Instantiate(pet.prefab, spawnPosition, Quaternion.identity);
        }
    }
}
