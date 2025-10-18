using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;
using UnityEngine;
using System.Collections;
using TMPro;

// Make sure Hashtable is recognized
using Hashtable = ExitGames.Client.Photon.Hashtable;

public class BattleManager : MonoBehaviourPunCallbacks
{
    // Assign in inspector OR resolve at runtime after spawn
    public PetBattle playerPet;
    public PetBattle enemyPet;

    // Optional: UI references to disable/enable during turns
    public GameObject commandPanel;
    public TextMeshProUGUI turnIndicatorText; // Changed to TextMeshProUGUI

    // Room property keys
    private const string PROP_TURN = "Turn"; // "PLAYER" or "ENEMY"
    private const string PROP_P1 = "P1";     // playerPet ViewID
    private const string PROP_P2 = "P2";     // enemyPet ViewID

    // Who am I controlling? (set when pets are spawned/assigned)
    public bool iAmPlayerSide = true;

    private bool isProcessingAttack = false; // Prevent double attacks

    // Determine which side I'm on based on who is Master
    private void DeterminePlayerSide()
    {
        // Master client is always "PLAYER" side (goes first)
        // Non-master is always "ENEMY" side
        iAmPlayerSide = PhotonNetwork.IsMasterClient;
        Debug.Log($"[BattleManager] I am {(iAmPlayerSide ? "PLAYER" : "ENEMY")} side (Master: {PhotonNetwork.IsMasterClient})");
    }

    // --- Lifecycle ----------------------------------------------------------
    private void Start()
    {
        StartCoroutine(WaitForPetsAndInit());
    }

    private IEnumerator WaitForPetsAndInit()
    {
        // First, determine which side I'm on
        DeterminePlayerSide();

        // Wait until both pets are assigned
        float timeout = 10f;
        float elapsed = 0f;

        while ((playerPet == null || enemyPet == null) && elapsed < timeout)
        {
            yield return new WaitForSeconds(0.1f);
            elapsed += 0.1f;
        }

        if (playerPet == null || enemyPet == null)
        {
            Debug.LogError("[BattleManager] Timeout: Pets not assigned!");
            yield break;
        }

        Debug.Log($"[BattleManager] Both pets assigned. PlayerPet Owner: {playerPet.photonView.Owner.NickName}, EnemyPet Owner: {enemyPet.photonView.Owner.NickName}");

        // Master sets initial properties
        if (PhotonNetwork.IsMasterClient)
        {
            EnsurePetsHaveViewIds();
            var ht = new Hashtable
            {
                [PROP_P1] = playerPet.photonView.ViewID,
                [PROP_P2] = enemyPet.photonView.ViewID,
                [PROP_TURN] = "PLAYER" // Master (PLAYER side) goes first
            };
            PhotonNetwork.CurrentRoom.SetCustomProperties(ht);
            Debug.Log("[BattleManager] Master initialized battle properties - PLAYER turn first");
        }
        else
        {
            // Non-masters try to resolve pets from room properties
            TryResolvePetsFromRoomProps(PhotonNetwork.CurrentRoom.CustomProperties);
        }

        // Gate UI on current turn at start
        OnRoomPropertiesUpdate(PhotonNetwork.CurrentRoom.CustomProperties);
    }

    private void EnsurePetsHaveViewIds()
    {
        if (playerPet == null || playerPet.photonView == null)
            Debug.LogError("BattleManager: playerPet/photonView not set on master.");
        if (enemyPet == null || enemyPet.photonView == null)
            Debug.LogError("BattleManager: enemyPet/photonView not set on master.");
    }

    private void TryResolvePetsFromRoomProps(Hashtable props)
    {
        if (playerPet == null && props.ContainsKey(PROP_P1))
        {
            var id = (int)props[PROP_P1];
            if (id > 0) playerPet = PhotonView.Find(id)?.GetComponent<PetBattle>();
        }
        if (enemyPet == null && props.ContainsKey(PROP_P2))
        {
            var id = (int)props[PROP_P2];
            if (id > 0) enemyPet = PhotonView.Find(id)?.GetComponent<PetBattle>();
        }
    }

    // --- Turn / UI logic ----------------------------------------------------
    private bool IsMyTurn(Hashtable props = null)
    {
        props ??= PhotonNetwork.CurrentRoom.CustomProperties;
        if (!props.ContainsKey(PROP_TURN))
        {
            Debug.LogWarning("[BattleManager] PROP_TURN not found in room properties!");
            return false;
        }
        string turn = (string)props[PROP_TURN];

        // If I am the player side (master), my turn is "PLAYER"
        // If I am the enemy side (non-master), my turn is "ENEMY"
        bool myTurn = iAmPlayerSide ? turn == "PLAYER" : turn == "ENEMY";
        Debug.Log($"[BattleManager] IsMyTurn check: turn={turn}, iAmPlayerSide={iAmPlayerSide}, myTurn={myTurn}");
        return myTurn;
    }

    private void SetTurn(string next)
    {
        if (!PhotonNetwork.IsMasterClient) return; // authority
        var ht = new Hashtable { [PROP_TURN] = next };

        // Use WebFlags to ensure the property update is sent immediately and reliably
        PhotonNetwork.CurrentRoom.SetCustomProperties(ht);

        Debug.Log($"[BattleManager] Master set turn to: {next}");

        // Force local UI update immediately for master
        UpdateTurnUI();
    }

    private void ToggleTurn()
    {
        if (!PhotonNetwork.IsMasterClient) return;
        var props = PhotonNetwork.CurrentRoom.CustomProperties;
        var current = props.ContainsKey(PROP_TURN) ? (string)props[PROP_TURN] : "PLAYER";
        var next = current == "PLAYER" ? "ENEMY" : "PLAYER";

        // Set the turn in room properties
        SetTurn(next);

        // Send RPC to all clients to update their UI immediately
        photonView.RPC(nameof(RPC_TurnChanged), RpcTarget.All, next);
    }

    // --- Public UI hooks ----------------------------------------------------
    public void PlayerAttack(int damage)
    {
        Debug.Log($"[BattleManager] PlayerAttack({damage}) clicked by {PhotonNetwork.LocalPlayer.NickName}");

        if (isProcessingAttack)
        {
            Debug.Log("[BattleManager] Attack already in progress, ignoring");
            return;
        }

        if (!IsMyTurn())
        {
            Debug.Log("[BattleManager] Not my turn!");
            return;
        }

        // Determine target
        PetBattle target = iAmPlayerSide ? enemyPet : playerPet;

        if (target == null || target.photonView == null)
        {
            Debug.LogError("[BattleManager] Target pet not found!");
            return;
        }

        int targetId = target.photonView.ViewID;
        Debug.Log($"[BattleManager] Attacking target ViewID: {targetId} (owner: {target.photonView.Owner.NickName}) with damage: {damage}");

        isProcessingAttack = true;

        // Tell everyone to apply damage
        photonView.RPC(nameof(RPC_ApplyDamage), RpcTarget.All, targetId, damage);

        // Request master to toggle turn (works for both master and non-master)
        photonView.RPC(nameof(RPC_RequestToggleTurn), RpcTarget.MasterClient);
    }

    private IEnumerator ToggleTurnAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        ToggleTurn();
        isProcessingAttack = false;

        // Force UI update immediately after turn change
        UpdateTurnUI();
    }

    private IEnumerator ResetAttackFlag(float delay)
    {
        yield return new WaitForSeconds(delay);
        isProcessingAttack = false;

        // Force UI update for non-master as well
        UpdateTurnUI();
    }

    // Update turn UI manually
    private void UpdateTurnUI()
    {
        bool myTurn = IsMyTurn();

        var props = PhotonNetwork.CurrentRoom.CustomProperties;
        string currentTurn = props.ContainsKey(PROP_TURN) ? (string)props[PROP_TURN] : "UNKNOWN";

        Debug.Log($"[BattleManager] UI Update: CurrentTurn={currentTurn}, MyTurn={myTurn}, iAmPlayerSide={iAmPlayerSide}");

        // Enable/disable UI based on turn
        if (commandPanel != null)
        {
            commandPanel.SetActive(myTurn);
            Debug.Log($"[BattleManager] Command panel set to: {myTurn}");
        }

        // Update turn indicator text
        if (turnIndicatorText != null)
        {
            string turnText = myTurn ? "YOUR TURN" : "ENEMY TURN";
            turnIndicatorText.text = turnText;
            turnIndicatorText.color = myTurn ? Color.green : Color.red;
            Debug.Log($"[BattleManager] Turn indicator text set to: {turnText}");
        }
    }

    // If you keep a separate button for AI/enemy testing (local only)
    public void EnemyAttack(int damage)
    {
        Debug.Log($"[BattleManager] EnemyAttack({damage})");

        if (isProcessingAttack) return;
        if (!IsMyTurn()) return;

        PetBattle target = iAmPlayerSide ? playerPet : enemyPet;

        if (target == null || target.photonView == null)
        {
            Debug.LogError("[BattleManager] Target pet not found!");
            return;
        }

        int targetId = target.photonView.ViewID;

        isProcessingAttack = true;
        photonView.RPC(nameof(RPC_ApplyDamage), RpcTarget.All, targetId, damage);

        if (PhotonNetwork.IsMasterClient)
        {
            StartCoroutine(ToggleTurnAfterDelay(0.5f));
        }
        else
        {
            StartCoroutine(ResetAttackFlag(0.5f));
        }
    }

    // --- RPCs ---------------------------------------------------------------
    [PunRPC]
    private void RPC_ApplyDamage(int targetViewId, int amount)
    {
        Debug.Log($"[BattleManager] RPC_ApplyDamage called: ViewID={targetViewId}, Damage={amount}");

        var targetView = PhotonView.Find(targetViewId);
        if (targetView == null)
        {
            Debug.LogWarning($"RPC_ApplyDamage: target view {targetViewId} not found");
            return;
        }

        var pet = targetView.GetComponent<PetBattle>();
        if (pet == null)
        {
            Debug.LogWarning("RPC_ApplyDamage: PetBattle missing on target.");
            return;
        }

        bool isDead = pet.ApplyDamage(amount);
        Debug.Log($"[BattleManager] Damage applied. Pet HP: {pet.currentHealth}/{pet.maxHealth}");

        // Master decides win/loss once per hit
        if (PhotonNetwork.IsMasterClient)
        {
            CheckWinLose();
        }
    }

    // Add RPC to sync turn changes and reset attack flag
    [PunRPC]
    private void RPC_TurnChanged(string newTurn)
    {
        Debug.Log($"[BattleManager] RPC_TurnChanged received: {newTurn}");

        // CRITICAL FIX: Reset the attack flag so the new turn player can attack
        isProcessingAttack = false;
        Debug.Log("[BattleManager] Attack flag reset for new turn");

        UpdateTurnUI();
    }

    [PunRPC]
    private void RPC_RequestToggleTurn()
    {
        Debug.Log($"[BattleManager] RPC_RequestToggleTurn received from {PhotonNetwork.LocalPlayer.NickName}");

        // Only the master should process this request
        if (!PhotonNetwork.IsMasterClient)
        {
            Debug.LogWarning("[BattleManager] RPC_RequestToggleTurn called on non-master, ignoring");
            return;
        }

        // Master toggles the turn
        ToggleTurn();
    }

    private void CheckWinLose()
    {
        if (playerPet == null || enemyPet == null) return;

        if (playerPet.IsDead)
        {
            photonView.RPC(nameof(RPC_EndBattle), RpcTarget.All, "ENEMY_WON");
        }
        else if (enemyPet.IsDead)
        {
            photonView.RPC(nameof(RPC_EndBattle), RpcTarget.All, "PLAYER_WON");
        }
    }

    [PunRPC]
    private void RPC_EndBattle(string result)
    {
        Debug.Log($"[BattleManager] Battle result: {result}");
        // TODO: Disable UI, show result panel, etc.
    }

    // --- Photon callbacks ---------------------------------------------------
    public override void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
    {
        Debug.Log($"[BattleManager] OnRoomPropertiesUpdate called. Changed properties: {string.Join(", ", propertiesThatChanged.Keys)}");

        // Resolve pets if late
        TryResolvePetsFromRoomProps(PhotonNetwork.CurrentRoom.CustomProperties);

        // Update the turn UI whenever room properties change
        if (propertiesThatChanged.ContainsKey(PROP_TURN))
        {
            Debug.Log($"[BattleManager] PROP_TURN changed to: {propertiesThatChanged[PROP_TURN]}");
            UpdateTurnUI();
        }
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        // Master can republish the current properties to help late joiners
        if (!PhotonNetwork.IsMasterClient) return;

        EnsurePetsHaveViewIds();
        var ht = new Hashtable
        {
            [PROP_P1] = playerPet != null ? playerPet.photonView.ViewID : -1,
            [PROP_P2] = enemyPet != null ? enemyPet.photonView.ViewID : -1,
            // keep current turn unchanged
        };
        PhotonNetwork.CurrentRoom.SetCustomProperties(ht);
    }
}