// ExitToStore.cs
using UnityEngine;
using UnityEngine.SceneManagement;

public class ExitToStore : MonoBehaviour
{
    const string CameFromShopKey = "CameFromShop";
    public void Go()
    {
        PlayerPrefs.SetInt(CameFromShopKey, 1);
        PlayerPrefs.Save();
        SceneManager.LoadScene("StoreScene");
    }
}
