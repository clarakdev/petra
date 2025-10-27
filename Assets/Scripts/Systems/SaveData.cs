using System;
using System.Collections.Generic;

[Serializable]
public class SaveData
{
    public int version = 1;

    // Main data
    public string currentScene;
    public int currency;
    public string selectedPetId;
    public string lastSavedUtc;

    // ✅ New: Inventory data
    public List<InventoryItemSave> inventory;
}

[Serializable]
public class InventoryItemSave
{
    public string itemId;
    public int quantity;
}
