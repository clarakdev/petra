using UnityEngine;

public enum EquipType
{
    None,
    GreenHat,
    StarSunglasses
}

[CreateAssetMenu(fileName = "ShopItem_", menuName = "Shop/Item")]
public class ShopItem : ScriptableObject
{
    public ShopItemCategory Category;
    public string Name;
    [TextArea(3, 5)] public string Description;
    public int Cost;
    public Sprite Icon;
    public EquipType EquipType = EquipType.None; // accessory type (Hat, Glasses, or None)

    // Lowercase properties for compatibility
    public ShopItemCategory category => Category;
    public string itemName => Name;
    public string description => Description;
    public int cost => Cost;
    public Sprite icon => Icon;
}
