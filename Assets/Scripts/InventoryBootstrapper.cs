using UnityEngine;

public class InventoryBootstrapper : MonoBehaviour
{
    [SerializeField] private InventoryManager managerPrefab; 

    private void Awake()
    {
        if (InventoryManager.Instance != null) return;

        var m = Instantiate(managerPrefab);
    }
}