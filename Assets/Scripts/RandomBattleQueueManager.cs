using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;
using System.Collections.Generic;
using UnityEngine;

public class RandomBattleQueueManager : MonoBehaviourPunCallbacks, IOnEventCallback
{
    private const string QueueKey = "RandomBattleQueue";
    private const byte StartBattleEventCode = 101;

    [Header("Scenes")]
    [SerializeField] private string battlefieldSceneName = "Battlefield"; // Ensure exact match with Build Settings

    void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    // Call this when the player talks to the NPC to join the queue
    public void JoinRandomBattleQueue()
    {
        if (!PhotonNetwork.InRoom)
        {
            Debug.LogWarning("[RandomBattleQueueManager] Not in a Photon room!");
            return;
        }

        if (string.IsNullOrEmpty(PhotonNetwork.LocalPlayer.UserId))
        {
            Debug.LogWarning("[RandomBattleQueueManager] LocalPlayer.UserId is null/empty. Ensure UserId is set and room PublishUserId is enabled.");
        }

        if (PhotonNetwork.IsMasterClient)
        {
            AddPlayerToQueue(PhotonNetwork.LocalPlayer.UserId);
        }
        else
        {
            photonView.RPC(nameof(RPC_RequestJoinQueue), RpcTarget.MasterClient, PhotonNetwork.LocalPlayer.UserId);
        }
    }

    [PunRPC]
    private void RPC_RequestJoinQueue(string userId)
    {
        if (PhotonNetwork.IsMasterClient)
        {
            AddPlayerToQueue(userId);
        }
    }

    private void AddPlayerToQueue(string userId)
    {
        // Work on in-memory copy, then write to room properties
        List<string> queue = GetQueue();
        if (!queue.Contains(userId))
        {
            queue.Add(userId);
            SetQueue(queue);
            Debug.Log($"[RandomBattleQueueManager] Added {userId} to random battle queue. Queue: {string.Join(",", queue)}");

            // Important: use the in-memory queue we just updated (don't read back immediately)
            TryStartBattle(queue);
        }
        else
        {
            Debug.Log($"[RandomBattleQueueManager] {userId} already in queue. Queue: {string.Join(",", queue)}");
        }
    }

    private void RemovePlayerFromQueue(string userId)
    {
        List<string> queue = GetQueue();
        if (queue.Remove(userId))
        {
            SetQueue(queue);
            Debug.Log($"[RandomBattleQueueManager] Removed {userId} from random battle queue. Queue: {string.Join(",", queue)}");
        }
    }

    // Accept an optional queue argument to avoid reading stale room properties
    private void TryStartBattle(List<string> maybeQueue = null)
    {
        if (!PhotonNetwork.IsMasterClient)
            return;

        List<string> queue = maybeQueue ?? GetQueue();
        Debug.Log($"[RandomBattleQueueManager] TryStartBattle() queueCount={queue.Count}");

        if (queue.Count >= 2)
        {
            string player1 = queue[0];
            string player2 = queue[1];

            // Remove matched players from the queue
            queue.RemoveAt(0);
            queue.RemoveAt(0);
            SetQueue(queue);

            // Resolve actor numbers for targeting
            int[] actorNumbers = GetActorNumbersForUsers(new[] { player1, player2 });
            Debug.Log($"[RandomBattleQueueManager] Matching {player1} and {player2} for battle! actorNumbers=[{string.Join(",", actorNumbers)}]");

            object[] content = new object[] { player1, player2 };

            RaiseEventOptions options;
            if (actorNumbers != null && actorNumbers.Length == 2)
            {
                options = new RaiseEventOptions { TargetActors = actorNumbers };
            }
            else
            {
                Debug.LogWarning("[RandomBattleQueueManager] Could not resolve both actor numbers. Falling back to ReceiverGroup.All.");
                options = new RaiseEventOptions { Receivers = ReceiverGroup.All };
            }

            PhotonNetwork.RaiseEvent(StartBattleEventCode, content, options, SendOptions.SendReliable);
        }
    }

    private int[] GetActorNumbersForUsers(string[] userIds)
    {
        List<int> actorNumbers = new List<int>();
        if (PhotonNetwork.CurrentRoom == null)
            return actorNumbers.ToArray();

        foreach (var p in PhotonNetwork.CurrentRoom.Players.Values)
        {
            if (!string.IsNullOrEmpty(p.UserId) && System.Array.Exists(userIds, id => id == p.UserId))
            {
                actorNumbers.Add(p.ActorNumber);
            }
        }

        if (actorNumbers.Count < 2)
        {
            Debug.LogWarning($"[RandomBattleQueueManager] GetActorNumbersForUsers resolved {actorNumbers.Count} actors. " +
                             $"HaveUserIds=[{string.Join(",", userIds)}], RoomPlayers={PhotonNetwork.CurrentRoom.PlayerCount}");
        }

        return actorNumbers.ToArray();
    }

    private List<string> GetQueue()
    {
        if (PhotonNetwork.CurrentRoom == null || PhotonNetwork.CurrentRoom.CustomProperties == null)
            return new List<string>();

        if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(QueueKey, out object queueObj) && queueObj is string queueStr)
        {
            if (!string.IsNullOrEmpty(queueStr))
                return new List<string>(queueStr.Split(','));
        }
        return new List<string>();
    }

    private void SetQueue(List<string> queue)
    {
        string queueStr = string.Join(",", queue);
        Hashtable props = new Hashtable { { QueueKey, queueStr } };
        PhotonNetwork.CurrentRoom.SetCustomProperties(props);
    }

    // Handle the custom event to load the battlefield scene
    public void OnEvent(EventData photonEvent)
    {
        if (photonEvent.Code == StartBattleEventCode)
        {
            object[] data = (object[])photonEvent.CustomData;
            string player1 = (string)data[0];
            string player2 = (string)data[1];

            Debug.Log($"[RandomBattleQueueManager] OnEvent StartBattleEventCode. player1={player1}, player2={player2}, localUserId={PhotonNetwork.LocalPlayer.UserId}");

            if (PhotonNetwork.LocalPlayer.UserId == player1 || PhotonNetwork.LocalPlayer.UserId == player2)
            {
                Debug.Log($"[RandomBattleQueueManager] Matched! Loading scene: {battlefieldSceneName}");
                PhotonNetwork.LoadLevel(battlefieldSceneName);
            }
            else
            {
                Debug.Log("[RandomBattleQueueManager] Not matched, ignoring event.");
            }
        }
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        if (PhotonNetwork.IsMasterClient)
        {
            RemovePlayerFromQueue(otherPlayer.UserId);
        }
    }

    private void OnEnable()
    {
        PhotonNetwork.AddCallbackTarget(this);
    }

    private void OnDisable()
    {
        PhotonNetwork.RemoveCallbackTarget(this);
    }
}