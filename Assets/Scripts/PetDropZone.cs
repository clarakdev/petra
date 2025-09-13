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
        var dragged = e.pointerDrag;
        if (!dragged || !manager) return;

        var food = dragged.GetComponent<DraggableFood>();
        if (!food) return;

        if (!manager.CanFeedNow()) return;
        manager.Feed(food);
    }
}
