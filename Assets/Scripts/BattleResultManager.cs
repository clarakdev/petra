using Photon.Pun;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class BattleResultManager : MonoBehaviourPun
{
    [Header("UI References")]
    [SerializeField] private GameObject resultPanel;
    [SerializeField] private TextMeshProUGUI resultText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private Button returnButton;

    [Header("Audio (Optional)")]
    [SerializeField] private AudioClip victorySound;
    [SerializeField] private AudioClip defeatSound;
    private AudioSource audioSource;

    [Header("Settings")]
    [SerializeField] private float delayBeforeReturn = 5f;
    [SerializeField] private string battleRoomSceneName = "BattleRoom";

    private bool resultShown = false;

    private void Awake()
    {
        if (resultPanel != null)
        {
            resultPanel.SetActive(false);
        }

        if (returnButton != null)
        {
            returnButton.onClick.AddListener(OnReturnButtonClicked);
        }

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null && (victorySound != null || defeatSound != null))
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }

    public void ShowResult(bool didIWin)
    {
        if (resultShown) return;
        resultShown = true;

        if (resultPanel != null)
            resultPanel.SetActive(true);

        if (resultText != null)
        {
            resultText.text = didIWin ? "VICTORY!" : "DEFEAT!";
            resultText.color = didIWin ? new Color(1f, 0.84f, 0f) : Color.red;
        }

        if (descriptionText != null)
        {
            descriptionText.text = didIWin
                ? "You defeated your opponent!"
                : "Your pet has fainted!";
        }

        // 🔊 Play win/lose sound
        if (audioSource != null)
        {
            AudioClip clipToPlay = didIWin ? victorySound : defeatSound;
            if (clipToPlay != null)
                audioSource.PlayOneShot(clipToPlay);
        }

        // 💰 Give rewards
        GiveBattleReward(didIWin);

        StartCoroutine(AutoReturnAfterDelay());
    }

    private void GiveBattleReward(bool playerWon)
    {
        var wallet = FindFirstObjectByType<PlayerCurrency>();
        if (wallet == null)
        {
            Debug.LogWarning("[BattleResultManager] No PlayerCurrency found — reward skipped.");
            return;
        }

        int reward = playerWon ? 200 : 100;
        wallet.EarnCurrency(reward);

        Debug.Log($"[BattleResultManager] Battle complete. Player {(playerWon ? "WON" : "LOST")} → +{reward} coins (Total: {wallet.currency}).");

        // Optional: show toast or notifier if you have one
        var notifier = GlobalNotifier.Instance;
        if (notifier != null)
        {
            string msg = playerWon
                ? $"+{reward} PetraCoins for winning the battle!"
                : $"+{reward} PetraCoins for participating!";
            notifier.ShowToast(msg, 2.5f);
        }

        // Auto-save progress (optional)
        var gameState = FindFirstObjectByType<GameState>();
        if (gameState != null)
        {
            gameState.SaveNow();
        }
    }

    private IEnumerator AutoReturnAfterDelay()
    {
        yield return new WaitForSeconds(delayBeforeReturn);
        ReturnToBattleRoom();
    }

    private void OnReturnButtonClicked()
    {
        ReturnToBattleRoom();
    }

    private void ReturnToBattleRoom()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.LoadLevel(battleRoomSceneName);
        }
    }

    private void OnDestroy()
    {
        if (returnButton != null)
            returnButton.onClick.RemoveListener(OnReturnButtonClicked);
    }
}
