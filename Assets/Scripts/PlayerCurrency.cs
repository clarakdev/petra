using UnityEngine;
using UnityEngine.Events;

public class PlayerCurrency : MonoBehaviour
{
    public static PlayerCurrency Instance { get; private set; }

    [Header("Currency Settings")]
    public int currency = 1000;
    public UnityEvent<int> OnCurrencyChanged;

    private const int STARTING_COINS = 1000;
    private const string KEY_CURRENCY = "player_currency";

    private void Awake()
    {
        // Singleton setup
        if (Instance && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // MODIFIED: Only reset to 1000 if there's no save file
        // If there is a save file, the SaveSystem will load the correct amount
        if (SaveSystem.Instance == null || !SaveSystem.Instance.HasSaveFile())
        {
            currency = STARTING_COINS;
            Debug.Log($"[PlayerCurrency] No save file - initialized with {currency} coins.");
        }
        else
        {
            Debug.Log($"[PlayerCurrency] Save file exists - waiting for SaveSystem to load currency.");
        }

        OnCurrencyChanged?.Invoke(currency);
    }

    public void EarnCurrency(int amount)
    {
        if (amount <= 0) return;

        currency += amount;
        PlayerPrefs.SetInt(KEY_CURRENCY, currency);
        PlayerPrefs.Save();

        Debug.Log($"[PlayerCurrency] Earned {amount} coins! New total: {currency}");
        OnCurrencyChanged?.Invoke(currency);
    }

    public bool SpendCurrency(int amount)
    {
        if (currency < amount)
        {
            Debug.LogWarning("[PlayerCurrency] Not enough coins to spend!");
            return false;
        }

        currency -= amount;
        PlayerPrefs.SetInt(KEY_CURRENCY, currency);
        PlayerPrefs.Save();

        Debug.Log($"[PlayerCurrency] Spent {amount} coins. Remaining: {currency}");
        OnCurrencyChanged?.Invoke(currency);
        return true;
    }

    public void ResetCurrency()
    {
        currency = STARTING_COINS;
        PlayerPrefs.SetInt(KEY_CURRENCY, currency);
        PlayerPrefs.Save();
        OnCurrencyChanged?.Invoke(currency);

        Debug.Log($"[PlayerCurrency] Reset to {STARTING_COINS} coins.");
    }
}