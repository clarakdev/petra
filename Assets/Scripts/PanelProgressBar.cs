using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class PanelProgressBar : MonoBehaviour
{
    public RectTransform fill;
    public TMP_Text label;
    public float max = 100f;
    [Range(0,100f)] public float value = 60f;
    public float padding = 6f;


    [Header("Colors")]
    public Color fillColorNormal = new Color(0.69f, 0.53f, 1f, 1.00f);
    public Color fillColorFull   = new Color(0.69f, 0.53f, 1f, 1.00f);


    RectTransform container;
    Image fillImage;

    public bool IsFull => value >= max - 0.01f;

    void Awake()
    {
        container = GetComponent<RectTransform>();
        if (fill) fillImage = fill.GetComponent<Image>();
        UpdateVisuals();
    }

    public void SetValue(float v)
    {
        value = Mathf.Clamp(v, 0f, max);
        UpdateVisuals();
    }

    public IEnumerator AnimateTo(float target, float dur)
    {
        float start = value;
        float t = 0f;
        while (t < dur)
        {
            t += Time.deltaTime;
            SetValue(Mathf.Lerp(start, target, t / dur));
            yield return null;
        }
        SetValue(target);
    }

    void UpdateVisuals()
    {
        if (!container || !fill) return;

        float pct = (max > 0f) ? value / max : 0f;

        float barWidth = Mathf.Max(0f, container.rect.width - padding * 2f);

        fill.anchorMin = new Vector2(0f, 0f);
        fill.anchorMax = new Vector2(0f, 1f);
        fill.offsetMin = new Vector2(padding, padding);
        fill.offsetMax = new Vector2(padding + barWidth * pct, -padding);

        if (fillImage) fillImage.color = IsFull ? fillColorFull : fillColorNormal;

        if (label) label.text = Mathf.RoundToInt(pct * 100f) + "%";
    }

    void OnRectTransformDimensionsChange()
    {
        if (container) UpdateVisuals();
    }
}
