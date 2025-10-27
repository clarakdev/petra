using UnityEngine;

public class Purchaser : MonoBehaviour
{
    [SerializeField] private PlayerCurrency playerCurrency;

    [Header("Audio")]
    [SerializeField] private AudioClip purchaseClip; // NEW: sound to play on successful buy

    private void Awake()
    {
        if (!playerCurrency)
            playerCurrency = FindObjectOfType<PlayerCurrency>(); // auto-find
    }

    public int GetCurrentCurrency() => playerCurrency ? playerCurrency.currency : 0;

    public bool SpendCurrency(int amount)
    {
        if (!playerCurrency) return false;

        bool ok = playerCurrency.SpendCurrency(amount);
        if (ok)
        {
            // ✅ purchase actually went through
            if (SoundManager.Instance != null && purchaseClip != null)
            {
                SoundManager.Instance.PlaySFX(purchaseClip);
            }
        }

        return ok;
    }

    public void EarnCurrency(int amount)
    {
        if (!playerCurrency) return;
        playerCurrency.EarnCurrency(amount);
    }
}
