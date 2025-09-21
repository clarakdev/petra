using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

[DisallowMultipleComponent]
public class PlaySatisfaction : MonoBehaviour
{
    [Header("UI")]
    public Slider slider;                 // 0..100 (min=0, max=100)

    [Header("Tuning")]
    public float addPerFetch = 15f;       // how much to add per successful fetch
    public float decayPerMinute = 2f;     // drains while not locked
    public float fullPauseMinutes = 20f;  // lockout duration after full

    [Header("Events")]
    public UnityEvent OnBecameFull;       // fires once when slider hits max

    float _current;                       // 0..100
    float _unlockAtTime = -1f;            // realtime when lock ends
    bool  _awardedThisRound = false;      // award-once guard

    const float EPS = 0.01f;

    void Awake()
    {
        if (slider)
        {
            slider.minValue = 0f;
            slider.maxValue = 100f;
            _current = Mathf.Clamp(slider.value, 0f, 100f);
            Push();
        }
    }

    void Update()
    {
        if (!slider) return;

        // If locked (cooldown), do nothing until time passes
        if (IsLocked()) return;

        // Drain if configured
        if (decayPerMinute > 0f && _current > 0f)
        {
            float perSec = decayPerMinute / 60f;
            _current = Mathf.Max(0f, _current - perSec * Time.unscaledDeltaTime);
            Push();

            // Once we’re below max again, re-arm the event for the next full
            if (_current < slider.maxValue - EPS)
                _awardedThisRound = false;
        }
    }

    public bool IsFull()   => slider && _current >= slider.maxValue - EPS;
    public bool IsLocked() => _unlockAtTime > 0f && Time.unscaledTime < _unlockAtTime;

    /// Gate for PetFetchManager to check before starting a fetch.
    public bool CanPlayFetch() => !IsLocked() && !IsFull();

    /// Call this when the pet successfully returns the ball.
    public void AddFromFetch()
    {
        if (!slider) return;
        if (IsLocked()) return; // ignore gains while locked

        _current = Mathf.Min(slider.maxValue, _current + addPerFetch);
        Push();

        if (!_awardedThisRound && IsFull())
        {
            _awardedThisRound = true;
            OnBecameFull?.Invoke();
            // Start lockout; slider will later drain after lock ends
            _unlockAtTime = Time.unscaledTime + fullPauseMinutes * 60f;
        }
    }

    /// Optional helper if you want to manually end the lock (debug, etc.)
    public void ForceUnlock() => _unlockAtTime = -1f;

    void Push()
    {
        if (slider) slider.value = _current;
    }
}
