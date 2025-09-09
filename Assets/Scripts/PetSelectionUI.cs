using UnityEngine;
using UnityEngine.UI;

public class PetSelectionUI : MonoBehaviour
{
    public GameObject optionPrefab;
    public Transform prevPet;
    public Transform selectedPet;

    private void Start()
    {
        foreach (Pet p in PetSelectionManager.instance.pets)
        {
            GameObject option = Instantiate(optionPrefab, transform);
            Button button = option.GetComponent<Button>();

            button.onClick.AddListener(() =>
            {
                PetSelectionManager.instance.SetPet(p);
                if (selectedPet != null)
                {
                    prevPet = selectedPet; // if there was a previously selected pet, store it in prevPet
                }

                selectedPet = option.transform;

            });

            Image petImage = option.transform.Find("PetCard_1").GetComponent<Image>();
            petImage.sprite = p.icon;
        }
    }

    private void Update()
    {
        if (selectedPet != null)
        {
            selectedPet.localScale = Vector3.Lerp(selectedPet.localScale,
                new Vector3(1.2f, 1.2f, 1.2f), Time.deltaTime * 10);
        }

        if (prevPet != null)
        {
            prevPet.localScale = Vector3.Lerp(prevPet.localScale,
                new Vector3(1f, 1f, 1f), Time.deltaTime * 10);
        }
    }
}
