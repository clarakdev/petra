using Photon.Pun;
using UnityEngine;
using System.Collections;

public class PetSpawner : MonoBehaviour
{
    private void Start()
    {
        var sel = PetSelectionManager.instance?.currentPet;
        if (sel == null || sel.prefab == null) return;

        // Find the player in the scene
        var player = GameObject.Find("Player");
        Vector3 spawnPos = player 
            ? player.transform.position + new Vector3(1, 0, 0) 
            : Vector3.zero;

        // Spawn pet either via PhotonNetwork or locally
        GameObject petGO = (PhotonNetwork.IsConnected && PhotonNetwork.InRoom)
            ? PhotonNetwork.Instantiate(sel.prefab.name, spawnPos, Quaternion.identity)
            : Instantiate(sel.prefab, spawnPos, Quaternion.identity);

        // Wait for the UI manager to be ready before linking
        StartCoroutine(WireToUIWhenReady(petGO));
    }

    private IEnumerator WireToUIWhenReady(GameObject petGO)
    {
        WalkUIManager mgr = null;
        float timeout = Time.time + 2f; // Try up to 2 seconds

        while (mgr == null && Time.time < timeout)
        {
            mgr = FindObjectOfType<WalkUIManager>();
            if (mgr == null)
                yield return null;
        }

        // Link the pet to the Walk UI Manager
        if (mgr)
        {
            mgr.pet = petGO.transform;
            mgr.petAnimator = petGO.GetComponent<Animator>(); // Optional if pet has animation
        }
    }
}
