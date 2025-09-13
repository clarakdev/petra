using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(CanvasGroup))]
[RequireComponent(typeof(Image))]
public class DraggableCleanItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public int cleanPower = 50;
    public Image image;
    public Canvas canvas;

    RectTransform rt;
    CanvasGroup cg;
    Vector2 startPos;

    void Awake()
    {
        rt = GetComponent<RectTransform>();
        cg = GetComponent<CanvasGroup>();
        if (!image) image = GetComponent<Image>();
        if (!canvas) canvas = GetComponentInParent<Canvas>();
        image.raycastTarget = true;
    }

    public void OnBeginDrag(PointerEventData e)
    {
        if (!canvas || !cg) return;
        startPos = rt.anchoredPosition;
        cg.blocksRaycasts = false;
        cg.alpha = 0.9f;
    }

    public void OnDrag(PointerEventData e)
    {
        if (!canvas) return;
        rt.anchoredPosition += e.delta / canvas.scaleFactor;
    }

    public void OnEndDrag(PointerEventData e)
    {
        if (!cg) return;
        cg.blocksRaycasts = true;
        cg.alpha = 1f;
        rt.anchoredPosition = startPos;
    }
}
