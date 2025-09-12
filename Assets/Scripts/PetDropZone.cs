using UnityEngine;
using UnityEngine.EventSystems;

public class PetDropZone : MonoBehaviour, IDropHandler
{
    public FeedUIManager manager;

    void Awake()
    {
        if (!manager) manager = FindFirstObjectByType<FeedUIManager>();
    }

    public void OnDrop(PointerEventData e)
    {
        if (!manager || !manager.CanFeed()) return;
        var dragged = e.pointerDrag ? e.pointerDrag.GetComponent<DraggableFood>() : null;
        if (dragged) manager.Feed(dragged);
    }
}
