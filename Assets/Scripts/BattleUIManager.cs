using UnityEngine;
using UnityEngine.UI;

public class BattleUIManager : MonoBehaviour
{
    [SerializeField] private GameObject commandPanel; // BattleUI/CommandPanel
    [SerializeField] private GameObject movePanel;    // BattleUI/MovePanel
    [SerializeField] private Button fightButton;      // BattleUI/CommandPanel/FightButton
    [SerializeField] private Button backButton;       // BattleUI/MovePanel/BackButton (optional)

    private void Awake()
    {
        // Auto-wire by name if not set in Inspector.
        if (!commandPanel) commandPanel = transform.Find("CommandPanel")?.gameObject;
        if (!movePanel) movePanel = transform.Find("MovePanel")?.gameObject;

        if (!fightButton && commandPanel)
            fightButton = commandPanel.transform.Find("FightButton")?.GetComponent<Button>();

        if (!backButton && movePanel)
            backButton = movePanel.transform.Find("BackButton")?.GetComponent<Button>();
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
        if (commandPanel) commandPanel.SetActive(true);
        if (movePanel) movePanel.SetActive(false);
    }

    public void OnFightClicked()
    {
        if (commandPanel) commandPanel.SetActive(false);
        if (movePanel) movePanel.SetActive(true);
    }

    public void OnBackClicked() => ShowCommandMenu();
}
