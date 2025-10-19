using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;

/// <summary>
/// Filters inventory to show only potions in battle and handles potion usage
/// Attach this to the InventoryPanel GameObject in your battle scene
/// </summary>
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
            Debug.Log($"[BattleInventoryFilter] Found {slots.Count} inventory slots");
        }

        if (confirmButton != null)
        {
            Debug.Log("[BattleInventoryFilter] Confirm button found");
        }
        else
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
            Debug.Log("[BattleInventoryFilter] Confirm button setup complete");
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
        currentPotions.Clear();

        foreach (var entry in stacks)
        {
            if (entry.item != null && IsPotionItem(entry.item))
            {
                currentPotions.Add(entry.item);
            }
        }

        Debug.Log($"[BattleInventoryFilter] Found {currentPotions.Count} potions");

        int slotIndex = 0;
        for (int i = 0; i < currentPotions.Count && slotIndex < slots.Count; i++)
        {
            var item = currentPotions[i];
            int quantity = GetItemQuantity(item);

            if (quantity > 0)
            {
                slots[slotIndex].ShowItem(item, quantity);

                int capturedIndex = slotIndex;
                ShopItem capturedItem = item;
                var btn = slots[slotIndex].GetComponent<Button>();
                if (btn != null)
                {
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(() => OnSlotClicked(capturedIndex, capturedItem));
                    btn.interactable = true;
                }

                slotIndex++;
            }
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

        if (item.Category.Name != null && item.Category.Name.ToLower().Contains("potion"))
        {
            return true;
        }

        if (item.Category.name.ToLower().Contains("potion"))
        {
            return true;
        }

        if (item.Name != null && item.Name.ToLower().Contains("potion"))
        {
            return true;
        }

        return false;
    }

    private bool HasPotion(ShopItem potion)
    {
        return GetItemQuantity(potion) > 0;
    }

    private void OnSlotClicked(int slotIndex, ShopItem potion)
    {
        selectedSlotIndex = slotIndex;
        selectedPotion = potion;

        Debug.Log($"[BattleInventoryFilter] Selected potion: {potion.Name} from slot {slotIndex}");

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
            Debug.Log("[BattleInventoryFilter] Confirm button enabled and listener added");
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
        Debug.Log("[BattleInventoryFilter] === CONFIRM BUTTON CLICKED ===");

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

        string potionName = selectedPotion.Name;
        ShopItem potionToUse = selectedPotion;

        Debug.Log($"[BattleInventoryFilter] Using {potionName}");

        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnInventoryChanged -= RebuildPotionView;
        }

        UsePotion(potionToUse);

        int removed = InventoryManager.Instance.RemoveItem(potionToUse, 1);
        Debug.Log($"[BattleInventoryFilter] Removed {removed} {potionName} from inventory");

        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnInventoryChanged += RebuildPotionView;
        }

        DeselectPotion();

        Debug.Log("[BattleInventoryFilter] Ending turn NOW...");
        EndTurnAfterPotionUse();

        Debug.Log("[BattleInventoryFilter] Closing inventory panel now");
        gameObject.SetActive(false);

        Debug.Log("[BattleInventoryFilter] === CONFIRM BUTTON COMPLETE ===");
    }

    private void UsePotion(ShopItem potion)
    {
        if (potion == null) return;

        Debug.Log($"[BattleInventoryFilter] === USING POTION: {potion.Name} ===");
        Debug.Log($"[BattleInventoryFilter] Description: '{potion.Description}'");

        int healAmount = ParseHealAmount(potion.Description);

        if (healAmount > 0)
        {
            Debug.Log($"[BattleInventoryFilter] Parsed heal amount: {healAmount}");
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

            Debug.Log($"[BattleInventoryFilter] Requesting networked heal for pet ViewID: {petViewID}");

            battleManager.photonView.RPC("RPC_ApplyHealing", RpcTarget.All, petViewID, amount);

            Debug.Log($"[BattleInventoryFilter] Heal RPC sent for {amount} HP");
        }
        else
        {
            Debug.LogError("[BattleInventoryFilter] Could not find player's pet or pet PhotonView!");
        }
    }

    private void EndTurnAfterPotionUse()
    {
        Debug.Log("[BattleInventoryFilter] === STARTING EndTurnAfterPotionUse ===");

        var battleManager = FindObjectOfType<BattleManager>();
        if (battleManager == null)
        {
            Debug.LogError("[BattleInventoryFilter] BattleManager not found!");
            return;
        }

        Debug.Log($"[BattleInventoryFilter] BattleManager found: {battleManager.name}");

        if (battleManager.photonView == null)
        {
            Debug.LogError("[BattleInventoryFilter] BattleManager.photonView is NULL!");
            return;
        }

        Debug.Log("[BattleInventoryFilter] === ENDING TURN AFTER POTION USE ===");
        Debug.Log($"[BattleInventoryFilter] PhotonView.ViewID: {battleManager.photonView.ViewID}");
        Debug.Log($"[BattleInventoryFilter] PhotonView.IsMine: {battleManager.photonView.IsMine}");
        Debug.Log($"[BattleInventoryFilter] IsMasterClient: {PhotonNetwork.IsMasterClient}");
        Debug.Log($"[BattleInventoryFilter] PhotonNetwork.IsConnected: {PhotonNetwork.IsConnected}");

        try
        {
            Debug.Log("[BattleInventoryFilter] Calling RPC_RequestToggleTurn...");
            battleManager.photonView.RPC("RPC_RequestToggleTurn", RpcTarget.MasterClient);
            Debug.Log("[BattleInventoryFilter] ??? RPC_RequestToggleTurn sent successfully ???");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[BattleInventoryFilter] ??? Failed to send RPC: {e.Message}\n{e.StackTrace}");
        }
    }
}