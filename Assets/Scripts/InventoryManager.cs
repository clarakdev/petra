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
    // Drag ALL your ShopItem assets here in the inspector (same list you already keep in InventoryManagerData)
    [Header("Catalog / Trading")]
    [Tooltip("All ShopItem assets available in the game. Used for trade ID lookup.")]
    [SerializeField] private List<ShopItem> catalog = new();

    // runtime lookup built from catalog: itemId -> ShopItem
    private Dictionary<string, ShopItem> id2item;

    // sessionId -> (playerId -> list of ItemStack offered by that player)
    // this is the escrow; items are removed from live inventory while 'offered'
    private readonly Dictionary<string, Dictionary<string, List<ItemStack>>> escrow
        = new();

    // === NEW: struct we pass around in trade/offers/UI ===
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
    // EXISTING INVENTORY API (unchanged logic)
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
    // NEW: catalog helpers (for TradeManager <-> network)
    // ----------------------------------------------------

    private void BuildCatalogIndexOnce()
    {
        if (id2item != null) return; // already built
        id2item = new Dictionary<string, ShopItem>();
        foreach (var si in catalog)
        {
            if (si == null) continue;

            // We assume ShopItem has some stable unique string ID field.
            // If your ShopItem script calls it "Id" or "ID" etc, update here.
            // We'll try some common patterns via reflection fallback.

            string itemId = TryGetShopItemId(si);
            if (!string.IsNullOrEmpty(itemId) && !id2item.ContainsKey(itemId))
                id2item[itemId] = si;
        }
    }

    // Try to read si.Id, si.ID, etc.
    private string TryGetShopItemId(ShopItem si)
    {
        if (si == null) return null;
        var t = si.GetType();

        // public string Id;
        var f = t.GetField("Id");
        if (f != null && f.FieldType == typeof(string))
        {
            var v = f.GetValue(si) as string;
            if (!string.IsNullOrEmpty(v)) return v;
        }

        // public string ID;
        f = t.GetField("ID");
        if (f != null && f.FieldType == typeof(string))
        {
            var v = f.GetValue(si) as string;
            if (!string.IsNullOrEmpty(v)) return v;
        }

        // public string id;
        f = t.GetField("id");
        if (f != null && f.FieldType == typeof(string))
        {
            var v = f.GetValue(si) as string;
            if (!string.IsNullOrEmpty(v)) return v;
        }

        // public string Id {get;}
        var p = t.GetProperty("Id");
        if (p != null && p.PropertyType == typeof(string))
        {
            var v = p.GetValue(si) as string;
            if (!string.IsNullOrEmpty(v)) return v;
        }

        // fallback: use asset name as ID
        return si.name;
    }

    // Called by TradeManager when it needs to send item IDs over the wire.
    public string GetItemId(ShopItem item)
    {
        if (item == null) return null;
        // re-run BuildCatalogIndexOnce to be safe
        BuildCatalogIndexOnce();

        // First try reflection to get "Id"
        string reflected = TryGetShopItemId(item);
        if (!string.IsNullOrEmpty(reflected))
            return reflected;

        // fallback: asset name
        return item.name;
    }

    // Called by TradeManager / TradeUI on the receiving side to map an ID back to a ShopItem.
    public ShopItem ResolveItem(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        BuildCatalogIndexOnce();

        if (id2item.TryGetValue(id, out var it))
            return it;

        // fallback: try match by asset name
        foreach (var si in catalog)
            if (si && si.name == id)
                return si;

        return null;
    }

    // Quality-of-life name for UI
    public string GetDisplayName(ShopItem item)
    {
        if (!item) return "<?>";
        // try some common fields like "Name", "DisplayName"
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

        // fallback to asset name in Project
        return item.name;
    }

    // ----------------------------------------------------
    // NEW: escrow logic (used by TradeManager)
    // ----------------------------------------------------

    // Move (qty) of each offered item out of the live inventory and into escrow for this session+player
    public bool BeginEscrow(string sessionId, string playerId, IEnumerable<ItemStack> items)
    {
        if (string.IsNullOrEmpty(sessionId) || string.IsNullOrEmpty(playerId))
            return false;

        // 1. confirm player actually has enough of each item
        foreach (var s in items)
        {
            if (s == null || s.item == null || s.qty <= 0) return false;
            if (!map.TryGetValue(s.item, out var e) || e.quantity < s.qty) return false;
        }

        // 2. remove from live inventory
        foreach (var s in items)
            RemoveItem(s.item, s.qty);

        // 3. store in escrow
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

    // Return *all* escrowed items for this session back to their original owners (used when trade cancels)
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

    // Return escrowed items for a single player (used when that player changes their offer mid-trade)
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

    // Finalize: after trade completes successfully, we DON'T refund escrow (the other side already got items).
    public bool CommitEscrow(string sessionId)
    {
        if (!escrow.ContainsKey(sessionId)) return false;
        escrow.Remove(sessionId);
        return true;
    }

    // helper mainly for debugging/inspection
    public IReadOnlyList<ItemStack> GetEscrow(string sessionId, string playerId)
    {
        if (escrow.TryGetValue(sessionId, out var byPlayer) &&
            byPlayer.TryGetValue(playerId, out var list))
        {
            return list;
        }
        return Array.Empty<ItemStack>();
    }

    // convenience for TradeManager completion step
    // In your design, each client owns ONLY their own local inventory
    // so giving items "to player X" on this machine is just AddItem() here.
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
}
