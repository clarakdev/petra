using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class PetVisual : MonoBehaviour
{
    [SerializeField] private SpriteRenderer sr;
    private string petName;
    private EquipType equippedType = EquipType.None;

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

        petName = mgr.currentPet.petName; // assumes currentPet has a petName field
        LoadAccessory();
    }

    void LoadAccessory()
    {
        string key = $"Equipped_{petName}";
        if (System.Enum.TryParse(PlayerPrefs.GetString(key, "None"), out EquipType saved))
            equippedType = saved;

        UpdateSprite();
    }

    void UpdateSprite()
    {
        string spriteName = $"Pet{petName}";

        // Add accessory suffix if equipped
        if (equippedType == EquipType.GreenHat)
            spriteName += "GreenHat";
        else if (equippedType == EquipType.StarSunglasses)
            spriteName += "StarSunglasses";

        spriteName += "Front"; // default facing direction for Fetch scene

        Sprite loaded = Resources.Load<Sprite>($"Sprites/{spriteName}");
        if (loaded)
        {
            sr.sprite = loaded;
            Debug.Log($"[PetVisual] Applied {equippedType} on {petName}");
        }
        else
        {
            // fallback to base pet sprite from cardImage
            var mgr = PetSelectionManager.instance;
            if (mgr?.currentPet?.cardImage != null)
                sr.sprite = mgr.currentPet.cardImage;
            else
                Debug.LogWarning($"[PetVisual] Missing sprite for {petName}");
        }
    }
}
