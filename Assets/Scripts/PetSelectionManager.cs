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
        Debug.Log("[PetSelectionManager] Current pet = " + pet.name);
    }
}
