using Photon.Pun;
using UnityEngine;
using System.Collections;

public class PetSpawner : MonoBehaviour
{
    private void Start()
    {
        var sel = PetSelectionManager.instance?.currentPet;
        if (sel == null || sel.prefab == null) return;

        var player = GameObject.Find("Player");
        Vector3 spawnPos = player ? player.transform.position + new Vector3(1, 0, 0) : Vector3.zero;

        GameObject petGO = (PhotonNetwork.IsConnected && PhotonNetwork.InRoom)
            ? PhotonNetwork.Instantiate(sel.prefab.name, spawnPos, Quaternion.identity)
            : Instantiate(sel.prefab, spawnPos, Quaternion.identity);

        StartCoroutine(WireToUIWhenReady(petGO));
    }

    IEnumerator WireToUIWhenReady(GameObject petGO)
    {
        WalkUIManager mgr = null;
        float timeout = Time.time + 2f; // try up to 2 seconds
        while (mgr == null && Time.time < timeout)
        {
            mgr = FindObjectOfType<WalkUIManager>();
            if (mgr == null) yield return null;
        }
        if (mgr)
        {
            mgr.pet = petGO.transform;
            mgr.petAnimator = petGO.GetComponent<Animator>(); // ok if null
        }
    }
}
