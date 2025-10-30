using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
public class FetchUIManager : MonoBehaviour
{
    [Header("UI")]
    public PanelProgressBar panelBar;

    [Header("Events")]
    public UnityEvent<float> OnProgress;

    private FetchNeedManager fetchNeeds;

    [Header("Reward Settings")]
    public int rewardCoins = 10; // ✅ change as you like

    void Start()
    {
        fetchNeeds = FetchNeedManager.Instance;
        if (fetchNeeds == null)
        {
            Debug.LogWarning("[FetchUIManager] FetchNeedManager not found!");
            return;
        }

        fetchNeeds.OnFetchChanged.AddListener(PushToUI);

        // ✅ reward trigger
        fetchNeeds.OnFetchHit100.AddListener(OnFetchFull);
    }

    void OnFetchFull()
    {
        if (PlayerCurrency.Instance != null)
        {
            PlayerCurrency.Instance.EarnCurrency(rewardCoins);
            Debug.Log($"[FetchUIManager] Fetch full! Awarded {rewardCoins} coins.");
        }
    }

    void PushToUI(float pct)
    {
        if (panelBar != null) panelBar.SetValue(pct);
    }
}
