using UnityEngine;
using Photon.Pun;

public class UniquePhotonObject : MonoBehaviourPun
{
    private void Awake()
    {
        // Prevent duplicates across teleports
        var objs = FindObjectsOfType<UniquePhotonObject>();
        if (objs.Length > 1)
        {
            Debug.LogWarning($"Duplicate Photon object destroyed: {name}");
            Destroy(gameObject);
            return;
        }

        DontDestroyOnLoad(gameObject);
    }
}
