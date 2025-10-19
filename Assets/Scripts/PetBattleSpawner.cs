using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Linq;
using UnityEngine;
using TMPro;

public class PetBattleSpawner : MonoBehaviourPunCallbacks
{
    public Vector2 playerSpawnPosition = new Vector2(-5, -1);
    public Vector2 enemySpawnPosition = new Vector2(5, 2);

    public HealthBar playerHealthBar;
    public HealthBar enemyHealthBar;

    private TextMeshProUGUI playerHealthText;
    private TextMeshProUGUI enemyHealthText;

    private bool myPetSpawned = false;
    private GameObject myPetInstance;

    void Awake()
    {
        // Auto-find health text references from the UI hierarchy
        // Find all TextMeshProUGUI in the scene and match by parent names
        var allHealthTexts = FindObjectsOfType<TextMeshProUGUI>();

        foreach (var text in allHealthTexts)
        {
            if (text.name == "HealthText")
            {
                // Check if parent is PlayerStatusPanel or EnemyStatusPanel
                var parentName = text.transform.parent?.name;

                if (parentName == "PlayerStatusPanel")
                    playerHealthText = text;
                else if (parentName == "EnemyStatusPanel")
                    enemyHealthText = text;
            }
        }

        Debug.Log($"[BattleSpawner] Awake: playerHealthText={(playerHealthText != null ? "FOUND" : "NOT FOUND")}, enemyHealthText={(enemyHealthText != null ? "FOUND" : "NOT FOUND")}");
    }

    void Start()
    {
        Debug.Log($"[BattleSpawner] START: playerHealthText={(playerHealthText != null ? "ASSIGNED" : "NULL")}, enemyHealthText={(enemyHealthText != null ? "ASSIGNED" : "NULL")}");

        RepositionExistingPets();
        SpawnMyPet();
        StartCoroutine(WaitForEnemyPet());
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
                Debug.Log($"[BattleSpawner] Found unassigned enemy pet. Health: {pet.currentHealth}/{pet.maxHealth}");

                if (pet.currentHealth < pet.maxHealth && pet.maxHealth == 100)
                {
                    Debug.LogWarning($"[BattleSpawner] Enemy pet has corrupted health ({pet.currentHealth}/{pet.maxHealth}). Waiting for sync...");
                    continue;
                }

                pet.transform.position = enemySpawnPosition;
                pet.SetFacing(false);
                pet.AssignHealthBar(enemyHealthBar);
                pet.AssignHealthText(enemyHealthText);
                Debug.Log($"[BattleSpawner] CheckForUnassignedPets: Assigned text to enemy pet. enemyHealthText is {(enemyHealthText != null ? "NOT NULL" : "NULL")}");
                foundUnassigned = true;
            }
        }

        if (foundUnassigned)
        {
            AssignPetsToBattleManager();
            CancelInvoke(nameof(CheckForUnassignedPets));
        }
    }

    private void RepositionExistingPets()
    {
        var existingPets = FindObjectsOfType<PetBattle>();
        Debug.Log($"[BattleSpawner] Found {existingPets.Length} existing pets");

        foreach (var pet in existingPets)
        {
            if (pet.photonView != null && !pet.photonView.IsMine)
            {
                Debug.Log($"[BattleSpawner] Repositioning existing enemy pet. Current health: {pet.currentHealth}/{pet.maxHealth}");

                pet.transform.position = enemySpawnPosition;
                pet.SetFacing(false);

                StartCoroutine(DelayedHealthAssignment(pet, enemyHealthBar, enemyHealthText));
            }
        }
    }

    private IEnumerator DelayedHealthAssignment(PetBattle pet, HealthBar healthBar, TextMeshProUGUI healthText)
    {
        yield return new WaitForSeconds(0.5f);

        Debug.Log($"[BattleSpawner] Delayed health assignment. Pet health: {pet.currentHealth}/{pet.maxHealth}");

        if (pet.currentHealth > 0 && pet.currentHealth <= pet.maxHealth)
        {
            pet.AssignHealthBar(healthBar);
            pet.AssignHealthText(healthText);
            Debug.Log($"[BattleSpawner] Health bar and text assigned successfully at {pet.currentHealth}/{pet.maxHealth}");
        }
        else
        {
            Debug.LogError($"[BattleSpawner] INVALID health values: {pet.currentHealth}/{pet.maxHealth}. Retrying...");
            StartCoroutine(DelayedHealthAssignment(pet, healthBar, healthText));
        }
    }

    private void SpawnMyPet()
    {
        if (myPetSpawned) return;

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

        string prefabPath = localPet.battlePrefab.name;

        myPetInstance = PhotonNetwork.Instantiate(
            prefabPath,
            Vector3.zero,
            Quaternion.identity
        );

        myPetInstance.transform.position = playerSpawnPosition;

        var petBattle = myPetInstance.GetComponent<PetBattle>();
        if (petBattle != null)
        {
            petBattle.maxHealth = 100;
            petBattle.currentHealth = petBattle.maxHealth;

            Debug.Log($"[BattleSpawner] MY pet health set to: {petBattle.currentHealth}/{petBattle.maxHealth}");

            petBattle.MarkInitialized();
            petBattle.SetFacing(true);

            petBattle.AssignHealthBar(playerHealthBar);
            petBattle.AssignHealthText(playerHealthText);

            Debug.Log($"[BattleSpawner] My pet fully initialized: {localPet.name} at {petBattle.currentHealth}/{petBattle.maxHealth}");
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
                if (pet.healthBar == null)
                {
                    pet.maxHealth = 100;
                    pet.currentHealth = pet.maxHealth;
                    pet.transform.position = playerSpawnPosition;
                    pet.SetFacing(true);
                    pet.AssignHealthBar(playerHealthBar);
                    pet.AssignHealthText(playerHealthText);
                    Debug.Log($"[BattleSpawner] Fixed MY pet's health: {pet.currentHealth}/{pet.maxHealth}. playerHealthText is {(playerHealthText != null ? "NOT NULL" : "NULL")}");
                }
            }
            else
            {
                Debug.Log($"[BattleSpawner] Enemy pet health BEFORE: {pet.currentHealth}/{pet.maxHealth}");

                pet.transform.position = enemySpawnPosition;
                pet.SetFacing(false);

                StartCoroutine(DelayedHealthAssignment(pet, enemyHealthBar, enemyHealthText));

                Debug.Log($"[BattleSpawner] Enemy pet repositioned, waiting for health sync");
            }
        }

        AssignPetsToBattleManager();
    }

    private IEnumerator WaitForEnemyPet()
    {
        PetBattle enemyPet = null;
        float timeout = 10f;
        float elapsed = 0f;

        while (enemyPet == null && elapsed < timeout)
        {
            var allPets = FindObjectsOfType<PetBattle>();

            foreach (var pet in allPets)
            {
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
            Debug.Log($"[BattleSpawner] Enemy pet detected. Initial health: {enemyPet.currentHealth}/{enemyPet.maxHealth}");

            enemyPet.transform.position = enemySpawnPosition;
            enemyPet.SetFacing(false);

            yield return StartCoroutine(WaitForValidHealth(enemyPet));

            Debug.Log($"[BattleSpawner] Enemy pet setup complete. Final health: {enemyPet.currentHealth}/{enemyPet.maxHealth}");

            AssignPetsToBattleManager();
        }
        else
        {
            Debug.LogError("[BattleSpawner] Timeout waiting for enemy pet!");
        }
    }

    private IEnumerator WaitForValidHealth(PetBattle pet)
    {
        float timeout = 5f;
        float elapsed = 0f;
        int lastHealth = pet.currentHealth;

        Debug.Log($"[BattleSpawner] Waiting for valid health sync. Current: {pet.currentHealth}/{pet.maxHealth}");

        while (elapsed < timeout)
        {
            if (pet.currentHealth != lastHealth)
            {
                Debug.Log($"[BattleSpawner] Health changed from {lastHealth} to {pet.currentHealth}");
                lastHealth = pet.currentHealth;
            }

            if (pet.currentHealth == pet.maxHealth && pet.maxHealth == 100)
            {
                Debug.Log($"[BattleSpawner] Valid health detected: {pet.currentHealth}/{pet.maxHealth}");
                pet.AssignHealthBar(enemyHealthBar);
                pet.AssignHealthText(enemyHealthText);
                yield break;
            }

            yield return new WaitForSeconds(0.2f);
            elapsed += 0.2f;
        }

        Debug.LogWarning($"[BattleSpawner] Timeout waiting for health sync. Assigning with current values: {pet.currentHealth}/{pet.maxHealth}");
        pet.AssignHealthBar(enemyHealthBar);
        pet.AssignHealthText(enemyHealthText);
    }

    private void AssignPetsToBattleManager()
    {
        var battleMgr = FindObjectOfType<BattleManager>();
        if (battleMgr == null)
        {
            Debug.LogWarning("[BattleSpawner] No BattleManager found");
            return;
        }

        var allPets = FindObjectsOfType<PetBattle>();
        PetBattle myPet = null;
        PetBattle theirPet = null;

        foreach (var pet in allPets)
        {
            if (pet.photonView == null) continue;

            if (pet.photonView.IsMine)
            {
                myPet = pet;
            }
            else
            {
                theirPet = pet;
            }
        }

        if (PhotonNetwork.IsMasterClient)
        {
            battleMgr.playerPet = myPet;
            battleMgr.enemyPet = theirPet;
            battleMgr.iAmPlayerSide = true;
        }
        else
        {
            battleMgr.playerPet = theirPet;
            battleMgr.enemyPet = myPet;
            battleMgr.iAmPlayerSide = false;
        }

        Debug.Log($"[BattleSpawner] Pets assigned to BattleManager.");
        if (battleMgr.playerPet)
            Debug.Log($"  playerPet: owner={battleMgr.playerPet.photonView.Owner.NickName}, health={battleMgr.playerPet.currentHealth}/{battleMgr.playerPet.maxHealth}");
        if (battleMgr.enemyPet)
            Debug.Log($"  enemyPet: owner={battleMgr.enemyPet.photonView.Owner.NickName}, health={battleMgr.enemyPet.currentHealth}/{battleMgr.enemyPet.maxHealth}");
    }

    private int GetLocalPetIndex()
    {
        if (PhotonNetwork.InRoom && PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue("SelectedPetIndex", out object indexObj) && indexObj is int index)
        {
            return index;
        }
        return -1;
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        if (!newPlayer.IsLocal)
        {
            Debug.Log("[BattleSpawner] Other player entered, rechecking positions");
            Invoke(nameof(ForceRepositionAllPets), 0.5f);
        }
    }
}