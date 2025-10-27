using UnityEngine;
using UnityEngine.UI;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance;

    [Header("UI (Optional in this scene)")]
    [SerializeField] private Slider volumeSlider; // music slider (can be None in most scenes)

    [Header("Music Player")]
    [SerializeField] private AudioSource musicSource;   // looping BGM source

    [Header("SFX")]
    [SerializeField] private AudioSource sfxSource;     // one-shot SFX source

    private float musicVolume = 1f;
    private float sfxVolume = 1f;

    private void Awake()
    {
        // Singleton setup
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Load saved volume values (or defaults)
        musicVolume = PlayerPrefs.GetFloat("musicVolume", 1f);
        sfxVolume   = PlayerPrefs.GetFloat("sfxVolume",   1f);
    }

    private void Start()
    {
        // Apply volumes to the actual AudioSources
        if (musicSource != null)
        {
            musicSource.volume = musicVolume;
        }

        if (sfxSource != null)
        {
            sfxSource.volume = sfxVolume;
        }

        // Hook up the music volume slider (only if this scene has one)
        if (volumeSlider != null)
        {
            volumeSlider.value = musicVolume;
            volumeSlider.onValueChanged.AddListener(SetMusicVolume);
        }

        // Start looping background music if it isn't already playing
        if (musicSource != null && !musicSource.isPlaying)
        {
            musicSource.loop = true;
            musicSource.Play();
        }
    }

    // ========== MUSIC CONTROL ==========
    private void SetMusicVolume(float v)
    {
        musicVolume = v;

        if (musicSource != null)
        {
            musicSource.volume = musicVolume;
        }

        PlayerPrefs.SetFloat("musicVolume", musicVolume);
    }

    // This lets you register a music slider later (like from Settings scene)
    public void RegisterSlider(Slider newSlider)
    {
        volumeSlider = newSlider;
        volumeSlider.value = musicVolume;
        volumeSlider.onValueChanged.AddListener(SetMusicVolume);
    }

    // ========== SFX CONTROL ==========

    public float GetSFXVolume()
    {
        return sfxVolume;
    }

    public void SetSFXVolume(float v)
    {
        sfxVolume = v;

        if (sfxSource != null)
        {
            sfxSource.volume = sfxVolume;
        }

        PlayerPrefs.SetFloat("sfxVolume", sfxVolume);
    }

    // <-- THIS is what BattleManager and CleanUIManager call
    public void PlaySFX(AudioClip clip)
    {
        if (clip == null || sfxSource == null) return;

        sfxSource.PlayOneShot(clip, sfxVolume);
    }
}
