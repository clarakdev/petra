using UnityEngine;
using TMPro;

public class CurrencyHUD : MonoBehaviour
{
    [SerializeField] private TMP_Text currencyText;        //TMP text 
    [SerializeField] private PlayerCurrency playerCurrency; //PlayerCurrency object 

    private void Start() => Refresh();
    private void Update() => Refresh(); //update loop 

    private void Refresh()
    {
        if (currencyText == null || playerCurrency == null) return;
        currencyText.text = $"Coins: {playerCurrency.currency}";
    }
}
