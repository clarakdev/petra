using UnityEngine;
using UnityEngine.UI;

public class BattleUIManager : MonoBehaviour
{
    [SerializeField] private GameObject commandPanel;
    [SerializeField] private GameObject movePanel;
    [SerializeField] private GameObject playerStatusPanel;
    [SerializeField] private Button fightButton;
    [SerializeField] private Button backButton;

    private void Awake()
    {
        if (!commandPanel) commandPanel = transform.Find("CommandPanel")?.gameObject;
        if (!movePanel) movePanel = transform.Find("MovePanel")?.gameObject;
        if (!playerStatusPanel) playerStatusPanel = transform.Find("PlayerStatusPanel")?.gameObject;
        if (!fightButton && commandPanel)
            fightButton = commandPanel.transform.Find("FightButton")?.GetComponent<Button>();
        if (!backButton && movePanel)
            backButton = movePanel.transform.Find("BackButton")?.GetComponent<Button>();

        Debug.Log($"[BattleUIManager] Awake - CommandPanel: {(commandPanel ? "YES" : "NO")}, " +
                  $"MovePanel: {(movePanel ? "YES" : "NO")}, " +
                  $"FightButton: {(fightButton ? "YES" : "NO")}, " +
                  $"BackButton: {(backButton ? "YES" : "NO")}");

        // DEBUG: Check all MoveButtons
        var moveButtons = GetComponentsInChildren<MoveButton>();
        Debug.Log($"[BattleUIManager] Found {moveButtons.Length} MoveButtons");
        foreach (var btn in moveButtons)
        {
            Debug.Log($"  - MoveButton: Move={btn.move?.moveName}, Power={btn.move?.power}, Manager={btn.manager}");
        }
    }

    private void OnEnable()
    {
        if (fightButton) fightButton.onClick.AddListener(OnFightClicked);
        if (backButton) backButton.onClick.AddListener(OnBackClicked);
    }

    private void OnDisable()
    {
        if (fightButton) fightButton.onClick.RemoveListener(OnFightClicked);
        if (backButton) backButton.onClick.RemoveListener(OnBackClicked);
    }

    private void Start() => ShowCommandMenu();

    public void ShowCommandMenu()
    {
        Debug.Log("[BattleUIManager] ShowCommandMenu - Showing command panel, hiding move panel");
        if (commandPanel) commandPanel.SetActive(true);
        if (playerStatusPanel) playerStatusPanel.SetActive(true);
        if (movePanel) movePanel.SetActive(false);
    }

    public void OnFightClicked()
    {
        Debug.Log("[BattleUIManager] OnFightClicked - Showing move panel");
        if (commandPanel) commandPanel.SetActive(false);
        if (playerStatusPanel) playerStatusPanel.SetActive(false);
        if (movePanel) movePanel.SetActive(true);
    }

    public void OnBackClicked()
    {
        Debug.Log("[BattleUIManager] OnBackClicked - Returning to command menu");
        ShowCommandMenu();
    }
}