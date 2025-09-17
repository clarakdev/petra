using UnityEngine;

/// Keeps a PanelProgressBar synced with one need in PetNeedsManager.
/// No global enums used — this component has its own local selector.
[RequireComponent(typeof(PanelProgressBar))]
public class PetNeedBarBinder : MonoBehaviour
{
    public enum Need { Walk, Clean, Feed }   // local to this component only
    [Header("Bind To")]
    public Need need = Need.Walk;

    [Header("UI")]
    public PanelProgressBar bar;             // auto-filled if left empty

    PetNeedsManager _mgr;
    bool _subscribed;

    void Awake()
    {
        if (!bar) bar = GetComponent<PanelProgressBar>();
    }

    void OnEnable()
    {
        TryBind();
    }

    void Start()
    {
        TryBind();
    }

    void Update()
    {
        // In case PetNeedsManager spawns after this component
        if (!_subscribed) TryBind();
    }

    void OnDisable()  => Unsubscribe();
    void OnDestroy()  => Unsubscribe();

    void TryBind()
    {
        if (_subscribed || bar == null) return;

        _mgr = PetNeedsManager.Instance;
        if (_mgr == null) return;

        switch (need)
        {
            case Need.Walk:
                _mgr.OnWalkChanged.AddListener(OnValueChanged);
                // seed once if this is the first scene (no saved value yet)
                _mgr.InitializeWalkIfUnset(bar.value);
                bar.SetValue(_mgr.walk);
                break;

            case Need.Clean:
                _mgr.OnCleanChanged.AddListener(OnValueChanged);
                _mgr.InitializeCleanIfUnset(bar.value);
                bar.SetValue(_mgr.clean);
                break;

            case Need.Feed:
                _mgr.OnFeedChanged.AddListener(OnValueChanged);
                _mgr.InitializeFeedIfUnset(bar.value);
                bar.SetValue(_mgr.feed);
                break;
        }

        _subscribed = true;
    }

    void Unsubscribe()
    {
        if (!_subscribed || _mgr == null) return;

        switch (need)
        {
            case Need.Walk:  _mgr.OnWalkChanged.RemoveListener(OnValueChanged);  break;
            case Need.Clean: _mgr.OnCleanChanged.RemoveListener(OnValueChanged); break;
            case Need.Feed:  _mgr.OnFeedChanged.RemoveListener(OnValueChanged);  break;
        }

        _subscribed = false;
    }

    void OnValueChanged(float v)
    {
        if (bar) bar.SetValue(v);
    }
}
