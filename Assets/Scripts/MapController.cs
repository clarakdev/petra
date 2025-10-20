using UnityEngine;
using UnityEngine.EventSystems;

public class MapController : MonoBehaviour
{
    public GameObject mapPanel;

    void Start()
    {
        mapPanel.SetActive(false);
    }

    public void ToggleMap()
    {
        bool isActive = mapPanel.activeSelf;
        mapPanel.SetActive(!isActive);
        Time.timeScale = isActive ? 1f : 0f;
    }

    public void CloseMap()
    {
        mapPanel.SetActive(false);
        Time.timeScale = 1f;
        
            // Reactivate EventSystem so buttons always work
        EventSystem.current.SetSelectedGameObject(null);
        EventSystem.current.enabled = false;
        EventSystem.current.enabled = true;
    }
}
