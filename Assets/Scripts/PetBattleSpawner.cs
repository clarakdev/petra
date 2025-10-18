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
                pet.healthBar = enemyHealthBar;
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
            petBattle.healthBar = playerHealthBar;  // MY health bar
            petBattle.maxHealth = 100;
            petBattle.currentHealth = petBattle.maxHealth;
            petBattle.healthBar.SetMaxHealth(petBattle.maxHealth);
        }

        myPetSpawned = true;
        Debug.Log($"[BattleSpawner] My pet spawned: {localPet.name}");

        // Force recheck of all pets immediately after spawning
        Invoke(nameof(ForceRepositionAllPets), 0.1f);
    }

    private void ForceRepositionAllPets()
    {
        var allPets = FindObjectsOfType<PetBattle>();

        foreach (var pet in allPets)
        {
            if (pet.photonView == null) continue;

            if (pet.photonView.IsMine)
            {
                // MY pet always goes to player position
                pet.transform.position = playerSpawnPosition;
                pet.SetFacing(true);
                pet.healthBar = playerHealthBar;
                Debug.Log("[BattleSpawner] Repositioned MY pet to player side");
            }
            else
            {
                // ENEMY pet always goes to enemy position
                pet.transform.position = enemySpawnPosition;
                pet.SetFacing(false);
                pet.healthBar = enemyHealthBar;
                Debug.Log("[BattleSpawner] Repositioned ENEMY pet to enemy side");
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
            Debug.Log("[BattleSpawner] Enemy pet detected, repositioning all pets");
            ForceRepositionAllPets();
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

        foreach (var pet in allPets)
        {
            if (pet.photonView == null) continue;

            if (pet.photonView.IsMine)
            {
                battleMgr.playerPet = pet;  // MY pet
            }
            else
            {
                battleMgr.enemyPet = pet;   // ENEMY pet
            }
        }

        battleMgr.iAmPlayerSide = true;  // I always control the "player" side

        Debug.Log("[BattleSpawner] Pets assigned to BattleManager");
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