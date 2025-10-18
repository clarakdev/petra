using TMPro;
using UnityEngine;

public class MoveButton : MonoBehaviour
{
    public DamageData move;                 // set per button in Inspector
    public BattleManager manager;
    [SerializeField] private TMP_Text label;
    [SerializeField] private BattleUIManager ui;

    private void Awake()
    {
        if (!ui) ui = GetComponentInParent<BattleUIManager>();
        if (label && move) label.text = move.moveName.ToUpper();
    }

    public void OnClick()
    {
        if (manager && move)
            manager.PlayerAttack(move.power);

        // Return to the command panel immediately after choosing a move
        if (ui) ui.ShowCommandMenu();
    }
}
