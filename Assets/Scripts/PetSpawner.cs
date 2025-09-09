using UnityEngine;


public class PetSpawner : MonoBehaviour
{
    [SerializeField] private Canvas uiCanvas;

    private void Start()
    {
        var pet = PetSelectionManager.instance?.currentPet;
        if (pet == null || pet.prefab == null)
        {
            return;
        }

        if (uiCanvas == null)
        {
            uiCanvas = FindObjectOfType<Canvas>();
        }

        if (uiCanvas == null) {
            return;
        }

        var go = Instantiate(pet.prefab, uiCanvas.transform);
        var rt = go.GetComponent<RectTransform>();
        rt.anchoredPosition = Vector2.zero;   // center of canvas
        rt.localScale = Vector3.one;
    }
}