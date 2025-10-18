using Photon.Pun;
using UnityEngine;

public class PetBattle : MonoBehaviourPun, IPunObservable
{
    [Header("Sprites")]
    public Sprite battleSpritePlayerSide;
    public Sprite battleSpriteEnemySide;

    [Header("Health")]
    public int maxHealth = 100;
    public int currentHealth;
    public HealthBar healthBar;

    public bool IsDead => currentHealth <= 0;

    private void Awake()
    {
        // Make sure we start with valid HP
        if (currentHealth <= 0) currentHealth = maxHealth;
    }

    private void Start()
    {
        // If a HealthBar is wired, reflect initial values.
        if (healthBar != null)
        {
            healthBar.SetMaxHealth(maxHealth);
            healthBar.SetHealth(currentHealth);
        }
    }

    public void SetFacing(bool isPlayerSide)
    {
        var spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
            spriteRenderer.sprite = isPlayerSide ? battleSpritePlayerSide : battleSpriteEnemySide;
    }

    /// Apply damage locally on this client. Returns true if HP reaches 0.
    /// Call this via RPC so all clients update simultaneously.
    public bool ApplyDamage(int damage)
    {
        currentHealth = Mathf.Max(0, currentHealth - Mathf.Max(0, damage));

        // Update the health bar if it exists
        if (healthBar != null)
        {
            healthBar.SetHealth(currentHealth);
            Debug.Log($"[PetBattle] {photonView.Owner.NickName}'s pet HP: {currentHealth}/{maxHealth} on {PhotonNetwork.LocalPlayer.NickName}'s screen");
        }
        else
        {
            Debug.LogWarning($"[PetBattle] No health bar assigned for {photonView.Owner.NickName}'s pet!");
        }

        return IsDead;
    }

    public void SetMaxHP()
    {
        currentHealth = maxHealth;
        if (healthBar != null)
        {
            healthBar.SetMaxHealth(maxHealth);
            healthBar.SetHealth(currentHealth);
        }
    }

    // Optional heal if you need it later
    public void Heal(int amount)
    {
        currentHealth = Mathf.Min(maxHealth, currentHealth + Mathf.Max(0, amount));
        if (healthBar != null)
            healthBar.SetHealth(currentHealth);
    }

    /// Assign health bar reference - can be called multiple times as pets are repositioned
    public void AssignHealthBar(HealthBar bar)
    {
        healthBar = bar;
        if (healthBar != null)
        {
            healthBar.SetMaxHealth(maxHealth);
            healthBar.SetHealth(currentHealth);
            Debug.Log($"[PetBattle] Health bar assigned to {photonView.Owner.NickName}'s pet. Current HP: {currentHealth}/{maxHealth}");
        }
    }

    // Synchronise health across network
    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            // Send current health to other clients
            stream.SendNext(currentHealth);
            stream.SendNext(maxHealth);
        }
        else
        {
            // Receive health from owner
            currentHealth = (int)stream.ReceiveNext();
            maxHealth = (int)stream.ReceiveNext();

            // Update health bar if we have one
            if (healthBar != null)
            {
                healthBar.SetMaxHealth(maxHealth);
                healthBar.SetHealth(currentHealth);
            }
        }
    }
}