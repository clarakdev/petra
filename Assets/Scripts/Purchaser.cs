using UnityEngine;

public class Purchaser : MonoBehaviour
{
    [SerializeField] private PlayerCurrency playerCurrency;

    private void Awake()
    {
        if (!playerCurrency)
            playerCurrency = FindObjectOfType<PlayerCurrency>(); // auto-find
    }

    public int GetCurrentCurrency() => playerCurrency ? playerCurrency.currency : 0;

    public bool SpendCurrency(int amount)
    {
        if (!playerCurrency) return false;
        return playerCurrency.SpendCurrency(amount);
    }

    public void EarnCurrency(int amount)
    {
        if (!playerCurrency) return;
        playerCurrency.EarnCurrency(amount);
    }
}
