using UnityEngine;
using TMPro;

public class CurrencyHUD : MonoBehaviour
{
    [SerializeField] private TMP_Text currencyText;
    private PlayerCurrency playerCurrency;

    private void Start()
    {
        playerCurrency = FindObjectOfType<PlayerCurrency>(); // find the global one
    }

    private void Update()
    {
        if (playerCurrency != null && currencyText != null)
        {
            currencyText.text = $"Coins: {playerCurrency.currency}";
        }
    }
}
