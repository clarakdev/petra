using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Slider))]
public class SFXSliderBinder : MonoBehaviour
{
    private Slider slider;

    void Start()
    {
        slider = GetComponent<Slider>();

        if (SoundManager.Instance == null) return;

        // start with saved value
        slider.value = SoundManager.Instance.GetSFXVolume();

        // update live + save
        slider.onValueChanged.AddListener(SoundManager.Instance.SetSFXVolume);
    }
}
