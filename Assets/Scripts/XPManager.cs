using UnityEngine;
using System;

public class XPManager : MonoBehaviour
{
    public static XPManager Instance { get; private set; }

    [Header("Leveling")]
    public int xpToLevel = 100;

    public int CurrentXP { get; private set; } = 0;
    public float Normalized => xpToLevel > 0 ? Mathf.Clamp01((float)CurrentXP / xpToLevel) : 0f;

    public event Action<int,int> OnXPChanged; // (current, max)

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Session starts fresh (no disk load)
        CurrentXP = 0;
    }

    public void AddXP(int amount)
    {
        if (amount == 0) return;
        CurrentXP = Mathf.Max(0, CurrentXP + amount);
        OnXPChanged?.Invoke(CurrentXP, xpToLevel);
    }

    public void SetXP(int value)
    {
        CurrentXP = Mathf.Max(0, value);
        OnXPChanged?.Invoke(CurrentXP, xpToLevel);
    }

    public void SetMax(int newMax)
    {
        xpToLevel = Mathf.Max(1, newMax);
        OnXPChanged?.Invoke(CurrentXP, xpToLevel);
    }

    public void ResetAll()
    {
        CurrentXP = 0;
        OnXPChanged?.Invoke(CurrentXP, xpToLevel);
    }
}