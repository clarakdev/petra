using UnityEngine;
using System.Collections.Generic;

public class FlowerRoundManager : MonoBehaviour
{
    public static FlowerRoundManager I;

    // You can keep these if you want analytics/debug info.
    readonly HashSet<FlowerSmellUI> all = new HashSet<FlowerSmellUI>();
    readonly HashSet<FlowerSmellUI> clicked = new HashSet<FlowerSmellUI>();

    void Awake()
    {
        if (I == null) { I = this; DontDestroyOnLoad(gameObject); }
        else Destroy(gameObject);
    }

    public void Register(FlowerSmellUI f)
    {
        if (f != null) all.Add(f);
    }

    public void Unregister(FlowerSmellUI f)
    {
        if (f != null)
        {
            all.Remove(f);
            clicked.Remove(f);
        }
    }

    // One-shot behavior: just record that it was clicked. No reset.
    public void ReportClicked(FlowerSmellUI f)
    {
        if (f != null) clicked.Add(f);
        // (intentionally no calls to ResetRoundClick)
    }
}
