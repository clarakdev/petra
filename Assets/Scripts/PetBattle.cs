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

    private bool isInitialized = false;

    private void Awake()
    {
        Debug.Log($"[PetBattle] Awake: currentHealth={currentHealth}, maxHealth={maxHealth}");
    }

    private void Start()
    {
        Debug.Log($"[PetBattle] Start: currentHealth={currentHealth}, maxHealth={maxHealth}");
    }

    public void SetFacing(bool isPlayerSide)
    {
        var spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
            spriteRenderer.sprite = isPlayerSide ? battleSpritePlayerSide : battleSpriteEnemySide;
    }

    /// Marks this pet as fully initialized (called by spawner after setting health)
    public void MarkInitialized()
    {
        isInitialized = true;
        Debug.Log($"[PetBattle] Pet marked as initialized. Health: {currentHealth}/{maxHealth}");
    }

    /// Apply damage locally on this client. Returns true if HP reaches 0.
    /// Call this via RPC so all clients update simultaneously.
    public bool ApplyDamage(int damage)
    {
        int oldHealth = currentHealth;
        currentHealth = Mathf.Max(0, currentHealth - Mathf.Max(0, damage));

        Debug.Log($"[PetBattle] Damage applied: {oldHealth} - {damage} = {currentHealth}");

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
            if (isInitialized)
            {
                stream.SendNext(currentHealth);
                stream.SendNext(maxHealth);
                Debug.Log($"[PetBattle] SENDING health data (initialized=true): currentHealth={currentHealth}, maxHealth={maxHealth}");
            }
            else
            {
                // Don't send during initialisation; let spawner take control
                stream.SendNext(-1);
                stream.SendNext(-1);
                Debug.Log($"[PetBattle] SKIPPING health send (not initialized yet)");
            }
        }
        else
        {
            // NON-OWNER receives health
            int receivedHealth = (int)stream.ReceiveNext();
            int receivedMaxHealth = (int)stream.ReceiveNext();

            if (receivedHealth == -1 || receivedMaxHealth == -1)
            {
                Debug.Log($"[PetBattle] RECEIVED sentinel values (initialization phase), ignoring");
                return;
            }

            // Only update if the values are valid
            if (receivedHealth >= 0 && receivedMaxHealth > 0 && receivedHealth <= receivedMaxHealth)
            {
                currentHealth = receivedHealth;
                maxHealth = receivedMaxHealth;

                Debug.Log($"[PetBattle] RECEIVED and ACCEPTED health data: currentHealth={currentHealth}, maxHealth={maxHealth}");

                // Update health bar if we have one
                if (healthBar != null)
                {
                    healthBar.SetMaxHealth(maxHealth);
                    healthBar.SetHealth(currentHealth);
                }
            }
            else
            {
                Debug.LogWarning($"[PetBattle] RECEIVED INVALID health data: receivedHealth={receivedHealth}, receivedMaxHealth={receivedMaxHealth}. Ignoring!");
            }
        }
    }
}