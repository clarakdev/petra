using UnityEngine;

[CreateAssetMenu(menuName = "Move/Damage", fileName = "NewDamage")]
public class DamageData : ScriptableObject
{
    [Header("Basic Info")]
    public string moveName = "New Move";
    public string description = "Describe what this move does.";

    [Header("Battle Stats")]
    public int power = 10;         // base damage
    public float accuracy = 1f;    // 1 = 100% hit rate

    [Header("Visuals & FX (optional)")]
    public Color moveColor = Color.white;  // UI color or effect tint
    public AudioClip soundEffect;
}
