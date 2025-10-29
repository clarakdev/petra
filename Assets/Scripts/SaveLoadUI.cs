using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SaveLoadUI : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Assign your Save button here")]
    public Button saveButton;

    [Tooltip("Optional: Text to show save feedback")]
    public TextMeshProUGUI feedbackText;

    [Header("Feedback Settings")]
    public float feedbackDuration = 2f;

    private void Start()
    {
        // Hook up the save button
        if (saveButton != null)
        {
            saveButton.onClick.AddListener(OnSaveButtonClicked);
        }
        else
        {
            Debug.LogWarning("[SaveLoadUI] Save button not assigned!");
        }

        // Hide feedback text initially
        if (feedbackText != null)
        {
            feedbackText.gameObject.SetActive(false);
        }
    }

    private void OnSaveButtonClicked()
    {
        if (SaveSystem.Instance != null)
        {
            SaveSystem.Instance.SaveGame();
            ShowFeedback("Game Saved!");
        }
        else
        {
            Debug.LogError("[SaveLoadUI] SaveSystem instance not found!");
            ShowFeedback("Save Failed!");
        }
    }

    private void ShowFeedback(string message)
    {
        if (feedbackText != null)
        {
            feedbackText.text = message;
            feedbackText.gameObject.SetActive(true);
            CancelInvoke(nameof(HideFeedback));
            Invoke(nameof(HideFeedback), feedbackDuration);
        }
    }

    private void HideFeedback()
    {
        if (feedbackText != null)
        {
            feedbackText.gameObject.SetActive(false);
        }
    }

    // Optional: Add this method if you want a "New Game" button that deletes the save
    public void OnNewGameButtonClicked()
    {
        if (SaveSystem.Instance != null)
        {
            SaveSystem.Instance.DeleteSave();
            ShowFeedback("Save Deleted - Starting Fresh!");
        }
    }
}