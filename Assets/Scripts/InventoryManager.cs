using System;
using System.Collections.Generic;
using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }

    [Serializable] public class Entry { public ShopItem item; public int quantity; public Entry(ShopItem i,int q){item=i;quantity=q;} }

    [Header("Capacity")]
    [Tooltip("Max distinct stacks (0 = unlimited)")]
    [SerializeField] private int maxStacks = 24;

    [Header("Persistence")]
    [SerializeField] private bool dontDestroyOnLoad = true;

    private readonly Dictionary<ShopItem, Entry> map = new();
    private readonly List<Entry> ordered = new();

    public event Action OnInventoryChanged;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        if (dontDestroyOnLoad) DontDestroyOnLoad(gameObject);
    }

    public int AddItem(ShopItem item, int qty = 1)
    {
        if (!item || qty <= 0) return qty;

        if (map.TryGetValue(item, out var e))
        {
            e.quantity += qty;
            Notify();
            return 0;
        }

        if (maxStacks > 0 && map.Count >= maxStacks) return qty;

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
        if (e.quantity <= 0) { map.Remove(item); ordered.Remove(e); }
        if (removed > 0) Notify();
        return removed;
    }

    public IReadOnlyList<Entry> Stacks => ordered;
    public int UsedStacks => map.Count;
    public int MaxStacks  => maxStacks;

    public void Clear()
    {
        if (map.Count == 0) return;
        map.Clear(); ordered.Clear(); Notify();
    }

    private void Notify() => OnInventoryChanged?.Invoke();
}