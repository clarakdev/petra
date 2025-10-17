using UnityEngine;

public class BattleUIManager : MonoBehaviour
{
    [SerializeField] private GameObject commandPanel;
    [SerializeField] private GameObject movePanel;
    [SerializeField] private GameObject playerStatusPanel;

    private void Start() => ShowCommandMenu();

    public void ShowCommandMenu()
    {
        commandPanel.SetActive(true);
        playerStatusPanel.SetActive(true);
        movePanel.SetActive(false);
    }

    public void OnFightClicked()
    {
        commandPanel.SetActive(false);
        playerStatusPanel.SetActive(false);
        movePanel.SetActive(true);
    }
}
