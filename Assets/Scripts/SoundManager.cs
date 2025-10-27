using UnityEngine;
using UnityEngine.UI;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance;

    [Header("UI (Optional in this scene)")]
    [SerializeField] private Slider volumeSlider;

    [Header("Music Player")]
    [SerializeField] private AudioSource musicSource;   // <-- you'll assign this in Inspector

    private void Awake()
    {
        // make this persist (optional but recommended if you want music across scenes)
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        // init saved volume
        if (!PlayerPrefs.HasKey("musicVolume"))
        {
            PlayerPrefs.SetFloat("musicVolume", 1f);
        }

        float savedVol = PlayerPrefs.GetFloat("musicVolume");
        AudioListener.volume = savedVol;

        if (volumeSlider != null)
        {
            volumeSlider.value = savedVol;
            volumeSlider.onValueChanged.AddListener(v => ChangeVolume(v));
        }

        // start music if not already playing
        if (musicSource != null && !musicSource.isPlaying)
        {
            musicSource.loop = true;       // safety in case you forget in Inspector
            musicSource.Play();
        }
    }

    // this gets called by the slider listener
    public void ChangeVolume(float newVol)
    {
        AudioListener.volume = newVol;
        PlayerPrefs.SetFloat("musicVolume", newVol);
    }

    // if you open an options menu scene later and need to hook a new slider:
    public void RegisterSlider(Slider newSlider)
    {
        volumeSlider = newSlider;

        float currentVol = PlayerPrefs.GetFloat("musicVolume");
        volumeSlider.value = currentVol;

        volumeSlider.onValueChanged.AddListener(v => ChangeVolume(v));
    }
}
