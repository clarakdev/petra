using UnityEngine;
using UnityEngine.Events;

/// Minimal fetch UI driver (PanelProgressBar only).
/// Keeps a PanelProgressBar in sync and exposes helpers to set/add progress.
[DisallowMultipleComponent]
public class FetchUIManager : MonoBehaviour
{
    [Header("UI")]
    public PanelProgressBar panelBar;   // 0..100 bar (custom)

    [Header("Events")]
    public UnityEvent<float> OnProgress; // emits 0..100 when changed via helpers

    void Awake() => PushToUI(ReadPercent());

    void Update()
    {
        // PanelProgressBar is the single source of truth.
        PushToUI(ReadPercent());
    }

    // ===== Public helpers you can call from gameplay =====

    /// Add percent points to the bar (e.g., bump UI when something happens).
    public void AddProgressPercent(float percent)
    {
        if (Mathf.Approximately(percent, 0f) || panelBar == null) return;
        float next = Mathf.Clamp(ReadPercent() + percent, 0f, 100f);
        WritePercent(next);
        OnProgress?.Invoke(next);
    }

    /// Use this if some other system sets an absolute % (0..100).
    public void SetFromPanel(float percent) => WritePercent(Mathf.Clamp(percent, 0f, 100f));

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
