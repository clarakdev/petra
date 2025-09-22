using System.Collections.Generic;
using UnityEngine;
using TMPro;

[DefaultExecutionOrder(10000)] // run after most scripts
public class Resolutions : MonoBehaviour
{
    [SerializeField] private TMP_Dropdown resolutionDropdown;

    // Whitelist only what you want to show
    [SerializeField] private Vector2Int[] allowedRes =
    {
        new Vector2Int(1920,1080),
        new Vector2Int(1600, 900),
        new Vector2Int(1280, 720)
    };

    private readonly List<Resolution> filtered = new List<Resolution>();
    private int currentIndex = 0;

    void OnEnable()  // runs when object becomes active, including in build
    {
        ApplyWhitelist();
    }

    private void ApplyWhitelist()
    {
        if (!resolutionDropdown) { Debug.LogWarning("Resolutions: No dropdown assigned"); return; }

        filtered.Clear();

        // Build whitelist set for quick lookups
        var allow = new HashSet<(int,int)>();
        foreach (var v in allowedRes) allow.Add((v.x, v.y));

        // Pick the highest refresh rate for each allowed size
        var best = new Dictionary<(int,int), Resolution>();
        foreach (var r in Screen.resolutions)
        {
            var key = (r.width, r.height);
            if (!allow.Contains(key)) continue;
#if UNITY_2022_2_OR_NEWER
            double hz = r.refreshRateRatio.value;
#else
            double hz = r.refreshRate;
#endif
            if (!best.TryGetValue(key, out var cur) ||
                hz > GetHz(cur))
                best[key] = r;
        }

        foreach (var r in best.Values) filtered.Add(r);
        filtered.Sort((a,b) => a.width != b.width ? a.width.CompareTo(b.width) : a.height.CompareTo(b.height));

        // Rebuild dropdown (wipe anything another script put there)
        resolutionDropdown.ClearOptions();
        var options = new List<string>();
        currentIndex = 0;

        for (int i = 0; i < filtered.Count; i++)
        {
            var r = filtered[i];
            options.Add($"{r.width} x {r.height}");
            if (r.width == Screen.currentResolution.width && r.height == Screen.currentResolution.height)
                currentIndex = i;
        }

        resolutionDropdown.AddOptions(options);
        resolutionDropdown.value = Mathf.Clamp(currentIndex, 0, options.Count > 0 ? options.Count - 1 : 0);
        resolutionDropdown.RefreshShownValue();

        // Ensure only our listener is attached
        resolutionDropdown.onValueChanged.RemoveAllListeners();
        resolutionDropdown.onValueChanged.AddListener(SetResolution);
    }

    private static double GetHz(Resolution r)
    {
#if UNITY_2022_2_OR_NEWER
        return r.refreshRateRatio.value;
#else
        return r.refreshRate;
#endif
    }

    private void SetResolution(int idx)
    {
        idx = Mathf.Clamp(idx, 0, filtered.Count - 1);
        var r = filtered[idx];
        Screen.SetResolution(r.width, r.height, Screen.fullScreenMode != FullScreenMode.Windowed);
    }
}