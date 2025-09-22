using UnityEngine;
using System;

public class XPManager : MonoBehaviour
{
    public static XPManager Instance { get; private set; }

    [Header("Leveling")]
    public int xpToLevel = 100;

    public int CurrentXP { get; private set; } = 0;
    public float Normalized => xpToLevel > 0 ? Mathf.Clamp01((float)CurrentXP / xpToLevel) : 0f;

    public event Action<int,int> OnXPChanged;

    const string K_XP   = "xp_current";
    const string K_MAX  = "xp_max";

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        CurrentXP = PlayerPrefs.GetInt(K_XP, 0);
        xpToLevel = PlayerPrefs.GetInt(K_MAX, xpToLevel);
    }

    public void AddXP(int amount)
    {
        if (amount == 0) return;
        CurrentXP = Mathf.Max(0, CurrentXP + amount);
        Persist();
        OnXPChanged?.Invoke(CurrentXP, xpToLevel);
    }

    public void SetXP(int value)
    {
        CurrentXP = Mathf.Max(0, value);
        Persist();
        OnXPChanged?.Invoke(CurrentXP, xpToLevel);
    }

    public void SetMax(int newMax)
    {
        xpToLevel = Mathf.Max(1, newMax);
        Persist();
        OnXPChanged?.Invoke(CurrentXP, xpToLevel);
    }

    public void ResetAll()
    {
        CurrentXP = 0;
        Persist();
        OnXPChanged?.Invoke(CurrentXP, xpToLevel);
    }

    void Persist()
    {
        PlayerPrefs.SetInt(K_XP, CurrentXP);
        PlayerPrefs.SetInt(K_MAX, xpToLevel);
        PlayerPrefs.Save();
    }
}
