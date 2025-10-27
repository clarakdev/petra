using System;
using System.Collections.Generic;
using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    // === EXISTING FIELDS / SINGLETON ===
    public static InventoryManager Instance { get; private set; }

    [Serializable]
    public class Entry
    {
        public ShopItem item;
        public int quantity;
        public Entry(ShopItem i, int q) { item = i; quantity = q; }
    }

    [Header("Capacity")]
    [Tooltip("Max distinct stacks (0 = unlimited)")]
    [SerializeField] private int maxStacks = 24;

    [Header("Persistence")]
    [SerializeField] private bool dontDestroyOnLoad = true;

    // your live inventory
    private readonly Dictionary<ShopItem, Entry> map = new();
    private readonly List<Entry> ordered = new();

    public event Action OnInventoryChanged;

    // === NEW: catalog so TradeManager can convert between item IDs and objects ===
    [Header("Catalog / Trading")]
    [Tooltip("All ShopItem assets available in the game. Used for trade ID lookup.")]
    [SerializeField] private List<ShopItem> catalog = new();

    // runtime lookup built from catalog: itemId -> ShopItem
    private Dictionary<string, ShopItem> id2item;

    // sessionId -> (playerId -> list of ItemStack offered by that player)
    private readonly Dictionary<string, Dictionary<string, List<ItemStack>>> escrow
        = new();

    [Serializable]
    public class ItemStack
    {
        public ShopItem item;
        public int qty;
        public ItemStack(ShopItem i, int q) { item = i; qty = q; }
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        if (dontDestroyOnLoad) DontDestroyOnLoad(gameObject);

        BuildCatalogIndexOnce();
    }

    // ----------------------------------------------------
    // INVENTORY CORE
    // ----------------------------------------------------

    public int AddItem(ShopItem item, int qty = 1)
    {
        if (!item || qty <= 0) return qty;

        if (map.TryGetValue(item, out var e))
        {
            e.quantity += qty;
            Notify();
            return 0;
        }

        if (maxStacks > 0 && map.Count >= maxStacks)
            return qty; // couldn't fit new stack

        var entry = new Entry(item, qty);
        map[item] = entry;
        ordered.Add(entry);
        Notify();
        return 0;
    }

    public int RemoveItem(ShopItem item, int qty = 1)
    {
        if (!item || qty <= 0 || !map.TryGetValue(item, out var e)) return 0;
        int removed = Mathf.Min(qty, e.quantity);
        e.quantity -= removed;
        if (e.quantity <= 0)
        {
            map.Remove(item);
            ordered.Remove(e);
        }
        if (removed > 0) Notify();
        return removed;
    }

    public IReadOnlyList<Entry> Stacks => ordered;
    public int UsedStacks => map.Count;
    public int MaxStacks => maxStacks;

    public void Clear()
    {
        if (map.Count == 0) return;
        map.Clear();
        ordered.Clear();
        Notify();
    }

    private void Notify() => OnInventoryChanged?.Invoke();

    // ----------------------------------------------------
    // CATALOG / TRADE HELPERS
    // ----------------------------------------------------

    private void BuildCatalogIndexOnce()
    {
        if (id2item != null) return; // already built
        id2item = new Dictionary<string, ShopItem>();
        foreach (var si in catalog)
        {
            if (si == null) continue;
            string itemId = TryGetShopItemId(si);
            if (!string.IsNullOrEmpty(itemId) && !id2item.ContainsKey(itemId))
                id2item[itemId] = si;
        }
    }

    private string TryGetShopItemId(ShopItem si)
    {
        if (si == null) return null;
        var t = si.GetType();

        var f = t.GetField("Id");
        if (f != null && f.FieldType == typeof(string))
        {
            var v = f.GetValue(si) as string;
            if (!string.IsNullOrEmpty(v)) return v;
        }

        f = t.GetField("ID");
        if (f != null && f.FieldType == typeof(string))
        {
            var v = f.GetValue(si) as string;
            if (!string.IsNullOrEmpty(v)) return v;
        }

        f = t.GetField("id");
        if (f != null && f.FieldType == typeof(string))
        {
            var v = f.GetValue(si) as string;
            if (!string.IsNullOrEmpty(v)) return v;
        }

        var p = t.GetProperty("Id");
        if (p != null && p.PropertyType == typeof(string))
        {
            var v = p.GetValue(si) as string;
            if (!string.IsNullOrEmpty(v)) return v;
        }

        // fallback: use asset name as ID
        return si.name;
    }

    public string GetItemId(ShopItem item)
    {
        if (item == null) return null;
        BuildCatalogIndexOnce();
        string reflected = TryGetShopItemId(item);
        if (!string.IsNullOrEmpty(reflected))
            return reflected;
        return item.name;
    }

    public ShopItem ResolveItem(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        BuildCatalogIndexOnce();

        if (id2item.TryGetValue(id, out var it))
            return it;

        foreach (var si in catalog)
            if (si && si.name == id)
                return si;

        return null;
    }

    public string GetDisplayName(ShopItem item)
    {
        if (!item) return "<?>";

        var t = item.GetType();
        var f = t.GetField("Name");
        if (f != null && f.FieldType == typeof(string))
        {
            var v = f.GetValue(item) as string;
            if (!string.IsNullOrEmpty(v)) return v;
        }

        f = t.GetField("DisplayName");
        if (f != null && f.FieldType == typeof(string))
        {
            var v = f.GetValue(item) as string;
            if (!string.IsNullOrEmpty(v)) return v;
        }

        var p = t.GetProperty("Name");
        if (p != null && p.PropertyType == typeof(string))
        {
            var v = p.GetValue(item) as string;
            if (!string.IsNullOrEmpty(v)) return v;
        }

        p = t.GetProperty("DisplayName");
        if (p != null && p.PropertyType == typeof(string))
        {
            var v = p.GetValue(item) as string;
            if (!string.IsNullOrEmpty(v)) return v;
        }

        return item.name;
    }

    // ----------------------------------------------------
    // ESCROW / TRADE LOGIC (unchanged)
    // ----------------------------------------------------

    public bool BeginEscrow(string sessionId, string playerId, IEnumerable<ItemStack> items)
    {
        if (string.IsNullOrEmpty(sessionId) || string.IsNullOrEmpty(playerId))
            return false;

        foreach (var s in items)
        {
            if (s == null || s.item == null || s.qty <= 0) return false;
            if (!map.TryGetValue(s.item, out var e) || e.quantity < s.qty) return false;
        }

        foreach (var s in items)
            RemoveItem(s.item, s.qty);

        if (!escrow.TryGetValue(sessionId, out var byPlayer))
        {
            byPlayer = new Dictionary<string, List<ItemStack>>();
            escrow[sessionId] = byPlayer;
        }

        if (!byPlayer.TryGetValue(playerId, out var list))
        {
            list = new List<ItemStack>();
            byPlayer[playerId] = list;
        }

        foreach (var s in items)
        {
            var found = list.Find(x => x.item == s.item);
            if (found != null) found.qty += s.qty;
            else list.Add(new ItemStack(s.item, s.qty));
        }

        return true;
    }

    public bool CancelEscrow(string sessionId)
    {
        if (!escrow.TryGetValue(sessionId, out var byPlayer)) return false;

        foreach (var kv in byPlayer)
        {
            foreach (var s in kv.Value)
                AddItem(s.item, s.qty);
        }

        escrow.Remove(sessionId);
        return true;
    }

    public bool CancelEscrowForPlayer(string sessionId, string playerId)
    {
        if (!escrow.TryGetValue(sessionId, out var byPlayer)) return false;
        if (!byPlayer.TryGetValue(playerId, out var list)) return false;

        foreach (var s in list)
            AddItem(s.item, s.qty);

        byPlayer.Remove(playerId);
        if (byPlayer.Count == 0)
            escrow.Remove(sessionId);

        return true;
    }

    public bool CommitEscrow(string sessionId)
    {
        if (!escrow.ContainsKey(sessionId)) return false;
        escrow.Remove(sessionId);
        return true;
    }

    public IReadOnlyList<ItemStack> GetEscrow(string sessionId, string playerId)
    {
        if (escrow.TryGetValue(sessionId, out var byPlayer) &&
            byPlayer.TryGetValue(playerId, out var list))
        {
            return list;
        }
        return Array.Empty<ItemStack>();
    }

    public void AddItemToPlayer(string playerId, ShopItem item, int qty)
    {
        AddItem(item, qty);
    }

    public void AddItemById(string id, int qty)
    {
        if (string.IsNullOrEmpty(id) || qty <= 0) return;

        var item = ResolveItem(id);
        if (item != null)
        {
            AddItem(item, qty);
        }
        else
        {
            Debug.LogWarning($"[InventoryManager] AddItemById failed. Couldn't resolve item id '{id}'");
        }
    }

    // ----------------------------------------------------
    // SAVE / LOAD SUPPORT
    // ----------------------------------------------------

    public List<InventoryItemSave> CaptureSave()
    {
        var data = new List<InventoryItemSave>();
        BuildCatalogIndexOnce();

        foreach (var e in ordered)
        {
            if (e.item == null) continue;
            string id = GetItemId(e.item);
            if (string.IsNullOrEmpty(id)) continue;

            data.Add(new InventoryItemSave
            {
                itemId = id,
                quantity = e.quantity
            });
        }

        Debug.Log($"[InventoryManager] Captured {data.Count} items for save.");
        return data;
    }

    public void RestoreFromSave(List<InventoryItemSave> saved)
    {
        Clear();
        if (saved == null) return;

        foreach (var entry in saved)
        {
            var item = ResolveItem(entry.itemId);
            if (item != null)
                AddItem(item, entry.quantity);
        }

        Debug.Log($"[InventoryManager] Restored {map.Count} items from save.");
    }
}
