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

    [Header("Rewards")]
    [SerializeField] private int winRewardCoins = 100;
    [SerializeField] private int loseRewardCoins = 50;

    private bool resultShown = false;

    private void Awake()
    {
        if (resultPanel != null)
            resultPanel.SetActive(false);

        if (returnButton != null)
            returnButton.onClick.AddListener(OnReturnButtonClicked);

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null && (victorySound != null || defeatSound != null))
            audioSource = gameObject.AddComponent<AudioSource>();
    }

    public void ShowResult(bool didIWin)
    {
        if (resultShown) return;
        resultShown = true;

        // ✅ COIN REWARD SECTION
        if (PlayerCurrency.Instance != null)
        {
            int reward = didIWin ? winRewardCoins : loseRewardCoins;
            PlayerCurrency.Instance.EarnCurrency(reward);
            Debug.Log($"[BattleResultManager] Battle ended. Rewarded {reward} coins ({(didIWin ? "WIN" : "LOSS")}).");
        }
        else
        {
            Debug.LogWarning("[BattleResultManager] PlayerCurrency not found — cannot reward coins.");
        }

        // === Existing UI and audio logic ===
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

        if (audioSource != null)
        {
            AudioClip clipToPlay = didIWin ? victorySound : defeatSound;
            if (clipToPlay != null)
                audioSource.PlayOneShot(clipToPlay);
        }

        StartCoroutine(AutoReturnAfterDelay());
    }

    private IEnumerator AutoReturnAfterDelay()
    {
        yield return new WaitForSeconds(delayBeforeReturn);
        ReturnToBattleRoom();
    }

    private void OnReturnButtonClicked() => ReturnToBattleRoom();

    private void ReturnToBattleRoom()
    {
        if (PhotonNetwork.IsMasterClient)
            PhotonNetwork.LoadLevel(battleRoomSceneName);
    }

    private void OnDestroy()
    {
        if (returnButton != null)
            returnButton.onClick.RemoveListener(OnReturnButtonClicked);
    }
}
