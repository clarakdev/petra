using UnityEngine;
using UnityEngine.UI;

public class BattleUIManager : MonoBehaviour
{
    [SerializeField] private GameObject commandPanel;
    [SerializeField] private GameObject movePanel;
    [SerializeField] private GameObject inventoryPanel;
    [SerializeField] private GameObject playerStatusPanel;

    [SerializeField] private Button fightButton;
    [SerializeField] private Button inventoryButton;
    [SerializeField] private Button backButton;

    private void Awake()
    {
        // Auto-find panels
        if (!commandPanel) commandPanel = transform.Find("CommandPanel")?.gameObject;
        if (!movePanel) movePanel = transform.Find("MovePanel")?.gameObject;
        if (!inventoryPanel) inventoryPanel = transform.Find("InventoryPanel")?.gameObject;
        if (!playerStatusPanel) playerStatusPanel = transform.Find("PlayerStatusPanel")?.gameObject;

        // Auto-find buttons in CommandPanel
        if (!fightButton && commandPanel)
            fightButton = commandPanel.transform.Find("FightButton")?.GetComponent<Button>();
        if (!inventoryButton && commandPanel)
            inventoryButton = commandPanel.transform.Find("InventoryButton")?.GetComponent<Button>();

        // Auto-find back button in MovePanel
        if (!backButton && movePanel)
            backButton = movePanel.transform.Find("BackButton")?.GetComponent<Button>();

        Debug.Log($"[BattleUIManager] Awake - CommandPanel: {(commandPanel ? "YES" : "NO")}, " +
                  $"MovePanel: {(movePanel ? "YES" : "NO")}, " +
                  $"InventoryPanel: {(inventoryPanel ? "YES" : "NO")}, " +
                  $"FightButton: {(fightButton ? "YES" : "NO")}, " +
                  $"InventoryButton: {(inventoryButton ? "YES" : "NO")}");
    }

    private void OnEnable()
    {
        if (fightButton) fightButton.onClick.AddListener(OnFightClicked);
        if (inventoryButton) inventoryButton.onClick.AddListener(OnInventoryClicked);
        if (backButton) backButton.onClick.AddListener(OnBackClicked);
    }

    private void OnDisable()
    {
        if (fightButton) fightButton.onClick.RemoveListener(OnFightClicked);
        if (inventoryButton) inventoryButton.onClick.RemoveListener(OnInventoryClicked);
        if (backButton) backButton.onClick.RemoveListener(OnBackClicked);
    }

    private void Start() => ShowCommandMenu();

    public void ShowCommandMenu()
    {
        Debug.Log("[BattleUIManager] ShowCommandMenu - Showing command panel");
        if (commandPanel) commandPanel.SetActive(true);
        if (playerStatusPanel) playerStatusPanel.SetActive(true);
        if (movePanel) movePanel.SetActive(false);
        if (inventoryPanel) inventoryPanel.SetActive(false);
    }

    public void OnFightClicked()
    {
        Debug.Log("[BattleUIManager] OnFightClicked - Showing move panel");
        if (commandPanel) commandPanel.SetActive(false);
        if (playerStatusPanel) playerStatusPanel.SetActive(false);
        if (movePanel) movePanel.SetActive(true);
        if (inventoryPanel) inventoryPanel.SetActive(false);
    }

    public void OnInventoryClicked()
    {
        Debug.Log("[BattleUIManager] OnInventoryClicked - Showing inventory (potions only)");
        if (commandPanel) commandPanel.SetActive(false);
        if (playerStatusPanel) playerStatusPanel.SetActive(false);
        if (movePanel) movePanel.SetActive(false);
        if (inventoryPanel) inventoryPanel.SetActive(true);
    }

    public void OnBackClicked()
    {
        Debug.Log("[BattleUIManager] OnBackClicked - Returning to command menu");
        ShowCommandMenu();
    }
}