using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class ShopUI_Item : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI ItemName;
    [SerializeField] private TextMeshProUGUI Description;
    [SerializeField] private TextMeshProUGUI Price;

    private ShopItem Item;

    public void Bind(ShopItem item)
    {
        Item = item;

        ItemName.text = Item.Name;
        Description.text = Item.Description;
        Price.text = $"${(Item.Cost / 100):0.00}";
    }
}
