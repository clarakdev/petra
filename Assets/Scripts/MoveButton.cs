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

        Debug.Log($"[MoveButton] Awake - Move: {(move ? move.moveName : "NULL")}, Power: {(move ? move.power : 0)}");
    }

    private void Start()
    {
        // If BattleManager not assigned in inspector, try to find it
        if (manager == null)
        {
            manager = FindObjectOfType<BattleManager>();
            Debug.Log($"[MoveButton] BattleManager was null, found: {(manager ? "YES" : "NO")}");
        }

        if (manager == null)
        {
            Debug.LogError("[MoveButton] CRITICAL: No BattleManager found!");
        }

        if (move == null)
        {
            Debug.LogError("[MoveButton] CRITICAL: No DamageData (move) assigned!");
        }

        Debug.Log($"[MoveButton] Start - Ready to attack. Move: {(move ? move.moveName : "NULL")}, Power: {(move ? move.power : 0)}, Manager: {(manager ? "YES" : "NO")}");
    }

    public void OnClick()
    {
        Debug.Log($"[MoveButton] OnClick - Attempting attack with move: {(move ? move.moveName : "NULL")}, Power: {(move ? move.power : 0)}");

        if (manager == null)
        {
            Debug.LogError("[MoveButton] CRITICAL: BattleManager is NULL, cannot attack!");
            return;
        }

        if (move == null)
        {
            Debug.LogError("[MoveButton] CRITICAL: Move data is NULL, cannot attack!");
            return;
        }

        Debug.Log($"[MoveButton] Calling PlayerAttack with damage: {move.power}");
        manager.PlayerAttack(move.power);

        // Return to the command panel immediately after choosing a move
        if (ui) ui.ShowCommandMenu();
    }
}