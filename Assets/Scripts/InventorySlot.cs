using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InventorySlot : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Image icon;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI qtyText;

    public void ShowEmpty()
    {
        if (icon)    { icon.sprite = null; icon.enabled = false; }
        if (nameText) nameText.text = "";
        if (qtyText)  qtyText.text  = "";
    }

    public void ShowItem(ShopItem item, int qty)
    {
        if (icon)
        {
            icon.sprite  = item ? item.Icon : null;  
            icon.enabled = icon.sprite != null;
            if (icon) icon.preserveAspect = true;
        }
        if (nameText) nameText.text = item ? item.Name : "";       
        if (qtyText)  qtyText.text  = qty > 1 ? $"x{qty}" : "";
    }
}