using UnityEngine;
using UnityEngine.EventSystems;

public class CleanDropZone : MonoBehaviour, IDropHandler
{
    public CleanUIManager manager;

    void Awake()
    {
        if (!manager) manager = FindFirstObjectByType<CleanUIManager>();
    }

    public void OnDrop(PointerEventData e)
    {
        var dragged = e.pointerDrag;
        if (!dragged || !manager) return;

        var item = dragged.GetComponent<DraggableCleanItem>();
        if (!item) return;

        if (!manager.CanCleanNow()) return;
        manager.Clean(item);
    }
}
