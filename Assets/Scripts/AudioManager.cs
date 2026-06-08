using System.Collections.Generic;
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

    [Header("Gameplay SFX Limits")]
    [SerializeField, Min(1)] private int maxGameplaySfxInstancesPerClipPerFrame = 3;

    [Header("Config Bus Paths")]
    [SerializeField] private string masterBusPath = "bus:/";
    [SerializeField] private string sfxBusPath = "bus:/SFX";

    [Header("Music Service")]
    [SerializeField] private MusicService musicService;

    private readonly Dictionary<AudioClip, int> gameplaySfxCountsByClip = new Dictionary<AudioClip, int>();
    private readonly HashSet<AudioSource> configuredTowerSfxSources = new HashSet<AudioSource>();
    private int trackedGameplaySfxFrame = -1;
    private float masterVolume = 1f;
    private float sfxVolume = 1f;

    private void Awake()
    {
        // Singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        // DontDestroyOnLoad(gameObject);

        // Otomatis assign mixer group ke source jika belum diatur via Inspector
        if (bgmSource != null && bgmGroup != null) bgmSource.outputAudioMixerGroup = bgmGroup;
        if (towerSfxSource != null && towerSfxGroup != null) towerSfxSource.outputAudioMixerGroup = towerSfxGroup;
        if (enemySfxSource != null && enemySfxGroup != null) enemySfxSource.outputAudioMixerGroup = enemySfxGroup;
        EnsureUISfxSource();

        ResolveMusicService();
        ApplyCurrentConfig();
        ServiceLocator.Register<AudioManager>(this);
    }

    private void OnEnable()
    {
        GeneralEventBus<SettingsChangedEvent>.Subscribe(HandleSettingsChanged);
    }

    private void OnDisable()
    {
        GeneralEventBus<SettingsChangedEvent>.Unsubscribe(HandleSettingsChanged);
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
        PlayLimitedGameplaySFX(clip, towerSfxSource, randomizePitch, 0.85f, 1.15f);
    }

    public void ConfigureTowerSFXSource(AudioSource source)
    {
        if (source == null) return;

        source.playOnAwake = false;
        if (towerSfxGroup != null)
        {
            source.outputAudioMixerGroup = towerSfxGroup;
        }

        configuredTowerSfxSources.Add(source);
        ApplySfxVolume(source);
    }

    public void PlayEnemySFX(AudioClip clip, bool randomizePitch = false)
    {
        PlayLimitedGameplaySFX(clip, enemySfxSource, randomizePitch, 0.9f, 1.1f);
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

    private void PlayLimitedGameplaySFX(
        AudioClip clip,
        AudioSource source,
        bool randomizePitch,
        float minPitch,
        float maxPitch)
    {
        if (clip == null || source == null)
        {
            return;
        }

        if (!TryAcceptGameplaySFXRequest(clip))
        {
            return;
        }

        source.pitch = randomizePitch ? Random.Range(minPitch, maxPitch) : 1f;
        source.PlayOneShot(clip);
    }

    private void HandleSettingsChanged(SettingsChangedEvent e)
    {
        ApplyConfig(e.Config);
    }

    private void ApplyCurrentConfig()
    {
        ConfigData config = ConfigWorker.Instance != null ? ConfigWorker.Instance.CurrentConfig : null;
        ApplyConfig(config);
    }

    private void ApplyConfig(ConfigData config)
    {
        masterVolume = GetBusVolume(config, masterBusPath);
        sfxVolume = GetBusVolume(config, sfxBusPath);

        ApplySfxVolume(towerSfxSource);
        ApplySfxVolume(enemySfxSource);
        ApplySfxVolume(uiSfxSource);

        configuredTowerSfxSources.RemoveWhere(source => source == null);
        foreach (AudioSource source in configuredTowerSfxSources)
        {
            ApplySfxVolume(source);
        }
    }

    private void ApplySfxVolume(AudioSource source)
    {
        if (source == null)
        {
            return;
        }

        source.volume = Mathf.Clamp01(masterVolume * sfxVolume);
    }

    private static float GetBusVolume(ConfigData config, string busPath)
    {
        if (config == null || config.mixerBuses == null || string.IsNullOrWhiteSpace(busPath))
        {
            return 1f;
        }

        for (int i = 0; i < config.mixerBuses.Count; i++)
        {
            MixerBusConfig bus = config.mixerBuses[i];
            if (bus == null)
            {
                continue;
            }

            if (string.Equals(bus.busPath, busPath, System.StringComparison.OrdinalIgnoreCase))
            {
                bus.ClampVolume();
                return bus.volume;
            }
        }

        return 1f;
    }

    private bool TryAcceptGameplaySFXRequest(AudioClip clip)
    {
        int currentFrame = Time.frameCount;
        if (trackedGameplaySfxFrame != currentFrame)
        {
            trackedGameplaySfxFrame = currentFrame;
            gameplaySfxCountsByClip.Clear();
        }

        int maxInstances = Mathf.Max(1, maxGameplaySfxInstancesPerClipPerFrame);
        gameplaySfxCountsByClip.TryGetValue(clip, out int currentCount);
        if (currentCount >= maxInstances)
        {
            return false;
        }

        gameplaySfxCountsByClip[clip] = currentCount + 1;
        return true;
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

        ApplySfxVolume(uiSfxSource);
    }
}
