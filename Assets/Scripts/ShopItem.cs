using UnityEngine;

[CreateAssetMenu(menuName = "Shop/Item", fileName = "ShopItem_")]
public class ShopItem : ScriptableObject
{
    public ShopItemCategory Category;   
    public string Name;
    [TextArea(3, 5)] public string Description;
    public int Cost;                    
    public Sprite Icon;
    
}
