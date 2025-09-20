using System.Collections.Generic;
using UnityEngine;

public class InventoryUI : MonoBehaviour
{
    private List<InventorySlot> slots = new();

    void Awake()
    {
        slots.Clear();
        for (int i = 0; i < transform.childCount; i++)
        {
            var view = transform.GetChild(i).GetComponent<InventorySlot>();
            if (!view) view = transform.GetChild(i).gameObject.AddComponent<InventorySlot>();
            slots.Add(view);
        }
    }

    void OnEnable()
    {
        if (InventoryManager.Instance != null)
            InventoryManager.Instance.OnInventoryChanged += Rebuild;
        Rebuild();
    }

    void OnDisable()
    {
        if (InventoryManager.Instance != null)
            InventoryManager.Instance.OnInventoryChanged -= Rebuild;
    }

    public void Rebuild()
    {
        if (!InventoryManager.Instance) return;
        var stacks = InventoryManager.Instance.Stacks;

        int i = 0;
        for (; i < slots.Count && i < stacks.Count; i++)
            slots[i].ShowItem(stacks[i].item, stacks[i].quantity);

        for (; i < slots.Count; i++)
            slots[i].ShowEmpty();
    }
}