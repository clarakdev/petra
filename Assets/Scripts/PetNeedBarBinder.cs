using System.Collections;
using UnityEngine;

/// Keeps a PanelProgressBar synced with one need in PetNeedsManager.
[RequireComponent(typeof(PanelProgressBar))]
[DisallowMultipleComponent]
public class PetNeedBarBinder : MonoBehaviour
{
    public enum Need { Walk, Clean, Feed, Fetch }

    [Header("Bind To")]
    [SerializeField] private Need need = Need.Walk;

    [Header("UI")]
    public PanelProgressBar bar;   // auto-filled if left empty

    PetNeedsManager _petMgr;       // Walk/Clean/Feed
    FetchNeedManager _fetchMgr;    // Fetch

    bool _subscribed;
    Coroutine _bindRoutine;

    void Awake()
    {
        if (!bar) bar = GetComponent<PanelProgressBar>();
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (!bar) bar = GetComponent<PanelProgressBar>();
    }
#endif

    void OnEnable()
    {
        if (_bindRoutine == null)
            _bindRoutine = StartCoroutine(BindWhenReady());
    }

    void OnDisable()
    {
        if (_bindRoutine != null) { StopCoroutine(_bindRoutine); _bindRoutine = null; }
        Unsubscribe();
    }

    void OnDestroy() => Unsubscribe();

    /// Public API to switch which need this bar shows at runtime.
    public void SetNeed(Need newNeed)
    {
        if (need == newNeed) return;

        Unsubscribe(); // unsubscribe from the OLD need first
        need = newNeed;

        if (isActiveAndEnabled)
        {
            if (_bindRoutine != null) { StopCoroutine(_bindRoutine); _bindRoutine = null; }
            _bindRoutine = StartCoroutine(BindWhenReady());
        }
    }

    IEnumerator BindWhenReady()
    {
        if (bar == null) yield break;

        if (need == Need.Fetch)
        {
            while ((_fetchMgr = FetchNeedManager.Instance) == null)
                yield return null;

            _fetchMgr.InitializeFetchIfUnset(bar.value);
        }
        else
        {
            while ((_petMgr = PetNeedsManager.Instance) == null)
                yield return null;

            switch (need)
            {
                case Need.Walk:  _petMgr.InitializeWalkIfUnset (bar.value); break;
                case Need.Clean: _petMgr.InitializeCleanIfUnset(bar.value); break;
                case Need.Feed:  _petMgr.InitializeFeedIfUnset (bar.value); break;
            }
        }

        Subscribe();
        PushCurrentToBar();
        _bindRoutine = null;
    }

    void Subscribe()
    {
        if (_subscribed) return;

        if (need == Need.Fetch)
        {
            if (_fetchMgr == null) return;
            _fetchMgr.OnFetchChanged.RemoveListener(OnValueChanged);
            _fetchMgr.OnFetchChanged.AddListener(OnValueChanged);
        }
        else
        {
            if (_petMgr == null) return;

            switch (need)
            {
                case Need.Walk:
                    _petMgr.OnWalkChanged.RemoveListener(OnValueChanged);
                    _petMgr.OnWalkChanged.AddListener(OnValueChanged);
                    break;
                case Need.Clean:
                    _petMgr.OnCleanChanged.RemoveListener(OnValueChanged);
                    _petMgr.OnCleanChanged.AddListener(OnValueChanged);
                    break;
                case Need.Feed:
                    _petMgr.OnFeedChanged.RemoveListener(OnValueChanged);
                    _petMgr.OnFeedChanged.AddListener(OnValueChanged);
                    break;
            }
        }

        _subscribed = true;
    }

    void Unsubscribe()
    {
        if (!_subscribed) { _subscribed = false; return; }

        if (need == Need.Fetch)
        {
            if (_fetchMgr != null)
                _fetchMgr.OnFetchChanged.RemoveListener(OnValueChanged);
        }
        else
        {
            if (_petMgr != null)
            {
                switch (need)
                {
                    case Need.Walk:  _petMgr.OnWalkChanged .RemoveListener(OnValueChanged); break;
                    case Need.Clean: _petMgr.OnCleanChanged.RemoveListener(OnValueChanged); break;
                    case Need.Feed:  _petMgr.OnFeedChanged .RemoveListener(OnValueChanged); break;
                }
            }
        }

        _subscribed = false;
    }

    void PushCurrentToBar()
    {
        if (bar == null) return;

        if (need == Need.Fetch)
        {
            if (_fetchMgr == null) return;
            bar.SetValue(_fetchMgr.fetch);
        }
        else
        {
            if (_petMgr == null) return;

            float v = need switch
            {
                Need.Walk  => _petMgr.walk,
                Need.Clean => _petMgr.clean,
                _          => _petMgr.feed
            };
            bar.SetValue(v);
        }
    }

    void OnValueChanged(float v)
    {
        if (bar != null) bar.SetValue(v);
    }
}
