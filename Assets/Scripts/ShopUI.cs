using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using TMPro;

public class ShopUI : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI AvailableFunds;
    [SerializeField] Transform CategoryUIRoot;
    [SerializeField] Transform ItemUIRoot;

    [SerializeField] Button PurchaseButton;
    [SerializeField] GameObject PurchasePrefab;

    [SerializeField] GameObject CategoryUIPrefab;
    [SerializeField] GameObject ItemUIPrefab;

    [SerializeField] List<ShopItem> AvailableItems;

    [SerializeField] Purchaser PurchaserRef;
    [SerializeField] string ExitSceneName = "StoreScene";

    IPurchaser CurrentPurchaser;
    ShopItemCategory SelectedCategory;
    ShopItem SelectedItem;

    List<ShopItemCategory> ShopCategories;
    Dictionary<ShopItemCategory, ShopUI_Category> ShopCategoryToUIMap;
    Dictionary<ShopItem, ShopUI_Item> ShopItemToUIMap;

    void Start()
    {
        // BEGIN TESTING CODE
        CurrentPurchaser = PurchaserRef != null
            ? PurchaserRef
            : UnityEngine.Object.FindFirstObjectByType<Purchaser>();
        // END TESTING CODE

        RefreshShopUI();
    }

    void Update()
    {
        RefreshShopUI_Common();
    }

    void RefreshShopUI()
    {
        RefreshShopUI_Common();
        RefreshShopUI_Categories();
    }

    void RefreshShopUI_Common()
    {
        if (AvailableFunds != null)
        {
            if (CurrentPurchaser != null)
                AvailableFunds.text = $"{(CurrentPurchaser.GetCurrentFunds() / 100f):0.00}";
            else
                AvailableFunds.text = string.Empty;
        }

        if (PurchaseButton != null)
        {
            if (CurrentPurchaser != null && SelectedItem != null &&
                CurrentPurchaser.GetCurrentFunds() >= SelectedItem.Cost)
                PurchaseButton.interactable = true;
            else
                PurchaseButton.interactable = false;
        }

        if (ShopItemToUIMap != null)
        {
            foreach (var kvp in ShopItemToUIMap)
            {
                var item = kvp.Key;
                var itemUI = kvp.Value;
                if (CurrentPurchaser != null)
                    itemUI.SetCanAfford(item.Cost <= CurrentPurchaser.GetCurrentFunds());
                else
                    itemUI.SetCanAfford(false);
            }
        }
    }

    void RefreshShopUI_Categories()
    {
        if (CategoryUIRoot == null || CategoryUIPrefab == null) return;

        for (int i = CategoryUIRoot.childCount - 1; i >= 0; i--)
            Destroy(CategoryUIRoot.GetChild(i).gameObject);

        ShopCategories = new List<ShopItemCategory>();
        ShopCategoryToUIMap = new Dictionary<ShopItemCategory, ShopUI_Category>();

        foreach (var item in AvailableItems)
        {
            if (item == null || item.Category == null) continue;
            if (!ShopCategories.Contains(item.Category))
                ShopCategories.Add(item.Category);
        }

        ShopCategories.Sort((a, b) => a.Name.CompareTo(b.Name));

        foreach (var category in ShopCategories)
        {
            var categoryGO = Instantiate(CategoryUIPrefab, CategoryUIRoot);
            var categoryUI = categoryGO.GetComponent<ShopUI_Category>();
            categoryUI.Bind(category, OnCategorySelected);
            ShopCategoryToUIMap[category] = categoryUI;
        }

        if (ShopCategories.Contains(SelectedCategory))
            OnCategorySelected(SelectedCategory);
        else
            SelectedCategory = null;
    }

    void RefreshShopUI_Items()
    {
        if (ItemUIRoot == null || ItemUIPrefab == null) return;

        for (int i = ItemUIRoot.childCount - 1; i >= 0; i--)
            Destroy(ItemUIRoot.GetChild(i).gameObject);

        ShopItemToUIMap = new Dictionary<ShopItem, ShopUI_Item>();

        foreach (var item in AvailableItems)
        {
            if (item == null) continue;
            if (item.Category != SelectedCategory) continue;

            var itemGO = Instantiate(ItemUIPrefab, ItemUIRoot);
            var itemUI = itemGO.GetComponent<ShopUI_Item>();
            itemUI.Bind(item, OnItemSelected);
            ShopItemToUIMap[item] = itemUI;
        }

        if (SelectedItem != null && ShopItemToUIMap.ContainsKey(SelectedItem))
            OnItemSelected(SelectedItem);
        else
            SelectedItem = null;

        RefreshShopUI_Common();
    }

    void OnCategorySelected(ShopItemCategory newlySelectedCategory)
    {
        if (SelectedCategory != null && newlySelectedCategory != null &&
            SelectedCategory != newlySelectedCategory)
        {
            SelectedItem = null;
        }

        SelectedCategory = newlySelectedCategory;
        foreach (var category in ShopCategories)
            ShopCategoryToUIMap[category].SetIsSelected(category == SelectedCategory);

        RefreshShopUI_Items();
    }

    void OnItemSelected(ShopItem newlySelectedItem)
    {
        SelectedItem = newlySelectedItem;
        foreach (var kvp in ShopItemToUIMap)
        {
            var item = kvp.Key;
            var itemUI = kvp.Value;
            itemUI.SetIsSelected(item == SelectedItem);
        }

        RefreshShopUI_Common();
    }

    public void OnClickedPurchase()
    {
        if (CurrentPurchaser == null || SelectedItem == null) return;

        if (CurrentPurchaser.SpendFunds(SelectedItem.Cost))
            RefreshShopUI_Common();
    }

    public void OnClickedExit()
    {
        if (!string.IsNullOrEmpty(ExitSceneName))
            SceneManager.LoadScene(ExitSceneName, LoadSceneMode.Single);
    }
}
