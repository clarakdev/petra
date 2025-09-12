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
        if (!manager || manager.IsFull()) return;
        var item = e.pointerDrag ? e.pointerDrag.GetComponent<DraggableCleanItem>() : null;
        if (item) manager.Clean(item);
    }
}
