using TMPro;
using UnityEngine;

public class CurrencyUI : MonoBehaviour
{
    public TextMeshProUGUI currencyText;

    public void UpdateCurrencyUI(int amount)
    {
        if (currencyText != null)
        {
            currencyText.text = "COINS: " + amount;
        }
    }
}
