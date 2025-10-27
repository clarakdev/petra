using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;

public class TradeUI : MonoBehaviour
{
    [Header("UI References (can be left empty; will auto-bind by name)")]
    public TextMeshProUGUI statusText;      // finds child "StatusText" if null
    public Transform myOfferList;           // finds "MyOfferList" if null
    public Transform theirOfferList;        // finds "TheirOfferList" if null
    public Button addBtn, lockBtn, confirmBtn, cancelBtn; // finds buttons if null

    [Header("Visibility")]
    [Tooltip("Leave OFF while debugging so the panel stays visible.")]
    public bool autoShowHide = false;

    private TradeSessionDTO current;

    // ---------- Helpers ----------
    private static string StableId(Player p)
        => !string.IsNullOrEmpty(p.UserId) ? p.UserId : p.ActorNumber.ToString();

    private string MyId => StableId(PhotonNetwork.LocalPlayer);

    private static string NickForId(string id)
    {
        foreach (var p in PhotonNetwork.PlayerList)
        {
            bool match = (!string.IsNullOrEmpty(p.UserId) && p.UserId == id) || p.ActorNumber.ToString() == id;
            if (match) return string.IsNullOrEmpty(p.NickName) ? $"Player {p.ActorNumber}" : p.NickName;
        }
        return "Partner";
    }

    private static string PrettyItemName(ShopItem it)
    {
        if (it == null) return "<?>";
        // try common display fields, then fall back to Unity asset name
        // (If you have a specific field like it.DisplayName or it.itemName, feel free to swap it in)
        var t = it.GetType();
        try
        {
            var f = t.GetField("Name"); if (f != null) { var v = f.GetValue(it) as string; if (!string.IsNullOrEmpty(v)) return v; }
            f = t.GetField("DisplayName"); if (f != null) { var v = f.GetValue(it) as string; if (!string.IsNullOrEmpty(v)) return v; }
            var p = t.GetProperty("Name"); if (p != null) { var v = p.GetValue(it) as string; if (!string.IsNullOrEmpty(v)) return v; }
            p = t.GetProperty("DisplayName"); if (p != null) { var v = p.GetValue(it) as string; if (!string.IsNullOrEmpty(v)) return v; }
        }
        catch { /* ignore reflection errors */ }
        return it.name; // Unity asset name as safe fallback
    }

    void OnEnable()
    {
        AutoBindIfMissing();
        if (TradeManager.Instance != null)
            TradeManager.Instance.OnTradeSnapshot += OnSnapshot;
    }

    void OnDisable()
    {
        if (TradeManager.Instance != null)
            TradeManager.Instance.OnTradeSnapshot -= OnSnapshot;
    }

    // Fallback pull so UI updates even if event hookup is missed
    void Update()
    {
        var tm = TradeManager.Instance;
        if (tm != null && tm.LastSnapshot != null)
        {
            if (current == null ||
                current.sessionId != tm.LastSnapshot.sessionId ||
                current.state     != tm.LastSnapshot.state)
            {
                OnSnapshot(tm.LastSnapshot);
            }
        }
    }

    void AutoBindIfMissing()
    {
        if (!statusText)
        {
            var t = transform.Find("StatusText");
            if (t) statusText = t.GetComponent<TextMeshProUGUI>();
        }
        if (!myOfferList)
        {
            var t = transform.Find("MyOfferList");
            if (t) myOfferList = t;
        }
        if (!theirOfferList)
        {
            var t = transform.Find("TheirOfferList");
            if (t) theirOfferList = t;
        }

        if (!addBtn)
        {
            var t = transform.Find("Add Button");
            if (t) addBtn = t.GetComponent<Button>();
        }
        if (!lockBtn)
        {
            var t = transform.Find("Lock Button");
            if (t) lockBtn = t.GetComponent<Button>();
        }
        if (!confirmBtn)
        {
            var t = transform.Find("Confirm Button");
            if (t) confirmBtn = t.GetComponent<Button>();
        }
        if (!cancelBtn)
        {
            var t = transform.Find("Cancel Button");
            if (t) cancelBtn = t.GetComponent<Button>();
        }
    }

    void OnSnapshot(TradeSessionDTO s)
    {
        current = s;

        bool iAmA = s.aId == MyId;
        bool iAmB = s.bId == MyId;

        if (autoShowHide)
        {
            bool shouldShow = (iAmA || iAmB) && s.state != TradeState.Cancelled;
            if (gameObject.activeSelf != shouldShow) gameObject.SetActive(shouldShow);
            if (!shouldShow) return;
        }

        var mine   = iAmA ? s.a : s.b;
        var theirs = iAmA ? s.b : s.a;

        // Friendlier status line
        string partner = NickForId(iAmA ? s.bId : s.aId);
        if (statusText) statusText.text = $"{s.state}\nTrading with: {partner}";

        RenderList(myOfferList,  mine.items ?? new List<InventoryManager.ItemStack>());
        RenderList(theirOfferList, theirs.items ?? new List<InventoryManager.ItemStack>());

        if (lockBtn)    lockBtn.interactable    = s.state == TradeState.Offering;
        if (confirmBtn) confirmBtn.interactable = mine.locked && theirs.locked && !mine.confirmed;
    }

    // Auto-create rows so you don’t need to place TMP children manually
    void EnsureRows(Transform root, int rows)
    {
        if (root == null) return;
        while (root.childCount < rows)
        {
            var go = new GameObject("Row", typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(root, false);
            var t = go.GetComponent<TextMeshProUGUI>();
            t.text = "";
            t.fontSize = 18;
            t.alignment = TextAlignmentOptions.Left;
        }
    }

    void RenderList(Transform root, List<InventoryManager.ItemStack> items)
    {
        const int kRows = 6;
        EnsureRows(root, kRows);
        for (int i = 0; i < root.childCount; i++)
        {
            var t = root.GetChild(i).GetComponent<TextMeshProUGUI>();
            if (i < items.Count && items[i].item != null)
            {
                string name = PrettyItemName(items[i].item);
                t.text = $"{name} x{items[i].qty}";
            }
            else t.text = "";
        }
    }

    // ===== Buttons =====

    // TEMP: adds 1x of the first stack from your inventory so you can test end-to-end
    public void OnAddSample()
    {
        if (current == null) { if (statusText) statusText.text = "No trade session."; return; }

        var stacks = InventoryManager.Instance.Stacks;
        if (stacks == null || stacks.Count == 0 || stacks[0].item == null)
        {
            if (statusText) statusText.text = "You have no items to offer.";
            return;
        }

        var offer = new List<InventoryManager.ItemStack> {
            new InventoryManager.ItemStack(stacks[0].item, 1)
        };

        TradeManager.Instance.AddOffer(current.sessionId, offer);
    }

    public void OnLock()
    {
        if (current != null) TradeManager.Instance.SetLocked(current.sessionId, true);
    }

    public void OnConfirm()
    {
        if (current != null) TradeManager.Instance.SetConfirmed(current.sessionId, true);
    }

    public void OnCancel()
    {
        if (current != null) TradeManager.Instance.Cancel(current.sessionId);
    }
}
