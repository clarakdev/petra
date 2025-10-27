using System;
using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public enum TradeState { Idle, Offering, Locked, Confirming, Completed, Cancelled }

[Serializable]
public class TradeOfferDTO
{
    public string playerId;

    // For UI (resolved locally):
    public List<InventoryManager.ItemStack> items = new();

    // Net payload (authoritative):
    public string[] itemIds;
    public int[] qtys;

    public bool locked;
    public bool confirmed;
}

[Serializable]
public class TradeSessionDTO
{
    public string sessionId;
    public string aId;
    public string bId;
    public TradeOfferDTO a = new();
    public TradeOfferDTO b = new();
    public TradeState state = TradeState.Idle;
}

[RequireComponent(typeof(PhotonView))]
public class TradeManager : MonoBehaviourPunCallbacks
{
    public static TradeManager Instance;

    [Header("Behavior")]
    [Tooltip("If ON, the first two players in the room are auto-paired into a trade session (no invite UI).")]
    [SerializeField] private bool autoStartWhenAnotherPlayerPresent = true;

    // sessionId -> session (master keeps truth; clients hold last snapshot only)
    private readonly Dictionary<string, TradeSessionDTO> sessions = new();

    // Master-only: (minId|maxId) -> sessionId to dedupe auto-creation
    private readonly Dictionary<string, string> pairKeyToSession = new();

    void Awake()
    {
        Instance = this;

        if (photonView == null)
            Debug.LogError("[Trade] Missing PhotonView! Add a PhotonView to TradeManager.");
        else
            Debug.Log($"[Trade] PhotonView OK. ViewID={photonView.ViewID} (0/neg means not assigned yet).");
    }

    void Start()
    {
        if (autoStartWhenAnotherPlayerPresent)
            StartCoroutine(AutoStartPoll());
    }

    // ===== Helpers =====

    private static string StableId(Player p)
    {
        return !string.IsNullOrEmpty(p.UserId) ? p.UserId : p.ActorNumber.ToString();
    }

    private Player FirstOtherPlayer()
    {
        foreach (var p in PhotonNetwork.PlayerList)
            if (!p.IsLocal) return p;
        return null;
    }

    private static string PairKey(string aId, string bId)
    {
        return string.CompareOrdinal(aId, bId) <= 0 ? $"{aId}|{bId}" : $"{bId}|{aId}";
    }

    // ===== Public kick-off (you can call this manually too) =====
    public void TryAutoStartTrade()
    {
        var other = FirstOtherPlayer();
        if (other == null)
        {
            Debug.Log("[Trade] TryAutoStartTrade: no other player yet.");
            return;
        }
        Debug.Log($"[Trade] TryAutoStartTrade: requesting session with Actor #{other.ActorNumber} (IsMaster={PhotonNetwork.IsMasterClient}).");
        InviteByActor(other.ActorNumber);
    }

    // ===== Auto-start (invite-less) =====

    private System.Collections.IEnumerator AutoStartPoll()
    {
        while (true)
        {
            if (PhotonNetwork.InRoom)
            {
                var other = FirstOtherPlayer();
                if (other != null)
                {
                    Debug.Log($"[Trade] AutoStartPoll: found other Actor #{other.ActorNumber}. Sending request to Master.");
                    InviteByActor(other.ActorNumber);
                    yield break;
                }
            }
            yield return new WaitForSeconds(0.5f);
        }
    }

    // ===== Public client API =====

    public void InviteByActor(int targetActorNumber)
    {
        Debug.Log($"[Trade] InviteByActor -> Master: me #{PhotonNetwork.LocalPlayer.ActorNumber} -> target #{targetActorNumber}");
        photonView.RPC(nameof(RPC_CreateOrGetSessionByActors), RpcTarget.MasterClient,
            PhotonNetwork.LocalPlayer.ActorNumber, targetActorNumber);
    }

    public void AddOffer(string sessionId, List<InventoryManager.ItemStack> items)
    {
        var ids  = new List<string>();
        var qtys = new List<int>();
        foreach (var it in items)
        {
            var id = InventoryManager.Instance.GetItemId(it.item);
            if (!string.IsNullOrEmpty(id) && it.qty > 0) { ids.Add(id); qtys.Add(it.qty); }
        }

        var myId = StableId(PhotonNetwork.LocalPlayer);

        // Local escrow
        InventoryManager.Instance.CancelEscrowForPlayer(sessionId, myId);
        InventoryManager.Instance.BeginEscrow(sessionId, myId, items);

        Debug.Log($"[Trade] AddOffer: sid={sessionId}, items={ids.Count}");
        photonView.RPC(nameof(RPC_AddOfferIds), RpcTarget.MasterClient, sessionId, myId, ids.ToArray(), qtys.ToArray());
    }

    public void SetLocked(string sessionId, bool locked)
    {
        Debug.Log($"[Trade] SetLocked: sid={sessionId}, locked={locked}");
        photonView.RPC(nameof(RPC_SetLocked), RpcTarget.MasterClient, sessionId, StableId(PhotonNetwork.LocalPlayer), locked);
    }

    public void SetConfirmed(string sessionId, bool confirmed)
    {
        Debug.Log($"[Trade] SetConfirmed: sid={sessionId}, confirmed={confirmed}");
        photonView.RPC(nameof(RPC_SetConfirmed), RpcTarget.MasterClient, sessionId, StableId(PhotonNetwork.LocalPlayer), confirmed);
    }

    public void Cancel(string sessionId)
    {
        Debug.Log($"[Trade] Cancel: sid={sessionId}");
        photonView.RPC(nameof(RPC_Cancel), RpcTarget.MasterClient, sessionId);
    }

    // ===== Master RPCs =====

    [PunRPC]
    void RPC_CreateOrGetSessionByActors(int aActor, int bActor, PhotonMessageInfo _info)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        var room = PhotonNetwork.CurrentRoom;
        if (room == null) return;

        var aP = room.GetPlayer(aActor);
        var bP = room.GetPlayer(bActor);
        if (aP == null || bP == null) return;

        string aId = StableId(aP);
        string bId = StableId(bP);
        string key = PairKey(aId, bId);

        Debug.Log($"[Trade][MASTER] Session request: A={aId} (#{aActor})  B={bId} (#{bActor})  key={key}");

        if (!pairKeyToSession.TryGetValue(key, out var sid) || string.IsNullOrEmpty(sid) || !sessions.ContainsKey(sid))
        {
            sid = Guid.NewGuid().ToString("N");
            var s = new TradeSessionDTO
            {
                sessionId = sid, aId = aId, bId = bId,
                state = TradeState.Offering,
                a = new TradeOfferDTO { playerId = aId },
                b = new TradeOfferDTO { playerId = bId }
            };
            sessions[sid] = s;
            pairKeyToSession[key] = sid;

            Debug.Log($"[Trade][MASTER] Created session {sid}");
            BroadcastSnapshot(s);
        }
        else
        {
            Debug.Log($"[Trade][MASTER] Reusing session {sid}");
            BroadcastSnapshot(sessions[sid]);
        }
    }

    [PunRPC]
    void RPC_AddOfferIds(string sid, string pid, string[] itemIds, int[] qtys, PhotonMessageInfo _info)
    {
        if (!PhotonNetwork.IsMasterClient || !sessions.TryGetValue(sid, out var s)) return;
        if (s.state != TradeState.Offering && s.state != TradeState.Locked) return;

        Debug.Log($"[Trade][MASTER] AddOffer: sid={sid}, from={pid}, count={itemIds?.Length ?? 0}");

        s.a.locked = s.a.confirmed = false;
        s.b.locked = s.b.confirmed = false;
        s.state = TradeState.Offering;

        var offer = (pid == s.aId) ? s.a : s.b;
        offer.itemIds = itemIds ?? Array.Empty<string>();
        offer.qtys    = qtys    ?? Array.Empty<int>();

        BroadcastSnapshot(s);
    }

    [PunRPC]
    void RPC_SetLocked(string sid, string pid, bool locked, PhotonMessageInfo _info)
    {
        if (!PhotonNetwork.IsMasterClient || !sessions.TryGetValue(sid, out var s)) return;
        var offer = (pid == s.aId) ? s.a : s.b;
        offer.locked = locked;

        s.state = (s.a.locked && s.b.locked) ? TradeState.Locked : TradeState.Offering;
        Debug.Log($"[Trade][MASTER] SetLocked: sid={sid} -> {s.state}");
        BroadcastSnapshot(s);
    }

    [PunRPC]
    void RPC_SetConfirmed(string sid, string pid, bool confirmed, PhotonMessageInfo _info)
    {
        if (!PhotonNetwork.IsMasterClient || !sessions.TryGetValue(sid, out var s)) return;
        if (!(s.a.locked && s.b.locked)) return;

        var offer = (pid == s.aId) ? s.a : s.b;
        offer.confirmed = confirmed;

        if (s.a.confirmed && s.b.confirmed) CompleteTrade(s);
        else { s.state = TradeState.Confirming; Debug.Log($"[Trade][MASTER] Confirming: sid={sid}"); BroadcastSnapshot(s); }
    }

    [PunRPC]
    void RPC_Cancel(string sid, PhotonMessageInfo _info)
    {
        if (!PhotonNetwork.IsMasterClient || !sessions.TryGetValue(sid, out var s)) return;

        photonView.RPC(nameof(RPC_CancelLocal), RpcTarget.All, sid);
        s.state = TradeState.Cancelled;
        Debug.Log($"[Trade][MASTER] Cancel: sid={sid}");
        BroadcastSnapshot(s);

        pairKeyToSession.Remove(PairKey(s.aId, s.bId));
        sessions.Remove(sid);
    }

    [PunRPC] void RPC_CancelLocal(string sid) => InventoryManager.Instance.CancelEscrow(sid);

    // ===== Complete trade =====

    private void CompleteTrade(TradeSessionDTO s)
    {
        var a = FindPlayerByStableId(s.aId);
        var b = FindPlayerByStableId(s.bId);

        Debug.Log($"[Trade][MASTER] Completing: sid={s.sessionId}");

        photonView.RPC(nameof(RPC_ApplyTradeLocal), a, s.sessionId, s.b.itemIds, s.b.qtys);
        photonView.RPC(nameof(RPC_ApplyTradeLocal), b, s.sessionId, s.a.itemIds, s.a.qtys);

        s.state = TradeState.Completed;
        BroadcastSnapshot(s);

        pairKeyToSession.Remove(PairKey(s.aId, s.bId));
        sessions.Remove(s.sessionId);
    }

    [PunRPC]
    void RPC_ApplyTradeLocal(string sid, string[] itemIds, int[] qtys)
    {
        if (itemIds != null && qtys != null)
            for (int i = 0; i < itemIds.Length && i < qtys.Length; i++)
                InventoryManager.Instance.AddItemById(itemIds[i], qtys[i]);

        InventoryManager.Instance.CommitEscrow(sid);
    }

    // ===== Snapshots =====

    private void BroadcastSnapshot(TradeSessionDTO s)
    {
        photonView.RPC(nameof(RPC_ReceiveSnapshot), RpcTarget.All, JsonUtility.ToJson(s));
    }

    public TradeSessionDTO LastSnapshot { get; private set; }


    [PunRPC]
    void RPC_ReceiveSnapshot(string json)
{
    var s = JsonUtility.FromJson<TradeSessionDTO>(json);

    // Resolve for UI (A)
    s.a.items = new List<InventoryManager.ItemStack>();
    if (s.a.itemIds != null && s.a.qtys != null)
        for (int i = 0; i < s.a.itemIds.Length && i < s.a.qtys.Length; i++)
        {
            var it = InventoryManager.Instance.ResolveItem(s.a.itemIds[i]);
            if (it != null) s.a.items.Add(new InventoryManager.ItemStack(it, s.a.qtys[i]));
        }

    // Resolve for UI (B)
    s.b.items = new List<InventoryManager.ItemStack>();
    if (s.b.itemIds != null && s.b.qtys != null)
        for (int i = 0; i < s.b.itemIds.Length && i < s.b.qtys.Length; i++)
        {
            var it = InventoryManager.Instance.ResolveItem(s.b.itemIds[i]);
            if (it != null) s.b.items.Add(new InventoryManager.ItemStack(it, s.b.qtys[i]));
        }

    Debug.Log($"[Trade] Snapshot received: sid={s.sessionId}, state={s.state}");

    // <-- NEW: cache latest snapshot so UI can pull it
    LastSnapshot = s;

    // Existing event
    OnTradeSnapshot?.Invoke(s);
}

    public event Action<TradeSessionDTO> OnTradeSnapshot;

    // ---- helpers ----
    private Player FindPlayerByStableId(string id)
    {
        foreach (var p in PhotonNetwork.PlayerList)
        {
            if (!string.IsNullOrEmpty(p.UserId) && p.UserId == id) return p;
            if (p.ActorNumber.ToString() == id) return p;
        }
        return null;
    }

    // Keep auto-start robust as roster changes
    public override void OnJoinedRoom()                      { if (autoStartWhenAnotherPlayerPresent) StartCoroutine(AutoStartPoll()); }
    public override void OnPlayerEnteredRoom(Player newP)    { if (autoStartWhenAnotherPlayerPresent) StartCoroutine(AutoStartPoll()); }
}
