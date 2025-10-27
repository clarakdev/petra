using Photon.Pun;
using TMPro;
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
    public TextMeshProUGUI healthText;

    public bool IsDead => currentHealth <= 0;

    private bool isInitialized = false;

    private void Awake()
    {
        Debug.Log($"[PetBattle] Awake: currentHealth={currentHealth}, maxHealth={maxHealth}");
    }

    private void Start()
    {
        Debug.Log($"[PetBattle] Start: currentHealth={currentHealth}, maxHealth={maxHealth}");
        UpdateHealthTextUI();
    }

    public void SetFacing(bool isPlayerSide)
    {
        var spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
            spriteRenderer.sprite = isPlayerSide ? battleSpritePlayerSide : battleSpriteEnemySide;
    }

    public void MarkInitialized()
    {
        isInitialized = true;
        Debug.Log($"[PetBattle] Pet marked as initialized. Health: {currentHealth}/{maxHealth}");
        UpdateHealthTextUI();
    }

    public bool ApplyDamage(int damage)
    {
        int oldHealth = currentHealth;
        currentHealth = Mathf.Max(0, currentHealth - Mathf.Max(0, damage));

        if (healthBar != null)
        {
            healthBar.SetHealth(currentHealth);
        }

        UpdateHealthTextUI();
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
        UpdateHealthTextUI();
    }

    public void Heal(int amount)
    {
        currentHealth = Mathf.Min(maxHealth, currentHealth + Mathf.Max(0, amount));
        if (healthBar != null)
            healthBar.SetHealth(currentHealth);

        UpdateHealthTextUI();
    }

    public void AssignHealthBar(HealthBar bar)
    {
        healthBar = bar;
        if (healthBar != null)
        {
            healthBar.SetMaxHealth(maxHealth);
            healthBar.SetHealth(currentHealth);
        }
        UpdateHealthTextUI();
    }

    public void AssignHealthText(TextMeshProUGUI textComponent)
    {
        healthText = textComponent;
        Debug.Log($"[PetBattle] *** ASSIGNED HEALTH TEXT to pet (IsMine={photonView.IsMine}), healthText is now: {(healthText != null ? "NOT NULL" : "NULL")}");
        UpdateHealthTextUI();
    }

    private void UpdateHealthTextUI()
    {
        if (healthText != null)
        {
            healthText.text = $"{currentHealth} / {maxHealth}";
        }
        else
        {
            Debug.LogWarning($"[PetBattle] *** WARNING: UpdateHealthTextUI called but healthText is NULL (IsMine={photonView.IsMine})");
        }
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            if (isInitialized)
            {
                stream.SendNext(currentHealth);
                stream.SendNext(maxHealth);
            }
            else
            {
                stream.SendNext(-1);
                stream.SendNext(-1);
            }
        }
        else
        {
            int receivedHealth = (int)stream.ReceiveNext();
            int receivedMaxHealth = (int)stream.ReceiveNext();

            Debug.Log($"[PetBattle] *** RECEIVED NETWORK UPDATE: health={receivedHealth}, maxHealth={receivedMaxHealth}, IsMine={photonView.IsMine}, healthText={(healthText != null ? "NOT NULL" : "NULL")}");

            if (receivedHealth == -1 || receivedMaxHealth == -1)
            {
                return;
            }

            if (receivedHealth >= 0 && receivedMaxHealth > 0 && receivedHealth <= receivedMaxHealth)
            {
                currentHealth = receivedHealth;
                maxHealth = receivedMaxHealth;

                if (healthBar != null)
                {
                    healthBar.SetMaxHealth(maxHealth);
                    healthBar.SetHealth(currentHealth);
                }

                UpdateHealthTextUI();
            }
        }
    }
}