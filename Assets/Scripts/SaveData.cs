using System;
using System.Collections.Generic;

[Serializable]
public class SaveData
{
    public string sceneName;
    public string currentPetName;
    public int selectedPetIndex;
    public int coins;
    public List<InventoryItemData> inventoryItems = new List<InventoryItemData>();

    [Serializable]
    public class InventoryItemData
    {
        public string id;
        public int qty;

        public InventoryItemData(string id, int qty)
        {
            this.id = id;
            this.qty = qty;
        }
    }
}