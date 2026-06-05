using UnityEngine;
using UnityEngine.Audio;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Mixer Routing")]
    [SerializeField] private AudioMixerGroup bgmGroup;
    [SerializeField] private AudioMixerGroup towerSfxGroup;
    [SerializeField] private AudioMixerGroup enemySfxGroup;
    [SerializeField] private AudioMixerGroup uiSfxGroup;

    [Header("Dedicated Audio Sources")]
    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private AudioSource towerSfxSource;
    [SerializeField] private AudioSource enemySfxSource;
    [SerializeField] private AudioSource uiSfxSource;

    [Header("Music Service")]
    [SerializeField] private MusicService musicService;

    private void Awake()
    {
        // Singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Otomatis assign mixer group ke source jika belum diatur via Inspector
        if (bgmSource != null && bgmGroup != null) bgmSource.outputAudioMixerGroup = bgmGroup;
        if (towerSfxSource != null && towerSfxGroup != null) towerSfxSource.outputAudioMixerGroup = towerSfxGroup;
        if (enemySfxSource != null && enemySfxGroup != null) enemySfxSource.outputAudioMixerGroup = enemySfxGroup;
        EnsureUISfxSource();

        ResolveMusicService();
        ServiceLocator.Register<AudioManager>(this);
    }

    private void OnDestroy()
    {
        ServiceLocator.Unregister<AudioManager>(this);

        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void PlayBGM(AudioClip clip)
    {
        if (clip == null) return;

        ResolveMusicService();

        if (musicService != null)
        {
            musicService.PlayClip(clip);
            return;
        }

        if (bgmSource == null) return;

        if (bgmSource.clip == clip) return;

        bgmSource.clip = clip;
        bgmSource.loop = true;
        bgmSource.Play();
    }

    public void PlayMusic(MusicIdentifier musicId)
    {
        ResolveMusicService();

        if (musicService == null)
        {
            Debug.LogWarning($"{nameof(AudioManager)} cannot play {musicId} because no {nameof(MusicService)} is available.", this);
            return;
        }

        musicService.PlayMusic(musicId);
    }

    public void StopMusic()
    {
        if (musicService != null)
        {
            musicService.StopMusic();
            return;
        }

        if (bgmSource != null)
        {
            bgmSource.Stop();
        }
    }

    public void PlayTowerSFX(AudioClip clip, bool randomizePitch = true)
    {
        if (clip == null || towerSfxSource == null) return;

        towerSfxSource.pitch = randomizePitch ? Random.Range(0.85f, 1.15f) : 1f;
        towerSfxSource.PlayOneShot(clip);
    }

    public void ConfigureTowerSFXSource(AudioSource source)
    {
        if (source == null) return;

        source.playOnAwake = false;
        if (towerSfxGroup != null)
        {
            source.outputAudioMixerGroup = towerSfxGroup;
        }
    }

    public void PlayEnemySFX(AudioClip clip, bool randomizePitch = false)
    {
        if (clip == null || enemySfxSource == null) return;

        enemySfxSource.pitch = randomizePitch ? Random.Range(0.9f, 1.1f) : 1f;
        enemySfxSource.PlayOneShot(clip);
    }

    public void PlayUISFX(AudioClip clip, bool randomizePitch = false)
    {
        if (clip == null) return;

        EnsureUISfxSource();
        if (uiSfxSource == null) return;

        uiSfxSource.pitch = randomizePitch ? Random.Range(0.95f, 1.05f) : 1f;
        uiSfxSource.PlayOneShot(clip);
    }

    private void ResolveMusicService()
    {
        if (musicService == null)
        {
            musicService = GetComponent<MusicService>();
        }

        if (musicService == null)
        {
            ServiceLocator.TryResolve(out musicService);
        }

        if (musicService == null)
        {
            musicService = gameObject.AddComponent<MusicService>();
        }

        musicService.ConfigureAudioSource(bgmSource, bgmGroup);
    }

    private void EnsureUISfxSource()
    {
        if (uiSfxSource == null)
        {
            uiSfxSource = gameObject.AddComponent<AudioSource>();
            uiSfxSource.playOnAwake = false;
        }

        AudioMixerGroup targetGroup = uiSfxGroup != null ? uiSfxGroup : towerSfxGroup;
        if (targetGroup != null)
        {
            uiSfxSource.outputAudioMixerGroup = targetGroup;
        }
    }
}
