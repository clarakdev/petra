using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class BattleManager : MonoBehaviourPun
{
    [SerializeField] private int currentTurnActor;   // actor number whose turn it is

    private void Start()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            photonView.RPC(nameof(RPC_SetTurn), RpcTarget.All, PhotonNetwork.MasterClient.ActorNumber);
        }
    }

    public bool IsMyTurn =>
        PhotonNetwork.LocalPlayer.ActorNumber == currentTurnActor;

    // Called by UI (MoveButton) on click
    public void TryAttack(DamageData move)
    {
        if (!IsMyTurn || move == null) return;

        // Apply damage on all clients to the enemy pet and then pass turn
        photonView.RPC(nameof(RPC_ApplyDamageToEnemy), RpcTarget.All, move.power);

        int next = GetOtherPlayerActorNumber();
        photonView.RPC(nameof(RPC_SetTurn), RpcTarget.All, next);
    }

    [PunRPC]
    private void RPC_SetTurn(int actorNumber)
    {
        currentTurnActor = actorNumber;
    }

    [PunRPC]
    private void RPC_ApplyDamageToEnemy(int damage)
    {
        // On each client, "enemy" = the pet that is NOT mine
        foreach (var pet in FindObjectsOfType<PetBattle>())
        {
            if (!pet.photonView.IsMine)
            {
                pet.TakeDamage(damage);
            }
        }
    }

    private int GetOtherPlayerActorNumber()
    {
        foreach (Player p in PhotonNetwork.PlayerList)
            if (p.ActorNumber != currentTurnActor)
                return p.ActorNumber;

        return currentTurnActor; // fallback (shouldn’t happen in 1v1)
    }
}
