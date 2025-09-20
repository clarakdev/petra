using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class BattleManager : MonoBehaviourPunCallbacks
{
    public Vector2 playerSpawnPosition = new Vector2(-5, -3);
    public Vector2 enemySpawnPosition = new Vector2(5, 3);

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

        // Get enemy's selected pet index
        int enemyPetIndex = GetEnemyPetIndex();
        Pet enemyPet = null;
        if (enemyPetIndex >= 0 && enemyPetIndex < PetSelectionManager.instance.pets.Length)
        {
            enemyPet = PetSelectionManager.instance.pets[enemyPetIndex];
        }

        if (enemyPet == null || enemyPet.battlePrefab == null)
        {
            Debug.LogError("[BattleManager] Enemy pet or battle prefab not set.");
            return;
        }

        // Spawn enemy's pet (enemy side)
        GameObject enemyPetObj = Instantiate(enemyPet.battlePrefab, enemySpawnPosition, Quaternion.identity);
        var enemyPetBattle = enemyPetObj.GetComponent<PetBattle>();
        if (enemyPetBattle != null)
            enemyPetBattle.SetFacing(false);
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