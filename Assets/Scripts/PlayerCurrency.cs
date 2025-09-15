using UnityEngine;

public class PlayerCurrency : MonoBehaviour
{
    public int currency;//variable

    private void Awake()
    {
        currency = 1000;//force default value
    }

    public void EarnCurrency(int amount)
    {
        if (amount > 0)
        {
            currency += amount;
        }
    }

    public bool SpendCurrency(int amount)
    {
        if (amount <= currency)
        {
            currency -= amount;
            return true;
        }
        return false;
    }
}
