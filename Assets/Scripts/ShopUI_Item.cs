using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using TMPro;

[RequireComponent(typeof(Image))] // root needs an Image so it can receive clicks
public class ShopUI_Item : MonoBehaviour, IPointerClickHandler
{
    [Header("UI")]
    [SerializeField] TextMeshProUGUI ItemName;
    [SerializeField] TextMeshProUGUI Description;
    [SerializeField] TextMeshProUGUI Price;
    [SerializeField] Image           BackgroundPanel; // the row bg (can be the same Image as on root)

    [Header("Selection Colours")]
    [SerializeField] Color DefaultColour  = Color.white;
    [SerializeField] Color SelectedColour = Color.yellow;

    [Header("Icon")]
    [SerializeField] Image IconImage;               // drag the “Icon” child here in the prefab
    [SerializeField] bool  HideIconWhenNull = true; // hide the icon Image if no sprite set

    // Data + callback
    private ShopItem Item;
    private UnityAction<ShopItem> OnSelectedFn;

    // ---------------------- Lifecycle ----------------------

    void Awake()
    {
        // Make sure only the root receives clicks
        var rootImage = GetComponent<Image>();
        if (rootImage) rootImage.raycastTarget = true;

        if (ItemName)     ItemName.raycastTarget     = false;
        if (Description)  Description.raycastTarget  = false;
        if (Price)        Price.raycastTarget        = false;
        if (IconImage)
        {
            IconImage.raycastTarget  = false;
            IconImage.preserveAspect = true; // keep pixel icons square
        }

        // If BackgroundPanel not set, use the root Image as the highlight target
        if (!BackgroundPanel) BackgroundPanel = GetComponent<Image>();
    }

    // ---------------------- Bind API ----------------------

    // Basic bind (will use item.Icon if present)
    public void Bind(ShopItem item)                          { Bind(item, (Sprite)null, null); }
    public void Bind(ShopItem item, UnityAction<ShopItem> cb){ Bind(item, (Sprite)null, cb);   }

    // Bind with an explicit icon and optional onSelected callback
    public void Bind(ShopItem item, Sprite icon, UnityAction<ShopItem> onSelectedFn = null)
    {
        Item         = item;
        OnSelectedFn = onSelectedFn;

        if (ItemName)    ItemName.text    = Item.Name;
        if (Description) Description.text = Item.Description;
        if (Price)       Price.text       = $"${(Item.Cost / 100f):0.00}";

        // If no explicit icon passed, use the data’s icon (add 'public Sprite Icon;' to ShopItem)
        if (icon == null && Item != null) icon = Item.Icon;

        ApplyIcon(icon);
        SetIsSelected(false);
    }

    // ---------------------- UI Helpers ----------------------

    public void SetIsSelected(bool selected)
    {
        if (BackgroundPanel) BackgroundPanel.color = selected ? SelectedColour : DefaultColour;
    }

    public void SetCanAfford(bool canAfford)
    {
        if (!Price) return;
        Price.fontStyle = canAfford ? FontStyles.Normal : FontStyles.Strikethrough;
        Price.color     = canAfford ? Color.white       : Color.red;
    }

    public ShopItem GetBoundItem() => Item;
    public void     SetIcon(Sprite icon) => ApplyIcon(icon);

    void ApplyIcon(Sprite sprite)
    {
        if (!IconImage) return;

        IconImage.sprite  = sprite;
        IconImage.enabled = sprite != null || !HideIconWhenNull;
        // Ensure the Icon Image (in Inspector) has Image Type = Simple.
        IconImage.preserveAspect = true;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        OnSelectedFn?.Invoke(Item);
    }
}
