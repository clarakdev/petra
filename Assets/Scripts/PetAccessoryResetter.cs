using UnityEngine;

public class PetAccessoryResetter : MonoBehaviour
{
    private void Awake()
    {
        ResetAllPetAccessories();
    }

    private void ResetAllPetAccessories()
    {
        // all pets in your game
        string[] allPets = { "Flower", "Deer", "Fox", "Snake" };

        foreach (string pet in allPets)
        {
            PlayerPrefs.SetString($"Equipped_{pet}", "None");
        }

        PlayerPrefs.DeleteKey("CurrentPet");
        PlayerPrefs.DeleteKey("LastSelectedPet");
        PlayerPrefs.Save();

        Debug.Log("[PetAccessoryResetter] All accessories cleared — pets start clean this play session.");
    }
}
