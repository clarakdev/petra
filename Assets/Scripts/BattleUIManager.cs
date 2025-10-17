using UnityEngine;

public class BattleUIManager : MonoBehaviour
{
    [SerializeField] private GameObject commandPanel;
    [SerializeField] private GameObject movePanel;

    private void Start() => ShowCommandMenu();

    public void ShowCommandMenu()
    {
        commandPanel.SetActive(true);
        movePanel.SetActive(false);
    }

    public void OnFightClicked()
    {
        commandPanel.SetActive(false);
        movePanel.SetActive(true);
    }
}
