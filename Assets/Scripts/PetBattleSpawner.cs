using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Linq;
using UnityEngine;

public class PetBattleSpawner : MonoBehaviourPunCallbacks
{
    public Vector2 playerSpawnPosition = new Vector2(-5, -1);  // MY pet (close to me)
    public Vector2 enemySpawnPosition = new Vector2(5, 2);     // ENEMY pet (far away)

    public HealthBar playerHealthBar;  // MY health bar
    public HealthBar enemyHealthBar;   // ENEMY health bar

    private bool myPetSpawned = false;
    private GameObject myPetInstance;

    void Start()
    {
        // First, check if any pets already exist (I joined late)
        RepositionExistingPets();

        // Spawn my pet over the network
        SpawnMyPet();

        // Wait for enemy pet to exist, then position it
        StartCoroutine(WaitForEnemyPet());

        // Also check periodically in case something was missed
        InvokeRepeating(nameof(CheckForUnassignedPets), 1f, 1f);
    }

    private void CheckForUnassignedPets()
    {
        var allPets = FindObjectsOfType<PetBattle>();
        bool foundUnassigned = false;

        foreach (var pet in allPets)
        {
            if (pet.photonView != null && !pet.photonView.IsMine && pet.healthBar == null)
            {
                Debug.Log("[BattleSpawner] Found enemy pet without health bar, fixing...");
                pet.transform.position = enemySpawnPosition;
                pet.SetFacing(false);
                pet.AssignHealthBar(enemyHealthBar);
                foundUnassigned = true;
            }
        }

        if (foundUnassigned)
        {
            AssignPetsToBattleManager();
            CancelInvoke(nameof(CheckForUnassignedPets)); // Stop checking once we found it
        }
    }

    private void RepositionExistingPets()
    {
        // If I'm joining late, other player's pet might already be spawned
        var existingPets = FindObjectsOfType<PetBattle>();

        Debug.Log($"[BattleSpawner] Found {existingPets.Length} existing pets");

        foreach (var pet in existingPets)
        {
            if (pet.photonView != null && !pet.photonView.IsMine)
            {
                // This is the enemy's pet, move it to enemy position on MY screen
                pet.transform.position = enemySpawnPosition;
                pet.SetFacing(false);
                pet.AssignHealthBar(enemyHealthBar);  // Use the new method
                Debug.Log("[BattleSpawner] Repositioned existing enemy pet to enemy side");
            }
        }
    }

    private void SpawnMyPet()
    {
        if (myPetSpawned) return;

        // Get my pet selection
        int localPetIndex = GetLocalPetIndex();
        Pet localPet = null;

        if (localPetIndex >= 0 && localPetIndex < PetSelectionManager.instance.pets.Length)
        {
            localPet = PetSelectionManager.instance.pets[localPetIndex];
        }
        else
        {
            localPet = PetSelectionManager.instance.currentPet;
        }

        if (localPet == null || localPet.battlePrefab == null)
        {
            Debug.LogError("[BattleSpawner] Local pet or battle prefab not set.");
            return;
        }

        // Get path relative to Resources folder
        string prefabPath = localPet.battlePrefab.name;

        // Spawn MY pet over network at a NEUTRAL position first
        // We'll reposition it locally after
        myPetInstance = PhotonNetwork.Instantiate(
            prefabPath,
            Vector3.zero,  // Spawn at origin, then move
            Quaternion.identity
        );

        // Immediately position MY pet on MY screen
        myPetInstance.transform.position = playerSpawnPosition;

        var petBattle = myPetInstance.GetComponent<PetBattle>();
        if (petBattle != null)
        {
            petBattle.SetFacing(true);  // Player side sprite
            petBattle.maxHealth = 100;
            petBattle.currentHealth = petBattle.maxHealth;
            petBattle.AssignHealthBar(playerHealthBar);  // Use the new method

            Debug.Log($"[BattleSpawner] My pet spawned: {localPet.name} with health bar assigned");
        }

        myPetSpawned = true;
    }

    private void ForceRepositionAllPets()
    {
        var allPets = FindObjectsOfType<PetBattle>();

        Debug.Log($"[BattleSpawner] ForceRepositionAllPets found {allPets.Length} pets");

        foreach (var pet in allPets)
        {
            if (pet.photonView == null) continue;

            if (pet.photonView.IsMine)
            {
                // MY pet - only reposition if health bar is missing
                if (pet.healthBar == null)
                {
                    pet.transform.position = playerSpawnPosition;
                    pet.SetFacing(true);
                    pet.AssignHealthBar(playerHealthBar);
                    Debug.Log("[BattleSpawner] Fixed MY pet's missing health bar");
                }
            }
            else
            {
                // ENEMY pet - always reposition and assign health bar
                pet.transform.position = enemySpawnPosition;
                pet.SetFacing(false);
                pet.AssignHealthBar(enemyHealthBar);
                Debug.Log($"[BattleSpawner] Repositioned ENEMY pet ({pet.photonView.Owner.NickName}) to enemy side with health bar");
            }
        }

        // Assign to battle manager
        AssignPetsToBattleManager();
    }

    private IEnumerator WaitForEnemyPet()
    {
        // Wait for the other player's pet to spawn
        PetBattle enemyPet = null;
        float timeout = 10f;
        float elapsed = 0f;

        while (enemyPet == null && elapsed < timeout)
        {
            // Find all pets
            var allPets = FindObjectsOfType<PetBattle>();

            foreach (var pet in allPets)
            {
                // The enemy pet is the one that's NOT mine
                if (pet.photonView != null && !pet.photonView.IsMine)
                {
                    enemyPet = pet;
                    break;
                }
            }

            if (enemyPet == null)
            {
                yield return new WaitForSeconds(0.2f);
                elapsed += 0.2f;
            }
        }

        if (enemyPet != null)
        {
            Debug.Log("[BattleSpawner] Enemy pet detected, setting up");
            // Directly setup the enemy pet
            enemyPet.transform.position = enemySpawnPosition;
            enemyPet.SetFacing(false);
            enemyPet.AssignHealthBar(enemyHealthBar);

            AssignPetsToBattleManager();
        }
        else
        {
            Debug.LogError("[BattleSpawner] Timeout waiting for enemy pet!");
        }
    }

    private void AssignPetsToBattleManager()
    {
        var battleMgr = FindObjectOfType<BattleManager>();
        if (battleMgr == null)
        {
            Debug.LogWarning("[BattleSpawner] No BattleManager found");
            return;
        }

        // Find my pet and enemy pet
        var allPets = FindObjectsOfType<PetBattle>();

        PetBattle myPet = null;
        PetBattle theirPet = null;

        foreach (var pet in allPets)
        {
            if (pet.photonView == null) continue;

            if (pet.photonView.IsMine)
            {
                myPet = pet;  // MY pet
            }
            else
            {
                theirPet = pet;   // ENEMY pet
            }
        }

        // CRITICAL FIX: Assign pets based on who I am
        // For Master (Player 1): playerPet = my pet, enemyPet = their pet
        // For Non-Master (Player 2): playerPet = their pet (Player 1's), enemyPet = my pet
        // This is because "playerPet" in BattleManager refers to Player 1's pet, not "my" pet
        if (PhotonNetwork.IsMasterClient)
        {
            // I am Player 1
            battleMgr.playerPet = myPet;      // Player 1's pet (mine)
            battleMgr.enemyPet = theirPet;    // Player 2's pet (theirs)
            battleMgr.iAmPlayerSide = true;
        }
        else
        {
            // I am Player 2
            battleMgr.playerPet = theirPet;   // Player 1's pet (theirs)
            battleMgr.enemyPet = myPet;       // Player 2's pet (mine)
            battleMgr.iAmPlayerSide = false;
        }

        Debug.Log($"[BattleSpawner] Pets assigned. IsMaster={PhotonNetwork.IsMasterClient}, " +
                  $"playerPet owner={battleMgr.playerPet?.photonView?.Owner?.NickName}, " +
                  $"enemyPet owner={battleMgr.enemyPet?.photonView?.Owner?.NickName}");
    }

    private int GetLocalPetIndex()
    {
        if (PhotonNetwork.InRoom && PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue("SelectedPetIndex", out object indexObj) && indexObj is int index)
        {
            return index;
        }
        return -1;
    }

    // This callback fires when ANY PhotonView is instantiated over the network
    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        // When the other player joins, recheck pet positions
        if (!newPlayer.IsLocal)
        {
            Debug.Log("[BattleSpawner] Other player entered, rechecking positions");
            Invoke(nameof(ForceRepositionAllPets), 0.5f);
        }
    }
}