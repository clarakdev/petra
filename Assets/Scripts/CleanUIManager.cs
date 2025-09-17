using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class CleanUIManager : MonoBehaviour
{
    [Header("UI Refs")]
    public Canvas canvas;
    public PanelProgressBar cleanBar;    // only seeds global once
    public RectTransform petRect;

    [Header("Drain (local scene – leave 0)")]
    public float cleanDecayPerMinute = 0f; // not used; global owns decay
    public float fullPauseMinutes    = 20f; // legacy; global handles 100% pause

    [Header("FX (optional)")]
    public PetEmotionFX petFX;

    void Awake()
    {
        if (!canvas) canvas = GetComponentInParent<Canvas>();

        // Seed global once from the bar (safe no-op if already persisted/seeded)
        var mgr = PetNeedsManager.Instance;
        if (mgr != null && cleanBar != null)
            mgr.InitializeCleanIfUnset(cleanBar.value);
    }

    void Start()
    {
        if (petRect == null)
        {
            var petImage = FindFirstObjectByType<PetImage>();
            if (petImage != null)
            {
                petRect = petImage.RectTransform;

                var sel = PetSelectionManager.instance;
                if (petImage != null && sel != null && sel.currentPet != null && sel.currentPet.cardImage != null)
                    petImage.SetPet(sel.currentPet.cardImage);
            }
        }
        else
        {
            var petImage = petRect.GetComponent<PetImage>();
            var sel = PetSelectionManager.instance;
            if (petImage != null && sel != null && sel.currentPet != null && sel.currentPet.cardImage != null)
                petImage.SetPet(sel.currentPet.cardImage);
        }
    }

    public bool CanCleanNow()
    {
        var mgr = PetNeedsManager.Instance;
        // If global exists and is full, cannot clean. If global missing, allow (fallback path)
        return !(mgr != null && mgr.IsCleanFull());
    }

    public void Clean(DraggableCleanItem item)
    {
        if (item == null || canvas == null || petRect == null) return;
        if (!CanCleanNow()) return;

        StartCoroutine(CleanRoutine(item));
    }

    IEnumerator CleanRoutine(DraggableCleanItem item)
    {
        var itemRT = item.GetComponent<RectTransform>();
        if (!itemRT) yield break;

        // ghost fly-in
        var ghost  = new GameObject("CleanGhost", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
        var gRT    = ghost.GetComponent<RectTransform>();
        var gImg   = ghost.GetComponent<Image>();
        gRT.SetParent(canvas.transform, false);
        gRT.position  = itemRT.position;
        gRT.sizeDelta = itemRT.sizeDelta;
        gImg.sprite   = (item.image != null) ? item.image.sprite : null;
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

        // Global-first award (percent points)
        var mgr = PetNeedsManager.Instance;
        if (mgr != null)
        {
            if (!mgr.IsCleanFull())
                mgr.AddCleanPercent(item.cleanPower);   // cleanPower is % points
        }
        else if (cleanBar != null) // fallback if no global
        {
            cleanBar.SetValue(Mathf.Min(100f, cleanBar.value + item.cleanPower));
        }

        if (petFX) petFX.PlayHappy();
    }

    IEnumerator FoamBurst(Vector3 center, Vector2 size)
    {
        int n = 6;
        for (int i = 0; i < n; i++)
        {
            var bubble = new GameObject("bubble", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
            var rt  = bubble.GetComponent<RectTransform>();
            var img = bubble.GetComponent<Image>();
            var cg  = bubble.GetComponent<CanvasGroup>();
            rt.SetParent(canvas.transform, false);
            rt.position  = center + (Vector3)Random.insideUnitCircle * 10f;
            rt.sizeDelta = size * Random.Range(0.7f, 1.2f);
            img.sprite   = Resources.GetBuiltinResource<Sprite>("UISprite.psd");
            img.type     = Image.Type.Sliced;
            img.color    = new Color(1f,1f,1f,0.75f);
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
            if (cg) cg.alpha = 1f - k;
            yield return null;
        }
        Destroy(rt.gameObject);
    }
}
