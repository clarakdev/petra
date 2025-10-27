using UnityEngine;

public class PetAccessoryManager : MonoBehaviour
{
    [Header("Pet Setup")]
    public string petName;                 
    public SpriteRenderer petRenderer;     

    [Header("Directional Sprites (Base Pet)")]
    public Sprite frontSprite;
    public Sprite backSprite;
    public Sprite leftSprite;
    public Sprite rightSprite;

    private EquipType equippedType = EquipType.None;
    private string currentFacing = "Front";
    private string Key => $"Equipped_{petName}";

    private void Awake()
    {
        if (string.IsNullOrEmpty(petName))
            petName = gameObject.name;

        if (petRenderer == null)
            petRenderer = GetComponent<SpriteRenderer>();
    }

    private void Start()
    {
        LoadAccessory();
    }

    public void Equip(EquipType type)
    {
        equippedType = type;
        PlayerPrefs.SetString(Key, type.ToString());
        UpdateSprite();
        Debug.Log($"[PetAccessoryManager] Equipped {type} on {petName}");
    }

    public void Unequip()
    {
        equippedType = EquipType.None;
        PlayerPrefs.SetString(Key, "None");
        UpdateSprite();
        Debug.Log($"[PetAccessoryManager] Unequipped on {petName}");
    }

    public void ToggleAccessory(EquipType type)
    {
        if (equippedType == type) Unequip();
        else Equip(type);
    }

    public void LoadAccessory()
    {
        if (System.Enum.TryParse(PlayerPrefs.GetString(Key, "None"), out EquipType saved))
            equippedType = saved;
        UpdateSprite();
    }

    public void SetFacing(string facing)
    {
        currentFacing = facing;
        UpdateSprite();
    }

    private void UpdateSprite()
    {
        string spriteName = $"Pet{petName}";

        if (equippedType == EquipType.GreenHat) spriteName += "GreenHat";
        else if (equippedType == EquipType.StarSunglasses) spriteName += "StarSunglasses";

        spriteName += currentFacing;

        Sprite newSprite = Resources.Load<Sprite>($"Sprites/{spriteName}");

        if (newSprite != null)
        {
            petRenderer.sprite = newSprite;
        }
        else
        {
            // fallback to base pet directional sprites
            switch (currentFacing)
            {
                case "Back": petRenderer.sprite = backSprite; break;
                case "Left": petRenderer.sprite = leftSprite; break;
                case "Right": petRenderer.sprite = rightSprite; break;
                default: petRenderer.sprite = frontSprite; break;
            }
        }
    }
}
