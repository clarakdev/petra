using Photon.Pun;
using UnityEngine;

public class PetBattle : MonoBehaviourPun
{
    [Header("Sprites")]
    public Sprite battleSpritePlayerSide;
    public Sprite battleSpriteEnemySide;

    [Header("Health")]
    public int maxHealth = 100;
    public int currentHealth;
    public HealthBar healthBar;

    void Start()
    {
        // Health is set by spawner, so no need to set here
    }

    public void SetFacing(bool isPlayerSide)
    {
        var spriteRenderer = GetComponent<SpriteRenderer>();
        spriteRenderer.sprite = isPlayerSide ? battleSpritePlayerSide : battleSpriteEnemySide;
    }

    [PunRPC]
    public void ApplyDamage(int damage)
    {
        currentHealth = Mathf.Max(0, currentHealth - damage);
        if (healthBar != null)
            healthBar.SetHealth(currentHealth);
    }
}