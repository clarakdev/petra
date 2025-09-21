using System.Collections;
using UnityEngine;

/// Keeps a PanelProgressBar synced with one need in PetNeedsManager.
/// No global enums used — this component has its own local selector.
[RequireComponent(typeof(PanelProgressBar))]
[DisallowMultipleComponent]
public class PetNeedBarBinder : MonoBehaviour
{
    public enum Need { Walk, Clean, Feed }

    [Header("Bind To")]
    [SerializeField] private Need need = Need.Walk;

    [Header("UI")]
    public PanelProgressBar bar;   // auto-filled if left empty

    PetNeedsManager _mgr;
    bool _subscribed;
    Coroutine _bindRoutine;

    void Awake()
    {
        if (!bar) bar = GetComponent<PanelProgressBar>();
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        // Helpful in editor: auto-fill bar if missing.
        if (!bar) bar = GetComponent<PanelProgressBar>();
    }
#endif

    void OnEnable()
    {
        // (Re)bind when enabled (avoid double-start)
        if (_bindRoutine == null)
            _bindRoutine = StartCoroutine(BindWhenReady());
    }

    void OnDisable()
    {
        if (_bindRoutine != null) { StopCoroutine(_bindRoutine); _bindRoutine = null; }
        Unsubscribe(); // uses current 'need'
    }

    void OnDestroy()
    {
        Unsubscribe();
    }

    /// Public API to switch which need this bar shows at runtime.
    public void SetNeed(Need newNeed)
    {
        if (need == newNeed) return;

        // IMPORTANT: unsubscribe from the OLD need first
        Unsubscribe();

        need = newNeed;

        // Rebind to the new event
        if (isActiveAndEnabled)
        {
            if (_bindRoutine != null) { StopCoroutine(_bindRoutine); _bindRoutine = null; }
            _bindRoutine = StartCoroutine(BindWhenReady());
        }
    }

    IEnumerator BindWhenReady()
    {
        if (bar == null) yield break;

        // Wait until PetNeedsManager exists (no Update() polling)
        while ((_mgr = PetNeedsManager.Instance) == null)
            yield return null;

        // Seed global once from the bar (only if not already persisted)
        switch (need)
        {
            case Need.Walk:  _mgr.InitializeWalkIfUnset (bar.value); break;
            case Need.Clean: _mgr.InitializeCleanIfUnset(bar.value); break;
            case Need.Feed:  _mgr.InitializeFeedIfUnset (bar.value); break;
        }

        Subscribe();
        PushCurrentToBar();

        // Done binding
        _bindRoutine = null;
    }

    void Subscribe()
    {
        if (_mgr == null || _subscribed) return;

        // Idempotent: remove then add for safety
        switch (need)
        {
            case Need.Walk:
                _mgr.OnWalkChanged.RemoveListener(OnValueChanged);
                _mgr.OnWalkChanged.AddListener(OnValueChanged);
                break;

            case Need.Clean:
                _mgr.OnCleanChanged.RemoveListener(OnValueChanged);
                _mgr.OnCleanChanged.AddListener(OnValueChanged);
                break;

            case Need.Feed:
                _mgr.OnFeedChanged.RemoveListener(OnValueChanged);
                _mgr.OnFeedChanged.AddListener(OnValueChanged);
                break;
        }

        _subscribed = true;
    }

    void Unsubscribe()
    {
        if (!_subscribed || _mgr == null) { _subscribed = false; return; }

        switch (need)
        {
            case Need.Walk:  _mgr.OnWalkChanged .RemoveListener(OnValueChanged); break;
            case Need.Clean: _mgr.OnCleanChanged.RemoveListener(OnValueChanged); break;
            case Need.Feed:  _mgr.OnFeedChanged .RemoveListener(OnValueChanged); break;
        }

        _subscribed = false;
    }

    void PushCurrentToBar()
    {
        if (bar == null || _mgr == null) return;

        float v = need switch
        {
            Need.Walk  => _mgr.walk,
            Need.Clean => _mgr.clean,
            _          => _mgr.feed
        };

        bar.SetValue(v);
    }

    void OnValueChanged(float v)
    {
        if (bar != null) bar.SetValue(v);
    }
}
