using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;

public class InventoryManagerTests
{
    private GameObject testObject;
    private InventoryManager inventory;
    private ShopItem testItem1;
    private ShopItem testItem2;

    [SetUp]
    public void Setup()
    {
        // Create a GameObject with InventoryManager
        testObject = new GameObject("TestInventory");
        inventory = testObject.AddComponent<InventoryManager>();

        // Create test items (ScriptableObjects)
        testItem1 = ScriptableObject.CreateInstance<ShopItem>();
        testItem1.name = "TestItem1";

        testItem2 = ScriptableObject.CreateInstance<ShopItem>();
        testItem2.name = "TestItem2";
    }

    [TearDown]
    public void Teardown()
    {
        if (testObject != null)
            Object.DestroyImmediate(testObject);
        if (testItem1 != null)
            Object.DestroyImmediate(testItem1);
        if (testItem2 != null)
            Object.DestroyImmediate(testItem2);
    }

    // ===================================================
    // TEST 1: Adding a Single Item
    // ===================================================
    [Test]
    public void Test1_AddItem_SingleItem_ReturnsZeroLeftover()
    {
        // Arrange
        ShopItem item = testItem1;
        int quantity = 1;

        // Act
        int leftover = inventory.AddItem(item, quantity);

        // Assert
        Assert.AreEqual(0, leftover, "Should return 0 leftover when item is added successfully");
        Assert.AreEqual(1, inventory.UsedStacks, "Should have 1 stack in inventory");
    }

    // ===================================================
    // TEST 2: Stacking Same Items
    // ===================================================
    [Test]
    public void Test2_AddItem_SameItemTwice_StacksQuantity()
    {
        // Arrange
        ShopItem item = testItem1;

        // Act
        inventory.AddItem(item, 3);
        inventory.AddItem(item, 2);

        // Assert
        Assert.AreEqual(1, inventory.UsedStacks, "Should still have only 1 stack");

        var stacks = inventory.Stacks;
        Assert.AreEqual(5, stacks[0].quantity, "Total quantity should be 5 (3 + 2)");
    }

    // ===================================================
    // TEST 3: Maximum Stack Limit
    // ===================================================
    [Test]
    public void Test3_AddItem_ExceedsMaxStacks_ReturnsLeftover()
    {
        // Arrange
        // Set max stacks to 2 using reflection (since it's a SerializeField)
        var maxStacksField = typeof(InventoryManager).GetField("maxStacks",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        maxStacksField.SetValue(inventory, 2);

        // Act
        inventory.AddItem(testItem1, 1);  // Stack 1
        inventory.AddItem(testItem2, 1);  // Stack 2

        ShopItem testItem3 = ScriptableObject.CreateInstance<ShopItem>();
        testItem3.name = "TestItem3";
        int leftover = inventory.AddItem(testItem3, 1);  // Should fail - inventory full

        // Assert
        Assert.AreEqual(1, leftover, "Should return 1 leftover when inventory is full");
        Assert.AreEqual(2, inventory.UsedStacks, "Should still have only 2 stacks");

        Object.DestroyImmediate(testItem3);
    }

    // ===================================================
    // TEST 4: Removing Items Partially
    // ===================================================
    [Test]
    public void Test4_RemoveItem_PartialQuantity_ReducesStack()
    {
        // Arrange
        inventory.AddItem(testItem1, 5);

        // Act
        int removed = inventory.RemoveItem(testItem1, 2);

        // Assert
        Assert.AreEqual(2, removed, "Should remove 2 items");
        Assert.AreEqual(1, inventory.UsedStacks, "Stack should still exist");
        Assert.AreEqual(3, inventory.Stacks[0].quantity, "Remaining quantity should be 3");
    }

    // ===================================================
    // TEST 5: Removing Entire Stack
    // ===================================================
    [Test]
    public void Test5_RemoveItem_EntireStack_RemovesFromInventory()
    {
        // Arrange
        inventory.AddItem(testItem1, 3);

        // Act
        int removed = inventory.RemoveItem(testItem1, 3);

        // Assert
        Assert.AreEqual(3, removed, "Should remove all 3 items");
        Assert.AreEqual(0, inventory.UsedStacks, "Inventory should be empty");
    }
}