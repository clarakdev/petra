using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class CleanUIManager : MonoBehaviour
{
    [Header("UI Refs")]
    public Canvas canvas;
    public PanelProgressBar cleanBar;
    public RectTransform petRect;

    [Header("FX (optional)")]
    public PetEmotionFX petFX;

    [Header("Tuning")]
    public float cleanDecayPerMinute = 0.3f;
    public float fullPauseMinutes = 20f;

    float _current;
    bool  _isCleaning = false;
    bool  _animating  = false;
    float _decayResumeTime = -1f;

    void Awake()
    {
        _current = cleanBar ? cleanBar.value : 0f;
        if (cleanBar) cleanBar.SetValue(_current);
    }

    void Update()
    {
        if (_animating || cleanBar == null) return;

        bool decayPaused = IsFull() && Time.unscaledTime < _decayResumeTime;
        if (!decayPaused && cleanDecayPerMinute > 0f && _current > 0f)
        {
            float perSecond = cleanDecayPerMinute / 60f;
            _current = Mathf.Max(0f, _current - perSecond * Time.unscaledDeltaTime);
            cleanBar.SetValue(_current);
        }
    }

    public bool IsFull() => _current >= 99.999f;
    public bool CanCleanNow() => !_isCleaning && !IsFull();

    public void Clean(DraggableCleanItem item)
    {
        if (!item || !canvas || !cleanBar) return;
        if (!CanCleanNow()) return;
        StartCoroutine(CleanRoutine(item));
    }

    IEnumerator CleanRoutine(DraggableCleanItem item)
    {
        _isCleaning = true;

        var itemRT = item.GetComponent<RectTransform>();

        var ghost = new GameObject("CleanGhost", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
        var gRT = ghost.GetComponent<RectTransform>();
        var gImg = ghost.GetComponent<Image>();
        gRT.SetParent(canvas.transform, false);
        gRT.position = itemRT.position;
        gRT.sizeDelta = itemRT.sizeDelta;
        gImg.sprite = item.image.sprite;
        gImg.preserveAspect = true;

        float t = 0f, dur = 0.35f;
        Vector3 a = gRT.position;
        Vector3 b = petRect.position + new Vector3(0, petRect.rect.height * 0.1f, 0);
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float k = t / dur;
            gRT.position   = Vector3.Lerp(a, b, k);
            gRT.localScale = Vector3.one * (1f - 0.2f * k);
            yield return null;
        }

        yield return StartCoroutine(FoamBurst(gRT.position, gRT.sizeDelta * 0.25f));
        Destroy(ghost);
        item.gameObject.SetActive(false);

        _current = Mathf.Min(100f, _current + item.cleanPower);

        _animating = true;
        yield return StartCoroutine(cleanBar.AnimateTo(_current, 0.25f));
        _animating = false;

        if (IsFull())
        {
            _current = 100f;
            cleanBar.SetValue(_current);
            _decayResumeTime = Time.unscaledTime + fullPauseMinutes * 60f;
        }

        if (petFX) petFX.PlayHappy();
        _isCleaning = false;
    }

    IEnumerator FoamBurst(Vector3 center, Vector2 size)
    {
        int n = 6;
        for (int i = 0; i < n; i++)
        {
            var bubble = new GameObject("bubble", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
            var rt = bubble.GetComponent<RectTransform>();
            var img = bubble.GetComponent<Image>();
            var cg = bubble.GetComponent<CanvasGroup>();

            rt.SetParent(canvas.transform, false);
            rt.position = center + (Vector3)Random.insideUnitCircle * 10f;
            rt.sizeDelta = size * Random.Range(0.7f, 1.2f);
            img.sprite = Resources.GetBuiltinResource<Sprite>("UISprite.psd");
            img.type = Image.Type.Sliced;
            img.color = new Color(1f, 1f, 1f, 0.75f);
            StartCoroutine(RiseFade(rt, cg));
        }
        yield return new WaitForSecondsRealtime(0.25f);
    }

    IEnumerator RiseFade(RectTransform rt, CanvasGroup cg)
    {
        float t = 0f, dur = 0.45f;
        Vector3 a = rt.position, b = a + new Vector3(Random.Range(-15f, 15f), 60f, 0);
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float k = t / dur;
            rt.position   = Vector3.Lerp(a, b, k);
            rt.localScale = Vector3.one * (1f + 0.3f * k);
            cg.alpha      = 1f - k;
            yield return null;
        }
        Destroy(rt.gameObject);
    }
}
