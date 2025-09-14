using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class FeedUIManager : MonoBehaviour
{
    [Header("UI Refs")]
    public Canvas canvas;
    public PanelProgressBar hungerBar;
    public RectTransform petRect;      // auto-bound in Start if left empty

    [Header("FX (optional)")]
    public PetEmotionFX petFX;

    [Header("Tuning")]
    public float hungerDecayPerMinute = 0.5f;  // try 6.0 while testing
    public float fullPauseMinutes     = 20f;   // pause decay after hitting 100%

    float _current;               // 0..100
    bool  _isConsuming = false;   // blocks a second feed while animating
    bool  _animating   = false;   // blocks Update while AnimateTo runs
    float _decayResumeTime = -1f;

    void Awake()
    {
        if (canvas == null) canvas = GetComponentInParent<Canvas>();
        _current = hungerBar ? hungerBar.value : 0f;
        if (hungerBar) hungerBar.SetValue(_current);
    }

    void Start()
    {
        // Auto-bind the pet image/rect and make sure the selected pet sprite is shown.
        if (petRect == null)
        {
            var petImg = FindFirstObjectByType<PetImage>();
            if (petImg != null)
            {
                petRect = petImg.RectTransform;

                var mgr = PetSelectionManager.instance;
                if (mgr != null && mgr.currentPet != null && mgr.currentPet.cardImage != null)
                    petImg.SetPet(mgr.currentPet.cardImage);
            }
        }
        else
        {
            var petImg = petRect.GetComponent<PetImage>();
            var mgr = PetSelectionManager.instance;
            if (petImg != null && mgr != null && mgr.currentPet != null && mgr.currentPet.cardImage != null)
                petImg.SetPet(mgr.currentPet.cardImage);
        }
    }

    void Update()
    {
        if (_animating || hungerBar == null) return;

        bool decayPaused = IsFull() && Time.unscaledTime < _decayResumeTime;
        if (!decayPaused && hungerDecayPerMinute > 0f && _current > 0f)
        {
            float perSecond = hungerDecayPerMinute / 60f;
            _current = Mathf.Max(0f, _current - perSecond * Time.unscaledDeltaTime);
            hungerBar.SetValue(_current);
        }
    }

    public bool IsFull() => _current >= 99.999f;

    // Also blocks while the last item is being consumed
    public bool CanFeedNow() => !_isConsuming && !IsFull();

    public void Feed(DraggableFood food)
    {
        if (!food || !canvas || !hungerBar || petRect == null) return;
        if (!CanFeedNow()) return;
        StartCoroutine(EatRoutine(food));
    }

    IEnumerator EatRoutine(DraggableFood food)
    {
        _isConsuming = true;

        var foodRT = food.GetComponent<RectTransform>();
        if (foodRT == null) { _isConsuming = false; yield break; }

        // ghost that flies to the pet
        var ghost = new GameObject("FoodGhost", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
        var gRT  = ghost.GetComponent<RectTransform>();
        var gImg = ghost.GetComponent<Image>();
        gRT.SetParent(canvas.transform, false);
        gRT.SetAsLastSibling();
        gRT.position  = foodRT.position;
        gRT.sizeDelta = foodRT.sizeDelta;
        gImg.sprite = (food.image != null) ? food.image.sprite : null;
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

        yield return StartCoroutine(BiteBurst(gImg, gRT));
        Destroy(ghost);
        food.gameObject.SetActive(false);

        // increase fullness
        _current = Mathf.Min(100f, _current + food.nutrition);

        // animate to the new value
        _animating = true;
        yield return StartCoroutine(hungerBar.AnimateTo(_current, 0.25f));
        _animating = false;

        // reached full → start pause window & block further feeding until below 100
        if (IsFull())
        {
            _current = 100f;
            hungerBar.SetValue(_current);
            _decayResumeTime = Time.unscaledTime + fullPauseMinutes * 60f;
        }

        if (petFX) petFX.PlayHappy();

        _isConsuming = false;
    }

    IEnumerator BiteBurst(Image srcImg, RectTransform at)
    {
        const int n = 5;
        for (int i = 0; i < n; i++)
        {
            var bit = new GameObject("bite", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
            var rt = bit.GetComponent<RectTransform>();
            var img = bit.GetComponent<Image>();
            var cg = bit.GetComponent<CanvasGroup>();
            rt.SetParent(canvas.transform, false);
            rt.SetAsLastSibling();
            rt.position  = at.position + (Vector3)Random.insideUnitCircle * 10f;
            rt.sizeDelta = at.sizeDelta * 0.2f;
            img.sprite   = srcImg ? srcImg.sprite : null;
            img.preserveAspect = true;
            StartCoroutine(FallAndFade(rt, cg));
        }
        yield return new WaitForSecondsRealtime(0.25f);
    }

    IEnumerator FallAndFade(RectTransform rt, CanvasGroup cg)
    {
        float t = 0f, dur = 0.35f;
        Vector3 a = rt.position, b = a + new Vector3(Random.Range(-20f, 20f), -60f, 0);
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float k = t / dur;
            rt.position   = Vector3.Lerp(a, b, k);
            rt.localScale = Vector3.one * (1f - 0.7f * k);
            if (cg) cg.alpha = 1f - k;
            yield return null;
        }
        Destroy(rt.gameObject);
    }
}
