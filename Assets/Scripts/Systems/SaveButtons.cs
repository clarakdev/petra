using UnityEngine;

public class SaveButtons : MonoBehaviour
{
    public void OnSaveClicked()
    {
        if (GameState.Instance != null)
        {
            GameState.Instance.SaveNow();
            
            Debug.Log("[SaveButtons] Manual save done.");
            FindObjectOfType<SaveStatusUI>()?.UpdateLastSavedText();

        }
        else
        {
            Debug.LogWarning("[SaveButtons] GameState not found!");
        }
    }

    public void OnLoadClicked()
    {
        if (GameState.Instance != null)
        {
            GameState.Instance.LoadNow();
            Debug.Log("[SaveButtons] Manual load done.");
        }
        else
        {
            Debug.LogWarning("[SaveButtons] GameState not found!");
        }
    }
}
