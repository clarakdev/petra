using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;
using UnityEngine;

public class BattleManager : MonoBehaviourPunCallbacks
{
    // Assign in inspector OR resolve at runtime after spawn
    public PetBattle playerPet;
    public PetBattle enemyPet;

    // Room property keys
    private const string PROP_TURN = "Turn"; // "PLAYER" or "ENEMY"
    private const string PROP_P1 = "P1";     // playerPet ViewID
    private const string PROP_P2 = "P2";     // enemyPet ViewID

    // Who am I controlling? (set when pets are spawned/assigned)
    public bool iAmPlayerSide = true;

    private void Start()
    {
        StartCoroutine(WaitForPetsAndInit());
    }

    private System.Collections.IEnumerator WaitForPetsAndInit()
    {
        // Wait until both pets are assigned
        while (playerPet == null || enemyPet == null)
        {
            yield return new WaitForSeconds(0.1f);
        }

        // NOW do the initialization
        if (PhotonNetwork.IsMasterClient)
        {
            EnsurePetsHaveViewIds();
            var ht = new Hashtable
            {
                [PROP_P1] = playerPet.photonView.ViewID,
                [PROP_P2] = enemyPet.photonView.ViewID,
                [PROP_TURN] = "PLAYER"
            };
            PhotonNetwork.CurrentRoom.SetCustomProperties(ht);
        }

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

    private bool IsMyTurn(Hashtable props = null)
    {
        props ??= PhotonNetwork.CurrentRoom.CustomProperties;
        if (!props.ContainsKey(PROP_TURN)) return false;

        string turn = (string)props[PROP_TURN];
        // If I am the player side, my turn is "PLAYER"; otherwise, it is "ENEMY"
        return iAmPlayerSide ? turn == "PLAYER" : turn == "ENEMY";
    }

    private void SetTurn(string next)
    {
        if (!PhotonNetwork.IsMasterClient) return; // authority
        var ht = new Hashtable { [PROP_TURN] = next };
        PhotonNetwork.CurrentRoom.SetCustomProperties(ht);
    }

    private void ToggleTurn()
    {
        if (!PhotonNetwork.IsMasterClient) return;
        var props = PhotonNetwork.CurrentRoom.CustomProperties;
        var current = props.ContainsKey(PROP_TURN) ? (string)props[PROP_TURN] : "PLAYER";
        var next = current == "PLAYER" ? "ENEMY" : "PLAYER";
        SetTurn(next);
    }

    public void PlayerAttack(int damage)
    {
        Debug.Log($"PlayerAttack({damage}) clicked by {PhotonNetwork.LocalPlayer.NickName}");

        if (!IsMyTurn()) return; // UI guard
        int targetId = iAmPlayerSide ? enemyPet?.photonView?.ViewID ?? -1
                                     : playerPet?.photonView?.ViewID ?? -1;
        if (targetId <= 0) { Debug.LogError("Target ViewID invalid."); return; }

        // Tell everyone to apply the same damage to the same target
        photonView.RPC(nameof(RPC_ApplyDamage), RpcTarget.All, targetId, damage);

        // Only master flips the turn (optionally after a short delay / animation)
        if (PhotonNetwork.IsMasterClient) ToggleTurn();
    }

    // If you keep a separate button for AI/enemy testing (local only)
    public void EnemyAttack(int damage)
    {
        Debug.Log($"EnemyAttack({damage})");

        if (!IsMyTurn()) return;
        int targetId = iAmPlayerSide ? playerPet?.photonView?.ViewID ?? -1
                                     : enemyPet?.photonView?.ViewID ?? -1;
        if (targetId <= 0) { Debug.LogError("Target ViewID invalid."); return; }

        photonView.RPC(nameof(RPC_ApplyDamage), RpcTarget.All, targetId, damage);
        if (PhotonNetwork.IsMasterClient) ToggleTurn();
    }

    [PunRPC]
    private void RPC_ApplyDamage(int targetViewId, int amount)
    {
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

        bool isDead = pet.ApplyDamage(amount); // make sure this updates HealthBar locally

        // Master decides win/loss once per hit
        if (PhotonNetwork.IsMasterClient)
        {
            CheckWinLose();
        }
    }

    private void CheckWinLose()
    {
        if (playerPet == null || enemyPet == null) return;

        if (playerPet.IsDead)
        {
            photonView.RPC(nameof(RPC_EndBattle), RpcTarget.All, "LOST");
        }
        else if (enemyPet.IsDead)
        {
            photonView.RPC(nameof(RPC_EndBattle), RpcTarget.All, "WON");
        }
    }

    [PunRPC]
    private void RPC_EndBattle(string result)
    {
        // TODO: Disable UI, show result panel, etc.
        Debug.Log($"Battle result: {result}");
        // can add: uiManager.SetMoveButtonsActive(false);
    }

    public override void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
    {
        // Resolve pets if late
        TryResolvePetsFromRoomProps(PhotonNetwork.CurrentRoom.CustomProperties);

        // Gate buttons based on whose turn it is
        bool myTurn = IsMyTurn(PhotonNetwork.CurrentRoom.CustomProperties);
        // TODO: uiManager.SetMoveButtonsActive(myTurn);
        // TODO: uiManager.UpdateTurnText(myTurn ? "Your Turn" : "Enemy Turn");
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
