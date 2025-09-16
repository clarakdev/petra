using UnityEngine;

public class PetBattle : MonoBehaviour
{
    public Sprite battleSpritePlayerSide; // Player's pet, seen from behind
    public Sprite battleSpriteEnemySide;  // Opponent's pet, seen from the front

    public int maxHealth;
    public int currentHealth;

    public void SetFacing(bool isPlayerSide)
    {
        var spriteRenderer = GetComponent<SpriteRenderer>();
        spriteRenderer.sprite = isPlayerSide ? battleSpritePlayerSide : battleSpriteEnemySide;
    }

    // Add battle logic here (e.g., TakeDamage, Attack, etc.)
}