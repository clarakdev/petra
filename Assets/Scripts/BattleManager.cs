using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;
using UnityEngine;
using System.Collections;
using TMPro;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public class BattleManager : MonoBehaviourPunCallbacks
{
    public PetBattle playerPet;
    public PetBattle enemyPet;

    public GameObject commandPanel;
    public TextMeshProUGUI turnIndicatorText;

    [Header("Battle Result")]
    [SerializeField] private BattleResultManager resultManager;

    private const string PROP_TURN = "Turn";
    private const string PROP_P1 = "P1";
    private const string PROP_P2 = "P2";

    public bool iAmPlayerSide = true;
    private bool isProcessingAttack = false;
    private bool battleEnded = false;

    [Header("Audio")]
    [SerializeField] private AudioClip punchClip;

    private void Awake()
    {
        if (resultManager == null)
        {
            resultManager = FindObjectOfType<BattleResultManager>();
        }
    }

    private void DeterminePlayerSide()
    {
        iAmPlayerSide = PhotonNetwork.IsMasterClient;
    }

    private void Start()
    {
        StartCoroutine(WaitForPetsAndInit());
    }

    private IEnumerator WaitForPetsAndInit()
    {
        DeterminePlayerSide();

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
        else
        {
            TryResolvePetsFromRoomProps(PhotonNetwork.CurrentRoom.CustomProperties);
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
        return iAmPlayerSide ? turn == "PLAYER" : turn == "ENEMY";
    }

    private void SetTurn(string next)
    {
        if (!PhotonNetwork.IsMasterClient) return;
        var ht = new Hashtable { [PROP_TURN] = next };
        PhotonNetwork.CurrentRoom.SetCustomProperties(ht);
        UpdateTurnUI();
    }

    private void ToggleTurn()
    {
        if (!PhotonNetwork.IsMasterClient) return;
        var props = PhotonNetwork.CurrentRoom.CustomProperties;
        var current = props.ContainsKey(PROP_TURN) ? (string)props[PROP_TURN] : "PLAYER";
        var next = current == "PLAYER" ? "ENEMY" : "PLAYER";
        SetTurn(next);
        photonView.RPC(nameof(RPC_TurnChanged), RpcTarget.All, next);
    }

    public void PlayerAttack(int damage)
    {
        if (battleEnded || isProcessingAttack || !IsMyTurn()) return;

        PetBattle target = iAmPlayerSide ? enemyPet : playerPet;
        if (target == null || target.photonView == null) return;

        int targetId = target.photonView.ViewID;
        isProcessingAttack = true;

        photonView.RPC(nameof(RPC_ApplyDamage), RpcTarget.All, targetId, damage);
        photonView.RPC(nameof(RPC_RequestToggleTurn), RpcTarget.MasterClient);
    }

    private void UpdateTurnUI()
    {
        if (battleEnded) return;

        bool myTurn = IsMyTurn();

        if (commandPanel != null)
        {
            commandPanel.SetActive(myTurn);
        }

        if (turnIndicatorText != null)
        {
            string turnText = myTurn ? "YOUR TURN" : "ENEMY TURN";
            turnIndicatorText.text = turnText;
            turnIndicatorText.color = myTurn ? Color.green : Color.red;
        }
    }

    [PunRPC]
    private void RPC_ApplyDamage(int targetViewId, int amount)
    {
        var targetView = PhotonView.Find(targetViewId);
        if (targetView == null) return;

        var pet = targetView.GetComponent<PetBattle>();
        if (pet == null) return;

        if (SoundManager.Instance != null && punchClip != null)
        {
            SoundManager.Instance.PlaySFX(punchClip);
        }

        bool isDead = pet.ApplyDamage(amount);

        if (PhotonNetwork.IsMasterClient && isDead && !battleEnded)
        {
            CheckWinLose();
        }
    }

    [PunRPC]
    private void RPC_TurnChanged(string newTurn)
    {
        isProcessingAttack = false;
        UpdateTurnUI();
    }

    [PunRPC]
    private void RPC_RequestToggleTurn()
    {
        if (!PhotonNetwork.IsMasterClient) return;
        ToggleTurn();
    }

    private void CheckWinLose()
    {
        if (battleEnded) return;
        if (playerPet == null || enemyPet == null) return;

        // FIXED: Use ViewID to identify which actual player's pet died
        int deadPetOwnerActorNumber = -1;

        if (playerPet.IsDead)
        {
            deadPetOwnerActorNumber = playerPet.photonView.Owner.ActorNumber;
        }
        else if (enemyPet.IsDead)
        {
            deadPetOwnerActorNumber = enemyPet.photonView.Owner.ActorNumber;
        }

        if (deadPetOwnerActorNumber > 0)
        {
            battleEnded = true;
            Debug.Log($"[BattleManager] Battle ended. Dead pet owner ActorNumber: {deadPetOwnerActorNumber}");
            photonView.RPC(nameof(RPC_EndBattle), RpcTarget.All, deadPetOwnerActorNumber);
        }
    }

    [PunRPC]
    private void RPC_EndBattle(int loserActorNumber)
    {
        if (battleEnded)
        {
            // Even if already processed, we need to show result for THIS client
            Debug.Log($"[BattleManager] RPC_EndBattle called again. LoserActorNumber: {loserActorNumber}, MyActorNumber: {PhotonNetwork.LocalPlayer.ActorNumber}");
        }

        battleEnded = true;

        if (commandPanel != null)
        {
            commandPanel.SetActive(false);
        }

        bool iWon = (PhotonNetwork.LocalPlayer.ActorNumber != loserActorNumber);

        Debug.Log($"[BattleManager] Result for {PhotonNetwork.LocalPlayer.NickName} (ActorNumber {PhotonNetwork.LocalPlayer.ActorNumber}): " +
                  $"iWon={iWon}, loserActorNumber={loserActorNumber}");

        if (resultManager == null)
        {
            resultManager = FindObjectOfType<BattleResultManager>();
        }

        if (resultManager != null)
        {
            resultManager.ShowResult(iWon);
        }
        else
        {
            Debug.LogError("[BattleManager] No BattleResultManager found!");
        }
    }

    [PunRPC]
    private void RPC_ApplyHealing(int targetViewId, int amount)
    {
        var targetView = PhotonView.Find(targetViewId);
        if (targetView == null)
        {
            Debug.LogError($"[BattleManager] RPC_ApplyHealing: Could not find PhotonView {targetViewId}");
            return;
        }

        var pet = targetView.GetComponent<PetBattle>();
        if (pet == null)
        {
            Debug.LogError($"[BattleManager] RPC_ApplyHealing: No PetBattle component on ViewID {targetViewId}");
            return;
        }

        int oldHP = pet.currentHealth;
        pet.Heal(amount);
        int newHP = pet.currentHealth;

        Debug.Log($"[BattleManager] RPC_ApplyHealing: Pet healed {oldHP} -> {newHP} (requested {amount})");
    }

    [PunRPC]
    private void RPC_IncreaseMaxHealth(int targetViewId, int amount)
    {
        var targetView = PhotonView.Find(targetViewId);
        if (targetView == null)
        {
            Debug.LogError($"[BattleManager] RPC_IncreaseMaxHealth: Could not find PhotonView {targetViewId}");
            return;
        }

        var pet = targetView.GetComponent<PetBattle>();
        if (pet == null)
        {
            Debug.LogError($"[BattleManager] RPC_IncreaseMaxHealth: No PetBattle component on ViewID {targetViewId}");
            return;
        }

        int oldMaxHP = pet.maxHealth;
        int oldCurrentHP = pet.currentHealth;
        pet.maxHealth += amount;
        pet.currentHealth = pet.maxHealth; // Set current health to new max
        int newMaxHP = pet.maxHealth;

        // Update health bar
        if (pet.healthBar != null)
        {
            pet.healthBar.SetMaxHealth(newMaxHP);
            pet.healthBar.SetHealth(pet.currentHealth);
        }

        if (pet.healthText != null)
        {
            pet.healthText.text = $"{pet.currentHealth} / {pet.maxHealth}";
        }

        Debug.Log($"[BattleManager] RPC_IncreaseMaxHealth: Pet max HP increased {oldMaxHP} -> {newMaxHP} (+{amount})");
    }

    public override void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
    {
        TryResolvePetsFromRoomProps(PhotonNetwork.CurrentRoom.CustomProperties);

        if (propertiesThatChanged.ContainsKey(PROP_TURN))
        {
            UpdateTurnUI();
        }
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        EnsurePetsHaveViewIds();
        var ht = new Hashtable
        {
            [PROP_P1] = playerPet != null ? playerPet.photonView.ViewID : -1,
            [PROP_P2] = enemyPet != null ? enemyPet.photonView.ViewID : -1,
        };
        PhotonNetwork.CurrentRoom.SetCustomProperties(ht);
    }
}