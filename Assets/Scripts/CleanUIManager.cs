// Assets/Scripts/CleanUIManager.cs
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class CleanUIManager : MonoBehaviour
{
    [Header("UI Refs")]
    public Canvas canvas;                  // PetCleanCanvas
    public PanelProgressBar panelProgress; // ProgressBar (with PanelProgressBar)
    public RectTransform petRect;          // PetImage RectTransform

    [Header("Celebration")]
    public PetEmotionFX petFX;             // Drag PetImage (with PetEmotionFX)
    public bool onlyCelebrateWhenFull = true;

    // Helper so drop zones can query fullness
    public bool IsFull() => panelProgress && panelProgress.IsFull;

    public void Clean(DraggableCleanItem item)
    {
        if (!item || IsFull()) return;
        StartCoroutine(CleanRoutine(item));
    }

    IEnumerator CleanRoutine(DraggableCleanItem item)
    {
        // Make a little "ghost" of the item that flies to the pet
        var itemRT = item.GetComponent<RectTransform>();

        var ghost = new GameObject("CleanGhost", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
        var gRT  = ghost.GetComponent<RectTransform>();
        var gImg = ghost.GetComponent<Image>();

        gRT.SetParent(canvas.transform, false);
        gRT.position = itemRT.position;
        gRT.sizeDelta = itemRT.sizeDelta;
        gImg.sprite = item.image.sprite;
        gImg.preserveAspect = true;

        // Fly animation
        float t = 0f, dur = 0.35f;
        Vector3 a = gRT.position;
        Vector3 b = petRect.position + new Vector3(0, petRect.rect.height * 0.1f, 0);
        while (t < dur)
        {
            t += Time.deltaTime;
            float k = t / dur;
            gRT.position   = Vector3.Lerp(a, b, k);
            gRT.localScale = Vector3.one * (1f - 0.2f * k);
            yield return null;
        }

        // Foam/bubble burst on impact
        yield return StartCoroutine(FoamBurst(gRT.position, gRT.sizeDelta * 0.25f));

        Destroy(ghost);
        item.gameObject.SetActive(false);

        // Raise progress
        float target = Mathf.Min(panelProgress.max, panelProgress.value + item.cleanPower);
        yield return StartCoroutine(panelProgress.AnimateTo(target, 0.3f));

        // 🎉 Celebrate (bounce + glow + sparkles)
        if (petFX && (!onlyCelebrateWhenFull || panelProgress.IsFull))
            petFX.PlayHappy();
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

            // Built-in sprite keeps it dependency-free
            img.sprite = Resources.GetBuiltinResource<Sprite>("UISprite.psd");
            img.type = Image.Type.Sliced;
            img.color = new Color(1f, 1f, 1f, 0.75f);

            StartCoroutine(RiseFade(rt, cg));
        }
        yield return new WaitForSeconds(0.25f);
    }

    IEnumerator RiseFade(RectTransform rt, CanvasGroup cg)
    {
        float t = 0f, dur = 0.45f;
        Vector3 a = rt.position, b = a + new Vector3(Random.Range(-15f, 15f), 60f, 0);
        while (t < dur)
        {
            t += Time.deltaTime;
            float k = t / dur;
            rt.position   = Vector3.Lerp(a, b, k);
            rt.localScale = Vector3.one * (1f + 0.3f * k);
            cg.alpha      = 1f - k;
            yield return null;
        }
        Destroy(rt.gameObject);
    }
}
