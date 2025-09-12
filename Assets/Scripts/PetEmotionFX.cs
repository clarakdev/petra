using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PetEmotionFX : MonoBehaviour
{
    [Header("References")]
    public Image petImage;    
    public Canvas canvas;     

    [Header("Hearts (no glow)")]
    public int hearts = 10;
    public Vector2 heartSizeRange = new Vector2(48f, 72f);     
    public Color heartColor = new Color(1f, 0.5f, 0.7f, 0.95f);
    public Vector2 heartRise = new Vector2(30f, 130f);        
    public Vector2 heartDur  = new Vector2(0.6f, 1.1f);        

    [Header("Bounce")]
    public bool playBounce = true;
    public float bounceScale = 1.10f;     
    public float bounceUp = 0.22f;
    public float bounceDown = 0.22f;

    [Header("Optional Happy Pose")]
    public Sprite happySprite;            
    public bool revertToDefault = true;
    public float happySeconds = 1.0f;

    Sprite _defaultSprite;
    RectTransform _rt;

    void Awake()
    {
        if (!petImage) petImage = GetComponent<Image>();
        if (!canvas)   canvas   = GetComponentInParent<Canvas>();
        _rt = GetComponent<RectTransform>();
        if (petImage) _defaultSprite = petImage.sprite;
    }

    /// <summary>Call this to play the happy effect (bounce + big hearts).</summary>
    public void PlayHappy()
    {
        StopAllCoroutines();
        StartCoroutine(HappyRoutine());
    }

    IEnumerator HappyRoutine()
    {
        if (happySprite && petImage) petImage.sprite = happySprite;

        if (playBounce) StartCoroutine(Bounce());
        yield return StartCoroutine(HeartsBurst());

        yield return new WaitForSeconds(happySeconds * 0.5f);

        if (revertToDefault && _defaultSprite && petImage)
            petImage.sprite = _defaultSprite;
    }

    IEnumerator Bounce()
    {
        Vector3 a = Vector3.one;
        Vector3 b = new Vector3(bounceScale, bounceScale, 1f);

        float t = 0f;
        while (t < bounceUp)
        {
            t += Time.deltaTime;
            float k = Mathf.SmoothStep(0f, 1f, t / bounceUp);
            _rt.localScale = Vector3.Lerp(a, b, k);
            yield return null;
        }

        t = 0f;
        while (t < bounceDown)
        {
            t += Time.deltaTime;
            float k = Mathf.SmoothStep(0f, 1f, t / bounceDown);
            _rt.localScale = Vector3.Lerp(b, a, k);
            yield return null;
        }

        _rt.localScale = Vector3.one;
    }

    IEnumerator HeartsBurst()
    {
        if (!canvas) yield break;

        for (int i = 0; i < hearts; i++)
        {
            var go = new GameObject("heart", typeof(RectTransform), typeof(CanvasGroup), typeof(TextMeshProUGUI));
            var rt  = go.GetComponent<RectTransform>();
            var cg  = go.GetComponent<CanvasGroup>();
            var tmp = go.GetComponent<TextMeshProUGUI>();

            rt.SetParent(canvas.transform, false);
            rt.position = _rt.position + (Vector3)Random.insideUnitCircle * 12f;

            tmp.text = "♥";
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontSize = Random.Range(heartSizeRange.x, heartSizeRange.y); // BIG hearts
            tmp.color = heartColor;
            tmp.raycastTarget = false;

            float dur = Random.Range(heartDur.x, heartDur.y);
            Vector3 end = rt.position + new Vector3(
                Random.Range(-heartRise.x, heartRise.x),
                Random.Range(heartRise.x * 0.7f, heartRise.y),
                0f
            );

            rt.localEulerAngles = new Vector3(0, 0, Random.Range(-12f, 12f));

            StartCoroutine(RiseFadeTMP(rt, cg, dur, end));
        }

        yield return new WaitForSeconds(0.25f);
    }

    IEnumerator RiseFadeTMP(RectTransform rt, CanvasGroup cg, float dur, Vector3 endPos)
    {
        float t = 0f;
        Vector3 start = rt.position;
        while (t < dur)
        {
            t += Time.deltaTime;
            float k = t / dur;
            rt.position   = Vector3.Lerp(start, endPos, k);
            rt.localScale = Vector3.one * (1f + 0.25f * k);
            cg.alpha      = 1f - k;
            yield return null;
        }
        Destroy(rt.gameObject);
    }
}

