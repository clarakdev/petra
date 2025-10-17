using TMPro;
using UnityEngine;

public class MoveButton : MonoBehaviour
{
    public DamageData move;                 // set per button in Inspector
    public BattleManager manager;
    [SerializeField] private TMP_Text label;

    private void Awake()
    {
        if (label && move) label.text = move.moveName.ToUpper();
    }

    public void OnClick()
    {
        manager.PlayerAttack(move.power);
    }
}
