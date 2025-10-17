using Photon.Pun;
using UnityEngine;

public class PetBattle : MonoBehaviourPun
{
    [Header("Sprites")]
    public Sprite battleSpritePlayerSide; // Player's pet, seen from behind
    public Sprite battleSpriteEnemySide;  // Opponent's pet, seen from the front

    [Header("Health")]
    public int maxHealth = 100;
    public int currentHealth;
    public HealthBar healthBar;

    void Start()
    {
        currentHealth = maxHealth;
        healthBar.SetMaxHealth(maxHealth);
    }

    public void SetFacing(bool isPlayerSide)
    {
        var spriteRenderer = GetComponent<SpriteRenderer>();
        spriteRenderer.sprite = isPlayerSide ? battleSpritePlayerSide : battleSpriteEnemySide;
    }

    [PunRPC]
    public void InitialiseHealth(int health)
    {
        maxHealth = health;
        currentHealth = health;
        if (healthBar != null)
            healthBar.SetMaxHealth(health);
    }

    [PunRPC]
    public void ApplyDamage(int damage)
    {
        currentHealth = Mathf.Max(0, currentHealth - damage);
        healthBar.SetHealth(currentHealth);
    }
}