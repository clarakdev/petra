using UnityEngine;

public class PetSelectionManager : MonoBehaviour
{
    public static PetSelectionManager instance;
    public Pet[] pets;
    public Pet currentPet;
    public static int SelectedPetIndex = 0;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        if (pets.Length > 0)
        {
            currentPet = pets[0];
        }
    }

    public void SetPet(Pet pet)
    {
        currentPet = pet;

        // Store the index for later use in matchmaking
        for (int i = 0; i < pets.Length; i++)
        {
            if (pets[i] == pet)
            {
                SelectedPetIndex = i;
                break;
            }
        }

        // Remember the selected pet name
        PlayerPrefs.SetString("CurrentPet", pet.petName);
        PlayerPrefs.Save();

        // Debug log (kept for clarity)
        string petLabel = !string.IsNullOrEmpty(pet.name) ? pet.name : pet.petName;
        Debug.Log("[PetSelectionManager] Current pet = " + petLabel);

        // Activate only the selected pet prefab
        foreach (Pet p in pets)
        {
            GameObject prefabObj = p.prefab != null ? p.prefab : p.petPrefab;
            if (prefabObj != null)
                prefabObj.SetActive(p == pet);
        }

        // Assign correct pet name to accessory manager if present
        GameObject selectedPrefab = pet.prefab != null ? pet.prefab : pet.petPrefab;
        if (selectedPrefab != null)
        {
            var accessoryMgr = selectedPrefab.GetComponent<PetAccessoryManager>();
            if (accessoryMgr != null)
            {
                accessoryMgr.petName = !string.IsNullOrEmpty(pet.petName) ? pet.petName : pet.name;
                // accessoryMgr.LoadAccessory(); // ❌ Removed safely
            }
        }
    }
}
