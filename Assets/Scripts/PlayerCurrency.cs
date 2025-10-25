using UnityEngine;

public class PlayerCurrency : MonoBehaviour
{
    public int currency;

    private void Awake()
    {
        // Singleton pattern: ensure only one PlayerCurrency exists
        if (FindObjectsOfType<PlayerCurrency>().Length > 1)
        {
            Destroy(gameObject);
            return;
        }
        DontDestroyOnLoad(gameObject);

        // Initialize coins early (before GameState tries to read it)
        if (currency <= 0)
        {
            currency = 1000; // starting coins
        }
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
