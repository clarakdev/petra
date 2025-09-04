using UnityEngine;

public class PlayerCurrency : MonoBehaviour
{
    public int currency = 0; //current player balance

    public void earnCurrency(int amount){
        if (amount > 0){
            currency = currency + amount; //add to currency
        }
    }
}
