using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

/// Minimal fetch UI driver (no awards, no completion toast).
/// Keeps a PanelProgressBar or Slider in sync and exposes helpers to set/add progress.
public class FetchUIManager : MonoBehaviour
{
    [Header("UI (pick one)")]
    public PanelProgressBar panelBar;   // 0..100 bar (custom)
    public Slider slider;               // OR Unity Slider (0..1)

    [Header("Events")]
    public UnityEvent<float> OnProgress; // emits 0..100 when changed via helpers

    const float EPS = 0.0001f;

    void Awake() => PushToUI(ReadPercent());

    void Update()
    {
        // If both are present, let PanelProgressBar be the source of truth.
        // Otherwise just keep whichever exists as-is.
        PushToUI(ReadPercent());
    }

    // ===== Public helpers you can call from gameplay (optional) =====

    /// Add percent points to the progress bar (e.g., if you want to bump UI when something happens).
    public void AddProgressPercent(float percent)
    {
        if (Mathf.Approximately(percent, 0f)) return;
        float next = Mathf.Clamp(ReadPercent() + percent, 0f, 100f);
        WritePercent(next);
        OnProgress?.Invoke(next);
    }

    /// Wire this to a Unity Slider (0..1) OnValueChanged (Dynamic float) if you use a Slider.
    public void SetFromSlider(float normalized) => WritePercent(Mathf.Clamp01(normalized) * 100f);

    /// Wire this to a PanelProgressBar-like callback that gives 0..100.
    public void SetFromPanel(float percent) => WritePercent(Mathf.Clamp(percent, 0f, 100f));

    // ===== Internals =====

    float ReadPercent()
    {
        if (panelBar != null) return Mathf.Clamp(panelBar.value, 0f, 100f);
        if (slider   != null) return Mathf.Clamp01(slider.value) * 100f;
        return 0f;
    }

    void WritePercent(float pct)
    {
        if (panelBar != null) panelBar.SetValue(pct);
        if (slider   != null) slider.value = pct / 100f;
    }

    void PushToUI(float pct)
    {
        if (panelBar != null) panelBar.SetValue(pct);
        if (slider   != null) slider.value = pct / 100f;
    }
}
