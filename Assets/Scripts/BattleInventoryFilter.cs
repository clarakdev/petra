using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;

/// Filters inventory to show only potions in battle and handles potion usage
public class BattleInventoryFilter : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform inventoryContainer;
    [SerializeField] private Button confirmButton;

    private List<InventorySlot> slots = new List<InventorySlot>();
    private ShopItem selectedPotion;
    private int selectedSlotIndex = -1;
    private List<ShopItem> currentPotions = new List<ShopItem>();

    void Awake()
    {
        if (inventoryContainer == null)
        {
            inventoryContainer = transform.Find("Inventory");
            if (inventoryContainer == null)
            {
                Debug.LogError("[BattleInventoryFilter] Could not find 'Inventory' container!");
            }
        }

        if (confirmButton == null)
        {
            var confirmBtn = transform.Find("ConfirmButton");
            if (confirmBtn != null)
            {
                confirmButton = confirmBtn.GetComponent<Button>();
            }
        }

        if (inventoryContainer != null)
        {
            slots.Clear();
            for (int i = 0; i < inventoryContainer.childCount; i++)
            {
                var child = inventoryContainer.GetChild(i);
                var slot = child.GetComponent<InventorySlot>();
                if (slot != null)
                {
                    slots.Add(slot);
                    var btn = child.GetComponent<Button>();
                    if (btn == null)
                    {
                        btn = child.gameObject.AddComponent<Button>();
                    }
                }
            }
        }

        if (confirmButton == null)
        {
            Debug.LogWarning("[BattleInventoryFilter] Confirm button not found!");
        }
    }

    void OnEnable()
    {
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnInventoryChanged += RebuildPotionView;
        }

        if (confirmButton != null)
        {
            confirmButton.onClick.RemoveAllListeners();
            confirmButton.interactable = false;
        }

        RebuildPotionView();
    }

    void OnDisable()
    {
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnInventoryChanged -= RebuildPotionView;
        }
        DeselectPotion();
    }

    private void RebuildPotionView()
    {
        if (InventoryManager.Instance == null)
        {
            Debug.LogWarning("[BattleInventoryFilter] InventoryManager.Instance is null!");
            return;
        }

        var stacks = InventoryManager.Instance.Stacks;
        var potionEntries = new List<(ShopItem item, int quantity)>();

        foreach (var entry in stacks)
        {
            if (entry.item != null && IsPotionItem(entry.item) && entry.quantity > 0)
            {
                potionEntries.Add((entry.item, entry.quantity));
            }
        }

        int slotIndex = 0;
        for (int i = 0; i < potionEntries.Count && slotIndex < slots.Count; i++)
        {
            var entry = potionEntries[i];
            slots[slotIndex].ShowItem(entry.item, entry.quantity);

            int capturedIndex = slotIndex;
            ShopItem capturedItem = entry.item;
            var btn = slots[slotIndex].GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() => OnSlotClicked(capturedIndex, capturedItem));
                btn.interactable = true;
            }
            slotIndex++;
        }

        for (; slotIndex < slots.Count; slotIndex++)
        {
            slots[slotIndex].ShowEmpty();
            var btn = slots[slotIndex].GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
                btn.interactable = false;
            }
        }

        if (selectedPotion != null && !HasPotion(selectedPotion))
        {
            DeselectPotion();
        }
    }

    private int GetItemQuantity(ShopItem item)
    {
        if (InventoryManager.Instance == null || item == null) return 0;

        foreach (var entry in InventoryManager.Instance.Stacks)
        {
            if (entry.item == item)
            {
                return entry.quantity;
            }
        }
        return 0;
    }

    private bool IsPotionItem(ShopItem item)
    {
        if (item == null || item.Category == null) return false;

        bool isPotion = false;

        if (item.Category.Name != null && item.Category.Name.ToLower().Contains("potion")) isPotion = true;
        if (item.Category.name.ToLower().Contains("potion")) isPotion = true;
        if (item.Name != null && item.Name.ToLower().Contains("potion")) isPotion = true;

        return isPotion;
    }

    private bool HasPotion(ShopItem potion)
    {
        return GetItemQuantity(potion) > 0;
    }

    private void OnSlotClicked(int slotIndex, ShopItem potion)
    {
        selectedSlotIndex = slotIndex;
        selectedPotion = potion;

        for (int i = 0; i < slots.Count; i++)
        {
            var slotObj = slots[i].gameObject;
            var img = slotObj.GetComponent<Image>();
            if (img != null)
            {
                img.color = (i == slotIndex) ? new Color(1f, 1f, 0.5f, 1f) : Color.white;
            }
            else
            {
                var childImg = slotObj.GetComponentInChildren<Image>();
                if (childImg != null)
                {
                    childImg.color = (i == slotIndex) ? new Color(1f, 1f, 0.5f, 1f) : Color.white;
                }
            }
        }

        if (confirmButton != null)
        {
            confirmButton.interactable = true;
            confirmButton.onClick.RemoveAllListeners();
            confirmButton.onClick.AddListener(OnConfirmClicked);
        }
    }

    private void DeselectPotion()
    {
        selectedPotion = null;
        selectedSlotIndex = -1;

        foreach (var slot in slots)
        {
            var img = slot.GetComponent<Image>();
            if (img != null)
            {
                img.color = Color.white;
            }
            else
            {
                var childImg = slot.GetComponentInChildren<Image>();
                if (childImg != null)
                {
                    childImg.color = Color.white;
                }
            }
        }

        if (confirmButton != null)
        {
            confirmButton.interactable = false;
        }
    }

    private void OnConfirmClicked()
    {
        if (selectedPotion == null)
        {
            Debug.LogWarning("[BattleInventoryFilter] No potion selected!");
            return;
        }

        if (InventoryManager.Instance == null)
        {
            Debug.LogError("[BattleInventoryFilter] InventoryManager not found!");
            return;
        }

        if (!HasPotion(selectedPotion))
        {
            Debug.LogWarning("[BattleInventoryFilter] Player no longer has potion!");
            DeselectPotion();
            RebuildPotionView();
            return;
        }

        UsePotion(selectedPotion);
        InventoryManager.Instance.RemoveItem(selectedPotion, 1);
        DeselectPotion();
        EndTurnAfterPotionUse();
        gameObject.SetActive(false);
    }

    private void UsePotion(ShopItem potion)
    {
        if (potion == null) return;

        int healAmount = ParseHealAmount(potion.Description);
        if (healAmount > 0)
        {
            HealPlayer(healAmount);
        }
        else
        {
            Debug.LogWarning($"[BattleInventoryFilter] Could not parse heal amount from: '{potion.Description}'");
        }
    }

    private int ParseHealAmount(string description)
    {
        if (string.IsNullOrEmpty(description)) return 0;

        string[] words = description.Split(new char[] { ' ', ',', '.', '!', '?' }, System.StringSplitOptions.RemoveEmptyEntries);

        for (int i = 0; i < words.Length - 1; i++)
        {
            string word = words[i].ToLower();
            if (word == "heal" || word == "heals" || word == "restore" || word == "restores")
            {
                if (int.TryParse(words[i + 1], out int amount))
                {
                    return amount;
                }
            }
        }

        foreach (string word in words)
        {
            if (int.TryParse(word, out int amount))
            {
                return amount;
            }
        }

        return 0;
    }

    private void HealPlayer(int amount)
    {
        var battleManager = FindObjectOfType<BattleManager>();
        if (battleManager == null)
        {
            Debug.LogError("[BattleInventoryFilter] BattleManager not found!");
            return;
        }

        PetBattle myPet = battleManager.iAmPlayerSide ? battleManager.playerPet : battleManager.enemyPet;

        if (myPet != null && myPet.photonView != null)
        {
            int petViewID = myPet.photonView.ViewID;
            battleManager.photonView.RPC("RPC_ApplyHealing", RpcTarget.All, petViewID, amount);
        }
        else
        {
            Debug.LogError("[BattleInventoryFilter] Could not find player's pet or pet PhotonView!");
        }
    }

    private void EndTurnAfterPotionUse()
    {
        var battleManager = FindObjectOfType<BattleManager>();
        if (battleManager == null)
        {
            Debug.LogError("[BattleInventoryFilter] BattleManager not found!");
            return;
        }

        if (battleManager.photonView == null)
        {
            Debug.LogError("[BattleInventoryFilter] BattleManager.photonView is NULL!");
            return;
        }

        try
        {
            battleManager.photonView.RPC("RPC_RequestToggleTurn", RpcTarget.MasterClient);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[BattleInventoryFilter] Failed to send RPC: {e.Message}");
        }
    }
}
