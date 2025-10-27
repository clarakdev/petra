using UnityEngine;
using UnityEngine.Events;

/// Minimal fetch UI driver (PanelProgressBar only).
/// Keeps a PanelProgressBar in sync and exposes helpers to set/add progress.
/// Now also awards coins when reaching 100%.
[DisallowMultipleComponent]
public class FetchUIManager : MonoBehaviour
{
    [Header("UI")]
    public PanelProgressBar panelBar;   // 0..100 bar (custom)

    [Header("Events")]
    public UnityEvent<float> OnProgress; // emits 0..100 when changed via helpers

    [Header("Rewards")]
    public int coinsForFullFetch = 100; // coins awarded once at 100%
    private bool _paidForThisFull = false;

    void Awake() => PushToUI(ReadPercent());

    void Update()
    {
        float pct = ReadPercent();
        PushToUI(pct);
        CheckReward(pct);
    }

    // ===== Public helpers you can call from gameplay =====

    /// Add percent points to the bar (e.g., bump UI when something happens).
    public void AddProgressPercent(float percent)
    {
        if (Mathf.Approximately(percent, 0f) || panelBar == null) return;
        float next = Mathf.Clamp(ReadPercent() + percent, 0f, 100f);
        WritePercent(next);
        OnProgress?.Invoke(next);
        CheckReward(next);
    }

    /// Use this if some other system sets an absolute % (0..100).
    public void SetFromPanel(float percent)
    {
        float next = Mathf.Clamp(percent, 0f, 100f);
        WritePercent(next);
        CheckReward(next);
    }

    // ===== Reward system =====
    private void CheckReward(float percent)
    {
        // Re-arm reward if progress dropped below 100%
        if (percent < 100f - 0.01f)
        {
            _paidForThisFull = false;
            return;
        }

        // Pay once when bar reaches full
        if (!_paidForThisFull && percent >= 100f - 0.01f)
        {
            var wallet = FindFirstObjectByType<PlayerCurrency>();
            if (wallet != null)
            {
                wallet.EarnCurrency(coinsForFullFetch);
                Debug.Log($"[FetchUIManager] Fetch reached 100%. Awarded {coinsForFullFetch} coins. Total: {wallet.currency}");
            }
            _paidForThisFull = true;
        }
    }

    // ===== Internals =====

    float ReadPercent()
    {
        if (panelBar == null) return 0f;
        return Mathf.Clamp(panelBar.value, 0f, 100f);
    }

    void WritePercent(float pct)
    {
        if (panelBar != null) panelBar.SetValue(pct);
    }

    void PushToUI(float pct)
    {
        if (panelBar != null) panelBar.SetValue(pct);
    }
}
