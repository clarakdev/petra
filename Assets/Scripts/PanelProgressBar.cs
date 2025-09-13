using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class PanelProgressBar : MonoBehaviour
{
    [Header("Wiring")]
    public RectTransform fill;    // ← drag the Fill child here
    public TMP_Text label;        // ← drag PercentText here

    [Header("Range & Value")]
    public float max = 100f;
    [Range(0,100f)] public float value = 60f;
    public float padding = 6f;

    [Header("Colors")]
    public Color fillColorNormal = new Color(0.69f, 0.53f, 1f, 1.00f);
    public Color fillColorFull   = new Color(0.69f, 0.53f, 1f, 1.00f);

    [Header("Label")]
    [Tooltip("Number of decimals to display in the percent text.")]
    [Range(0,3)] public int labelDecimals = 1;

    RectTransform container;
    Image fillImage;

    public bool IsFull => value >= max - 0.01f;

    void Awake()
    {
        container = GetComponent<RectTransform>();
        if (fill) fillImage = fill.GetComponent<Image>();
        Refresh();
    }

    // --- Public API ---------------------------------------------------------

    public void SetValue(float v)
    {
        value = Mathf.Clamp(v, 0f, max);
        Refresh();
    }

    public void SetMax(float newMax, bool clampCurrent = true)
    {
        max = Mathf.Max(0.0001f, newMax);
        if (clampCurrent) value = Mathf.Clamp(value, 0f, max);
        Refresh();
    }

    public void SetColors(Color normal, Color full)
    {
        fillColorNormal = normal;
        fillColorFull = full;
        Refresh();
    }

    /// <summary>Animate value over time using unscaled time.</summary>
    public IEnumerator AnimateTo(float target, float dur)
    {
        float start = value;
        float t = 0f;
        target = Mathf.Clamp(target, 0f, max);

        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            SetValue(Mathf.Lerp(start, target, Mathf.Clamp01(t / dur)));
            yield return null;
        }
        SetValue(target);
    }

    // --- Internals ----------------------------------------------------------

    public void Refresh() => UpdateVisuals();

    void UpdateVisuals()
    {
        if (!container || !fill) return;

        float pct = (max > 0f) ? value / max : 0f;

        // Calculate width
        float barWidth = Mathf.Max(0f, container.rect.width - padding * 2f);

        // Stretch fill horizontally from left, with padding
        fill.anchorMin = new Vector2(0f, 0f);
        fill.anchorMax = new Vector2(0f, 1f);
        fill.offsetMin = new Vector2(padding, padding);
        fill.offsetMax = new Vector2(padding + barWidth * pct, -padding);

        // Color state
        if (fillImage) fillImage.color = IsFull ? fillColorFull : fillColorNormal;

        // Label (one decimal by default so you can see slow decay)
        if (label)
        {
            float percent = pct * 100f;
            string fmt = (labelDecimals <= 0) ? "0" : "0." + new string('#', labelDecimals);
            label.text = percent.ToString(fmt) + "%";
        }
    }

    void OnRectTransformDimensionsChange()
    {
        if (container) UpdateVisuals();
    }
}
