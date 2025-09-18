using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class BattleManager : MonoBehaviourPunCallbacks
{
    public Vector2 playerSpawnPosition = new Vector2(-5, -3);
    public Vector2 enemySpawnPosition = new Vector2(5, 3);

    void Start()
    {
        // Get local player's selected pet index
        int localPetIndex = GetLocalPetIndex();
        Pet localPet = PetSelectionManager.instance.pets[localPetIndex];
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
        Pet enemyPet = PetSelectionManager.instance.pets[enemyPetIndex];
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
        // Get the local player's selected pet index from custom properties
        if (PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue("SelectedPetIndex", out object indexObj) && indexObj is int index)
        {
            return index;
        }
        // Fallback to 0 if not set
        return 0;
    }

    private int GetEnemyPetIndex()
    {
        // Find the other player in the room
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
        // Fallback to 0 if not found
        return 0;
    }
}