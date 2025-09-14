using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class FeedUIManager : MonoBehaviour
{
    public Canvas canvas;
<<<<<<< Updated upstream
    public PanelProgressBar panelProgress;
    public RectTransform petRect;

    public bool CanFeed() => panelProgress != null && !panelProgress.IsFull;

    void Start()
    {
        // Auto-bind petRect & sprite if not wired in the Inspector
=======
    public PanelProgressBar hungerBar;
    public RectTransform petRect;     

    [Header("FX (optional)")]
    public PetEmotionFX petFX;

    [Header("Tuning")]
    public float hungerDecayPerMinute = 0.5f;  
    public float fullPauseMinutes     = 20f;   

    float _current;               
    bool  _isConsuming = false;   
    bool  _animating   = false;   
    float _decayResumeTime = -1f;

    void Awake()
    {
        if (canvas == null) canvas = GetComponentInParent<Canvas>();
        _current = hungerBar ? hungerBar.value : 0f;
        if (hungerBar) hungerBar.SetValue(_current);
    }

    void Start()
    {
>>>>>>> Stashed changes
        if (petRect == null)
        {
            var petImage = FindFirstObjectByType<PetFeedingImage>();
            if (petImage != null)
            {
                petRect = petImage.GetComponent<RectTransform>();

                var mgr = PetSelectionManager.instance;
                if (mgr != null && mgr.currentPet != null && mgr.currentPet.cardImage != null)
                    petImage.SetPet(mgr.currentPet.cardImage);
            }
        }
        else
        {
            var petImage = petRect.GetComponent<PetFeedingImage>();
            var mgr = PetSelectionManager.instance;
            if (petImage != null && mgr != null && mgr.currentPet != null && mgr.currentPet.cardImage != null)
                petImage.SetPet(mgr.currentPet.cardImage);
        }
    }

    public void Feed(DraggableFood food)
    {
        if (!CanFeed() || food == null) return;
        StartCoroutine(EatRoutine(food));
    }

    IEnumerator EatRoutine(DraggableFood food)
    {
        var foodRT = food.GetComponent<RectTransform>();

        var ghost = new GameObject("FoodGhost", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
        var ghostRT = ghost.GetComponent<RectTransform>();
        var ghostImg = ghost.GetComponent<Image>();
        ghostRT.SetParent(canvas.transform, false);
        ghostRT.position = foodRT.position;
        ghostRT.sizeDelta = foodRT.sizeDelta;
        ghostImg.sprite = food.image.sprite;
        ghostImg.preserveAspect = true;

        float t = 0f, dur = 0.35f;
        Vector3 a = ghostRT.position;
        Vector3 b = petRect.position + new Vector3(0, petRect.rect.height * 0.1f, 0);
        while (t < dur)
        {
            t += Time.deltaTime;
            float k = t / dur;
            ghostRT.position = Vector3.Lerp(a, b, k);
            ghostRT.localScale = Vector3.one * (1f - 0.2f * k);
            yield return null;
        }

        yield return StartCoroutine(BiteBurst(ghostImg, ghostRT));
        Destroy(ghost);
        food.gameObject.SetActive(false);

        float target = Mathf.Min(panelProgress.max, panelProgress.value + food.nutrition);
        yield return StartCoroutine(panelProgress.AnimateTo(target, 0.3f));
    }

    IEnumerator BiteBurst(Image srcImg, RectTransform at)
    {
        int n = 5;
        for (int i = 0; i < n; i++)
        {
            var bit = new GameObject("bite", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
            var rt = bit.GetComponent<RectTransform>();
            var img = bit.GetComponent<Image>();
            var cg = bit.GetComponent<CanvasGroup>();
            rt.SetParent(canvas.transform, false);
            rt.position = at.position + (Vector3)Random.insideUnitCircle * 10f;
            rt.sizeDelta = at.sizeDelta * 0.2f;
            img.sprite = srcImg.sprite;
            img.preserveAspect = true;
            StartCoroutine(FallAndFade(rt, cg));
        }
        yield return new WaitForSeconds(0.25f);
    }

    IEnumerator FallAndFade(RectTransform rt, CanvasGroup cg)
    {
        float t = 0f, dur = 0.35f;
        Vector3 a = rt.position, b = a + new Vector3(Random.Range(-20f, 20f), -60f, 0);
        while (t < dur)
        {
            t += Time.deltaTime;
            float k = t / dur;
            rt.position = Vector3.Lerp(a, b, k);
            rt.localScale = Vector3.one * (1f - 0.7f * k);
            cg.alpha = 1f - k;
            yield return null;
        }
        Destroy(rt.gameObject);
    }
}
