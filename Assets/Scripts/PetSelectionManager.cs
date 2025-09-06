using UnityEngine;

public class PetSelectionManager : MonoBehaviour
{
    public static PetSelectionManager instance;
    public Pet[] pets;
    public Pet currentPet;

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
        Debug.Log("[PetSelectionManager] Current pet = " + pet.name);
    }
}
