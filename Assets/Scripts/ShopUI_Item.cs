using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using TMPro;

[RequireComponent(typeof(Image))]
public class ShopUI_Item : MonoBehaviour, IPointerClickHandler
{
    [Header("UI")]
    [SerializeField] private TextMeshProUGUI ItemName;
    [SerializeField] private TextMeshProUGUI Description;
    [SerializeField] private TextMeshProUGUI Price;
    [SerializeField] private Image BackgroundPanel;

    [Header("Icon (slot on LEFT)")]
    [SerializeField] private Image IconImage;               // assign child "Icon"
    [SerializeField] private bool  HideIconWhenNull = true; // hide if no sprite

    [Header("Selection")]
    [SerializeField] private Color DefaultColour  = Color.white;
    [SerializeField] private Color SelectedColour = Color.yellow;

    private ShopItem Item;
    private UnityAction<ShopItem> OnSelectedFn;

    private void Awake()
    {
        var root = GetComponent<Image>();
        if (root) root.raycastTarget = true;

        if (!BackgroundPanel) BackgroundPanel = root;

        if (ItemName)    ItemName.raycastTarget    = false;
        if (Description) Description.raycastTarget = false;
        if (Price)       Price.raycastTarget       = false;

        if (IconImage)
        {
            IconImage.raycastTarget  = false;
            IconImage.preserveAspect = true;
        }
    }

    // keep your existing callsites
    public void Bind(ShopItem item)                                   => Bind(item, (Sprite)null, null);
    public void Bind(ShopItem item, UnityAction<ShopItem> onSelected) => Bind(item, (Sprite)null, onSelected);

    // optional icon override
    public void Bind(ShopItem item, Sprite icon, UnityAction<ShopItem> onSelectedFn = null)
    {
        Item         = item;
        OnSelectedFn = onSelectedFn;

        if (ItemName)    ItemName.text    = item != null ? item.Name        : "";
        if (Description) Description.text = item != null ? item.Description : "";
        if (Price)       Price.text       = item != null ? $"${item.Cost}"  : "";

        if (icon == null && item) icon = item.Icon;
        ApplyIcon(icon);

        SetIsSelected(false);
    }

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

    private void ApplyIcon(Sprite sprite)
    {
        if (!IconImage) return;
        IconImage.sprite  = sprite;
        IconImage.enabled = sprite != null || !HideIconWhenNull;
        IconImage.preserveAspect = true;
    }

#if UNITY_EDITOR
    // tiny helper to auto-find/create the icon slot on the LEFT
    private void OnValidate()
    {
        if (!BackgroundPanel) BackgroundPanel = GetComponent<Image>();
        if (!IconImage)
        {
            var t = transform.Find("Icon");
            if (t) IconImage = t.GetComponent<Image>();
        }
        if (!IconImage)
        {
            var go = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(transform, false);
            // LEFT–Middle slot
            rt.anchorMin = new Vector2(0f, 0.5f);
            rt.anchorMax = new Vector2(0f, 0.5f);
            rt.pivot     = new Vector2(0f, 0.5f);
            rt.sizeDelta = new Vector2(64, 64);
            rt.anchoredPosition = new Vector2(12, 0);
            IconImage = go.GetComponent<Image>();
            IconImage.preserveAspect = true;
        }
    }
#endif

    public void OnPointerClick(PointerEventData _) => OnSelectedFn?.Invoke(Item);
    public void OnClicked()                        => OnSelectedFn?.Invoke(Item);
}
