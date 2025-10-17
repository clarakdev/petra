using Photon.Pun;
using UnityEngine;

public class BattleManager : MonoBehaviourPun
{
    public PetBattle playerPet;
    public PetBattle enemyPet;
    public HealthBar playerHealthBar;
    public HealthBar enemyHealthBar;
    public bool isPlayerTurn = true;

    void Start()
    {
        if (playerPet != null && playerHealthBar != null)
            playerPet.healthBar = playerHealthBar;

        if (enemyPet != null && enemyHealthBar != null)
            enemyPet.healthBar = enemyHealthBar;
    }

    public void PlayerAttack(int damage)
    {
        if (isPlayerTurn)
        {
            enemyPet.photonView.RPC("ApplyDamage", RpcTarget.All, damage);
            isPlayerTurn = false;
        }
    }

    public void EnemyAttack(int damage)
    {
        if (!isPlayerTurn)
        {
            playerPet.photonView.RPC("ApplyDamage", RpcTarget.All, damage);
            isPlayerTurn = true;
        }
    }
}