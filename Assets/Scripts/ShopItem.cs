using UnityEngine;

[CreateAssetMenu(fileName = "ShopItem_", menuName = "Shop/Item")]
public class ShopItem : ScriptableObject
{
    public ShopItemCategory Category;
    public string Name;
    [TextArea(3, 5)] public string Description;
    public int Cost;
    public Sprite Icon;
}
