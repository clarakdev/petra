using UnityEngine;
using UnityEngine.UI;

public class PlaySatisfaction : MonoBehaviour
{
    public Slider slider; 

    [Header("Tuning")]
    public float addPerFetch = 15f; 
    public float decayPerMinute = 2f;
    public float fullPauseMinutes = 5f;

    float current;
    float decayResumeTime = -1f;

    void Awake()
    {
        if (slider)
        {
            slider.minValue = 0f;
            slider.maxValue = 100f;
            current = slider.value;
        }
    }

    void Update()
    {
        if (!slider) return;

        bool pauseDecay = IsFull() && Time.unscaledTime < decayResumeTime;
        if (!pauseDecay && decayPerMinute > 0f && current > 0f)
        {
            float perSec = decayPerMinute / 60f;
            current = Mathf.Max(0f, current - perSec * Time.unscaledDeltaTime);
            slider.value = current;
        }
    }

    public bool IsFull() => slider && current >= slider.maxValue - 0.01f;

    // Call this when the pet successfully returns the ball
    public void AddFromFetch()
    {
        if (!slider) return;

        current = Mathf.Min(slider.maxValue, current + addPerFetch);
        slider.value = current;

        if (IsFull())
            decayResumeTime = Time.unscaledTime + fullPauseMinutes * 60f;
    }
}