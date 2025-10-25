using TMPro;
using UnityEngine;

public class SaveStatusUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI lastSavedText;

    private void Start()
    {
        UpdateLastSavedText();
    }

    public void UpdateLastSavedText()
    {
        var data = SaveSystem.Load();
        if (data != null && !string.IsNullOrEmpty(data.lastSavedUtc))
        {
            lastSavedText.text = $"Last saved: {data.lastSavedUtc}";
        }
        else
        {
            lastSavedText.text = "Last saved: —";
        }
    }
}
