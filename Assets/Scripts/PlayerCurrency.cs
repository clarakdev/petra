using UnityEngine;

public class PlayerCurrency : MonoBehaviour
{
    public int currency = 0; //current player balance

    public void EarnCurrency(int amount){
        if (amount > 0){
            currency = currency + amount; //add to currency
        }
    }

    public bool SpendCurrency(int amount){
        if (amount <= currency){
            currency = currency - amount;
            return true; //make purchase 
        }

        return false; //not enough currency to purchase 
    }
}
