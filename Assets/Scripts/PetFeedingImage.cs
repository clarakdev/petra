using UnityEngine;
using UnityEngine.UI;

public class PetFeedingImage : MonoBehaviour
{
    [SerializeField] private Image petImage;
    [SerializeField] private Sprite defaultPet;

    private void Awake()
    {
        if (petImage == null)
            petImage = GetComponent<Image>();

        if (petImage != null && petImage.sprite == null && defaultPet != null)
            petImage.sprite = defaultPet;

        if (petImage != null)
            petImage.preserveAspect = true;
    }

    public void SetPet(Sprite newPet)
    {
        if (petImage == null) return;

        petImage.sprite = newPet != null ? newPet : defaultPet;
        petImage.preserveAspect = true;
    }
}
