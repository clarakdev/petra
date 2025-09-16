using UnityEngine;

public class Purchaser : MonoBehaviour
{
    [SerializeField] private PlayerCurrency playerCurrency;

    public int GetCurrentCurrency()
    {
        return playerCurrency ? playerCurrency.currency : 0;
    }

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
