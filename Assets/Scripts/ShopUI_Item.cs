using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;

public class ShopUI_Item : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI ItemName;
    [SerializeField] TextMeshProUGUI Description;
    [SerializeField] TextMeshProUGUI Price;
    [SerializeField] Image BackgroundPanel;
    [SerializeField] Color DefaultColour = Color.white;
    [SerializeField] Color SelectedColour = Color.yellow;

    ShopItem Item;
    UnityAction<ShopItem> OnSelectedFn;

    public void Bind(ShopItem item) => Bind(item, null);

    public void Bind(ShopItem item, UnityAction<ShopItem> onSelectedFn)
    {
        Item = item;
        OnSelectedFn = onSelectedFn;

        if (ItemName)    ItemName.text = Item.Name;
        if (Description) Description.text = Item.Description;
        if (Price)       Price.text = $"${Item.Cost}"; // COINS (no /100f)

        SetIsSelected(false);
    }

    public void SetIsSelected(bool selected)
    {
        if (BackgroundPanel)
            BackgroundPanel.color = selected ? SelectedColour : DefaultColour;
    }

    public void SetCanAfford(bool canAfford)
    {
        if (!Price) return;
        Price.fontStyle = canAfford ? FontStyles.Normal : FontStyles.Strikethrough;
        Price.color     = canAfford ? Color.white : Color.red;
    }

    public void OnClicked() => OnSelectedFn?.Invoke(Item);
}
