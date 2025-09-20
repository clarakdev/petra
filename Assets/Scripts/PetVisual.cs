using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class PetVisual : MonoBehaviour
{
    [SerializeField] private SpriteRenderer sr;

    void Awake()
    {
        if (!sr) sr = GetComponent<SpriteRenderer>();
    }

    void Start()
    {
        var mgr = PetSelectionManager.instance;
        if (mgr == null || mgr.currentPet == null)
        {
            Debug.LogWarning("[PetVisual] No PetSelectionManager or currentPet found.");
            return;
        }

        // Use the team's Pet.cardImage
        if (sr)
        {
            var sprite = mgr.currentPet.cardImage;
            if (sprite == null)
                Debug.LogWarning("[PetVisual] currentPet.cardImage is null.");
            sr.sprite = sprite;
        }
    }
}