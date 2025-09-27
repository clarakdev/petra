using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using System.Linq;
using UnityEngine;

public class PetBattleSpawner : MonoBehaviourPunCallbacks
{
    public Vector2 playerSpawnPosition = new Vector2(-5, -1);
    public Vector2 enemySpawnPosition = new Vector2(5, 2);

    private bool enemyPetSpawned = false;

    void Start()
    {
        // Try to get the local player's selected pet index
        int localPetIndex = GetLocalPetIndex();
        Pet localPet = null;

        if (localPetIndex >= 0 && localPetIndex < PetSelectionManager.instance.pets.Length)
        {
            localPet = PetSelectionManager.instance.pets[localPetIndex];
        }
        else
        {
            // Fallback to currentPet if index is not set
            localPet = PetSelectionManager.instance.currentPet;
        }

        if (localPet == null || localPet.battlePrefab == null)
        {
            Debug.LogError("[BattleManager] Local pet or battle prefab not set.");
            return;
        }

        // Spawn local player's pet (player side)
        GameObject playerPet = Instantiate(localPet.battlePrefab, playerSpawnPosition, Quaternion.identity);
        var playerPetBattle = playerPet.GetComponent<PetBattle>();
        if (playerPetBattle != null)
            playerPetBattle.SetFacing(true);

        // Try to spawn enemy's pet if the property is already set
        int enemyPetIndex = GetEnemyPetIndex();
        if (enemyPetIndex >= 0)
        {
            TrySpawnEnemyPet(enemyPetIndex);
        }
    }

    private void TrySpawnEnemyPet(int enemyPetIndex)
    {
        if (enemyPetSpawned) return;

        if (enemyPetIndex >= 0 && enemyPetIndex < PetSelectionManager.instance.pets.Length)
        {
            Pet enemyPet = PetSelectionManager.instance.pets[enemyPetIndex];
            if (enemyPet != null && enemyPet.battlePrefab != null)
            {
                GameObject enemyPetObj = Instantiate(enemyPet.battlePrefab, enemySpawnPosition, Quaternion.identity);
                var enemyPetBattle = enemyPetObj.GetComponent<PetBattle>();
                if (enemyPetBattle != null)
                    enemyPetBattle.SetFacing(false);

                enemyPetSpawned = true;
                Debug.Log("[BattleManager] Enemy pet spawned: " + enemyPet.name);
            }
            else
            {
                Debug.LogError("[BattleManager] Enemy pet or battle prefab not set.");
            }
        }
    }

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
    {
        Debug.Log($"[BattleManager] OnPlayerPropertiesUpdate for {targetPlayer.NickName}: {string.Join(", ", changedProps.Keys.Cast<object>())}");
        if (!targetPlayer.IsLocal && changedProps.ContainsKey("SelectedPetIndex") && !enemyPetSpawned)
        {
            int enemyPetIndex = (int)changedProps["SelectedPetIndex"];
            TrySpawnEnemyPet(enemyPetIndex);
        }
    }

    private int GetLocalPetIndex()
    {
        if (PhotonNetwork.InRoom && PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue("SelectedPetIndex", out object indexObj) && indexObj is int index)
        {
            return index;
        }
        // Return -1 if not set
        return -1;
    }

    private int GetEnemyPetIndex()
    {
        if (PhotonNetwork.InRoom)
        {
            foreach (var kvp in PhotonNetwork.CurrentRoom.Players)
            {
                var player = kvp.Value;
                if (!player.IsLocal)
                {
                    if (player.CustomProperties.TryGetValue("SelectedPetIndex", out object indexObj) && indexObj is int index)
                    {
                        return index;
                    }
                }
            }
        }
        // Return -1 if not found
        return -1;
    }
}