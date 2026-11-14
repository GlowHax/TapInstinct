using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Clips")]
    [SerializeField] private AudioClip backgroundMusic;
    [SerializeField] private AudioClip keyTapClip;

    [Header("Settings")]
    [Range(0f, 1f)][SerializeField] private float musicVolume = 0.3f;
    [Range(0f, 1f)][SerializeField] private float sfxVolume = 0.6f;

    private AudioSource musicSource;
    private AudioSource sfxSource;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        SetupAudioSources();
    }

    private void Start()
    {
        PlayBackgroundMusic();
    }

    private void SetupAudioSources()
    {
        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.loop = true;
        musicSource.playOnAwake = false;
        musicSource.volume = musicVolume;

        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.loop = false;
        sfxSource.playOnAwake = false;
        sfxSource.volume = sfxVolume;
    }

    public void PlayBackgroundMusic()
    {
        if (backgroundMusic == null || musicSource == null) return;

        if (musicSource.clip != backgroundMusic)
        {
            musicSource.clip = backgroundMusic;
        }

        if (!musicSource.isPlaying)
        {
            musicSource.Play();
        }
    }

    public void PlayKeyTap()
    {
        if (keyTapClip == null || sfxSource == null) return;

        sfxSource.PlayOneShot(keyTapClip, sfxVolume);
    }
}
