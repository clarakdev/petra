using UnityEngine;

public class PlayerPrefsCleaner : MonoBehaviour
{
    void Awake()
    {
        string[] pets = { "Flower", "Deer", "Fox", "Snake" };

        foreach (string pet in pets)
        {
            PlayerPrefs.DeleteKey($"Equipped_{pet}");
        }

        PlayerPrefs.DeleteKey("CurrentPet");
        PlayerPrefs.DeleteKey("LastSelectedPet");

        PlayerPrefs.Save();
        Debug.Log("[PlayerPrefsCleaner] Cleared all accessory data and pet selection keys.");
    }
}
