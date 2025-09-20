using System.Reflection;
using UnityEngine;

public class ShopToInventory : MonoBehaviour
{
    [SerializeField] private ShopUI shopUI;       
    [SerializeField] private Purchaser purchaser; 

    private int lastCurrency;
    private FieldInfo selectedItemField;
    private float debounceTimer;

    void Awake()
    {
        if (!purchaser) purchaser = FindFirstObjectByType<Purchaser>();
        if (!shopUI) shopUI = FindFirstObjectByType<ShopUI>();

        selectedItemField = typeof(ShopUI)
            .GetField("SelectedItem", BindingFlags.Instance | BindingFlags.NonPublic);

        if (purchaser != null)
            lastCurrency = purchaser.GetCurrentCurrency();
    }

    void Update()
    {
        if (purchaser == null || shopUI == null || InventoryManager.Instance == null) return;

        int currencyNow = purchaser.GetCurrentCurrency();

        if (currencyNow < lastCurrency && debounceTimer <= 0f)
        {
            var item = selectedItemField?.GetValue(shopUI) as ShopItem;
            if (item != null)
            {
                InventoryManager.Instance.AddItem(item); 
                Debug.Log($"[Bridge] Added {item.Name} to inventory.");
                debounceTimer = 0.1f;
            }
        }

        lastCurrency = currencyNow;
        if (debounceTimer > 0f) debounceTimer -= Time.deltaTime;
    }
}