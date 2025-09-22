using UnityEngine;
using UnityEngine.UI;

public class ProgressBar : MonoBehaviour
{
    [Header("Watch these PanelProgressBars directly (optional per scene)")]
    public PanelProgressBar feedBar;
    public PanelProgressBar cleanBar;
    public PanelProgressBar walkBar; 
    public PanelProgressBar fetchBar; 

    [Header("UI")]
    public Slider progressSlider;  

    [Header("XP Rewards")]
    public int xpPerFullFeed  = 10;
    public int xpPerFullClean = 10;
    public int xpPerFullWalk  = 10; 
    public int xpPerFullFetch = 10; 

    bool wasFullFeed, wasFullClean, wasFullWalk, wasFullFetch;

    void Start()
    {
        UpdateUI();
        if (XPManager.Instance != null)
            XPManager.Instance.OnXPChanged += OnXPChanged;
    }

    void OnDestroy()
    {
        if (XPManager.Instance != null)
            XPManager.Instance.OnXPChanged -= OnXPChanged;
    }

    void Update()
    {
        CheckBar(feedBar,  ref wasFullFeed,  xpPerFullFeed);
        CheckBar(cleanBar, ref wasFullClean, xpPerFullClean);
        CheckBar(walkBar,  ref wasFullWalk,  xpPerFullWalk);
        CheckBar(fetchBar, ref wasFullFetch, xpPerFullFetch);

        UpdateUI();
    }

    void CheckBar(PanelProgressBar bar, ref bool wasFullFlag, int reward)
    {
        if (!bar) return;

        if (bar.IsFull && !wasFullFlag)
        {
            wasFullFlag = true;
            XPManager.Instance?.AddXP(reward);
        }
        else if (!bar.IsFull)
        {
            wasFullFlag = false; 
        }
    }

    void OnXPChanged(int current, int max) => UpdateUI();

    void UpdateUI()
    {
        if (!progressSlider) return;
        float normalized = (XPManager.Instance != null) ? XPManager.Instance.Normalized : 0f;
        progressSlider.value = normalized;
    }
}