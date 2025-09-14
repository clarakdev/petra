using UnityEngine;
using UnityEngine.UI;

public class PetFeedingImage : MonoBehaviour
{
    [SerializeField] private Image  petImage;     // auto-grab if left empty
    [SerializeField] private Sprite defaultPet;   // optional fallback

    void Awake()
    {
        if (petImage == null) petImage = GetComponent<Image>();
        if (petImage != null) petImage.preserveAspect = true;
    }

    void OnEnable()
    {
        var mgr = PetSelectionManager.instance;

        if (petImage != null)
        {
            if (mgr != null && mgr.currentPet != null && mgr.currentPet.cardImage != null)
                petImage.sprite = mgr.currentPet.cardImage;
            else if (defaultPet != null)
                petImage.sprite = defaultPet;
        }
    }

    public void SetPet(Sprite s)
    {
        if (petImage == null) return;
        petImage.sprite = (s != null) ? s : defaultPet;
        petImage.preserveAspect = true;
    }
}
