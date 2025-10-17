using Photon.Pun;
using UnityEngine;

public class BattleManager : MonoBehaviourPun
{
    public PetBattle playerPet;
    public PetBattle enemyPet;
    public bool isPlayerTurn = true;

    public void PlayerAttack(int damage)
    {
        Debug.Log("PlayerAttack called with damage: " + damage);

        if (enemyPet == null || enemyPet.photonView == null)
        {
            Debug.LogError("enemyPet or its PhotonView is null!");
            return;
        }

        if (isPlayerTurn)
        {
            enemyPet.photonView.RPC("ApplyDamage", RpcTarget.All, damage);
            isPlayerTurn = false;
        }
    }

    public void EnemyAttack(int damage)
    {
        Debug.Log("EnemyAttack called with damage: " + damage);

        if (playerPet == null || playerPet.photonView == null)
        {
            Debug.LogError("playerPet or its PhotonView is null!");
            return;
        }

        if (!isPlayerTurn)
        {
            playerPet.photonView.RPC("ApplyDamage", RpcTarget.All, damage);
            isPlayerTurn = true;
        }
    }
}