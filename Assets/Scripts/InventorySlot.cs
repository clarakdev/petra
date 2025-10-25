using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class InventorySlot : MonoBehaviour, IPointerClickHandler
{
    [Header("UI")]
    [SerializeField] private Image icon;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI qtyText;

    private ShopItem storedItem;

    public void ShowEmpty()
    {
        if (icon)    { icon.sprite = null; icon.enabled = false; }
        if (nameText) nameText.text = "";
        if (qtyText)  qtyText.text  = "";
        storedItem = null;
    }

    public void ShowItem(ShopItem item, int qty)
    {
        storedItem = item;

        if (icon)
        {
            icon.sprite  = item ? item.Icon : null;  
            icon.enabled = icon.sprite != null;
            if (icon) icon.preserveAspect = true;
        }
        if (nameText) nameText.text = item ? item.Name : "";       
        if (qtyText)  qtyText.text  = qty > 1 ? $"x{qty}" : "";
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (storedItem == null) return;

        // Only accessories can be equipped
        if (storedItem.EquipType == EquipType.GreenHat || storedItem.EquipType == EquipType.StarSunglasses)
        {
            var petMgr = PetSelectionManager.instance;
            if (petMgr && petMgr.currentPet != null)
            {
                GameObject petObj = petMgr.currentPet.prefab != null
                    ? petMgr.currentPet.prefab
                    : petMgr.currentPet.petPrefab;

                if (petObj)
                {
                    var accessory = petObj.GetComponent<PetAccessoryManager>();
                    if (accessory)
                        accessory.ToggleAccessory(storedItem.EquipType);
                }
            }
        }
        else
        {
            Debug.Log($"[Inventory] {storedItem.Name} is not an equipable accessory.");
        }
    }
}
