using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;

public class ShopUI_Category : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI CategoryName;
    [SerializeField] Image BackgroundPanel;
    [SerializeField] Color DefaultColour = Color.white;
    [SerializeField] Color SelectedColour = Color.yellow;

    ShopItemCategory Category;
    UnityAction<ShopItemCategory> OnSelectedFn;

    public void Bind(ShopItemCategory category, UnityAction<ShopItemCategory> onSelectedFn)
    {
        Category = category;
        OnSelectedFn = onSelectedFn;
        CategoryName.text = Category.Name;
        SetIsSelected(false);
    }

    public void SetIsSelected(bool selected)
    {
        if (BackgroundPanel != null)
            BackgroundPanel.color = selected ? SelectedColour : DefaultColour;
    }

    public void OnClicked()
    {
        OnSelectedFn?.Invoke(Category);
    }
}
