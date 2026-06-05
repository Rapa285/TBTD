using System.Collections.Generic;
using Ambience;
using UnityEngine;
using UnityEngine.Audio;

[DefaultExecutionOrder(-830)]
public sealed class MusicService : MonoBehaviour
{
    public static MusicService Instance { get; private set; }

    [Header("Routing")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioMixerGroup bgmGroup;

    [Header("Music Data")]
    [SerializeField] private List<MusicClipData> musicTracks = new List<MusicClipData>();
    [SerializeField] private bool loadResourcesOnAwake = true;

    [Header("Startup")]
    [SerializeField] private bool playOnStart;
    [SerializeField] private MusicIdentifier startMusic = MusicIdentifier.MainMenuBGM;
    [SerializeField] private bool persistAcrossScenes = true;

    [Header("Config Bus Paths")]
    [SerializeField] private string masterBusPath = "bus:/";
    [SerializeField] private string musicBusPath = "bus:/BGM";

    private readonly Dictionary<MusicIdentifier, MusicClipData> tracksById = new Dictionary<MusicIdentifier, MusicClipData>();
    private MusicClipData currentMusicData;
    private float masterVolume = 1f;
    private float musicVolume = 1f;

    public MusicClipData CurrentMusicData => currentMusicData;
    public AudioClip CurrentClip => musicSource != null ? musicSource.clip : null;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (persistAcrossScenes)
        {
            DontDestroyOnLoad(gameObject);
        }

        RegisterWithServiceLocator();
        EnsureAudioSource();
        BuildTrackLookup();
        ApplyCurrentConfig();
    }

    private void OnEnable()
    {
        GeneralEventBus<SettingsChangedEvent>.Subscribe(HandleSettingsChanged);
    }

    private void Start()
    {
        if (playOnStart)
        {
            PlayMusic(startMusic);
        }
    }

    private void OnDisable()
    {
        GeneralEventBus<SettingsChangedEvent>.Unsubscribe(HandleSettingsChanged);
    }

    private void OnDestroy()
    {
        ServiceLocator.Unregister<MusicService>(this);

        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void ConfigureAudioSource(AudioSource source, AudioMixerGroup outputGroup)
    {
        if (source != null)
        {
            musicSource = source;
        }

        if (outputGroup != null)
        {
            bgmGroup = outputGroup;
        }

        EnsureAudioSource();
        ApplyCurrentTrackVolume();
    }

    public void PlayMusic(MusicIdentifier musicId)
    {
        if (!tracksById.TryGetValue(musicId, out MusicClipData musicData)
            || musicData == null
            || musicData.Clip == null)
        {
            Debug.LogWarning($"{nameof(MusicService)} could not find music data for {musicId}.", this);
            return;
        }

        EnsureAudioSource();

        if (musicSource.clip == musicData.Clip && musicSource.isPlaying)
        {
            return;
        }

        if (currentMusicData != null && musicSource.clip != null)
        {
            MusicClipData fromData = currentMusicData;
            MusicTransitionHelper.Instance.FadeOut(
                musicSource,
                fromData.ExitDelay,
                TransitionCurve.ExponentialOut,
                () => StartTrackAfterTransition(fromData, musicData));
            return;
        }

        StartTrack(musicData, 0f, playImmediately: true);
    }

    public void PlayClip(AudioClip clip, bool loop = true)
    {
        if (clip == null)
        {
            return;
        }

        EnsureAudioSource();
        MusicTransitionHelper.Instance.StopAllTransitions();
        currentMusicData = null;
        musicSource.clip = clip;
        musicSource.loop = loop;
        musicSource.volume = masterVolume * musicVolume;
        musicSource.Play();
    }

    public void StopMusic()
    {
        if (musicSource == null)
        {
            return;
        }

        MusicTransitionHelper.Instance.StopAllTransitions();
        musicSource.Stop();
        currentMusicData = null;
    }

    public void RefreshConfig()
    {
        ApplyCurrentConfig();
    }

    public float TimeToNextBeat(MusicBeat beatType = MusicBeat.Beat)
    {
        if (currentMusicData == null || musicSource == null)
        {
            return 0f;
        }

        float beatDuration = beatType switch
        {
            MusicBeat.Bar => currentMusicData.SecondsPerBar,
            MusicBeat.Beat => currentMusicData.SecondsPerBeat,
            MusicBeat.HalfBeat => currentMusicData.SecondsPerBeat / 2f,
            MusicBeat.QuarterBeat => currentMusicData.SecondsPerBeat / 4f,
            _ => currentMusicData.SecondsPerBeat
        };

        if (beatDuration <= 0f)
        {
            return 0f;
        }

        float currentBeatPosition = musicSource.time % beatDuration;
        return beatDuration - currentBeatPosition;
    }

    private void StartTrackAfterTransition(MusicClipData fromData, MusicClipData toData)
    {
        float targetTime = MusicTransitionHelper.CalculateTransitionEnterTimeSeconds(fromData, musicSource, toData);
        StartTrack(toData, targetTime, playImmediately: false);
    }

    private void StartTrack(MusicClipData musicData, float startTime, bool playImmediately)
    {
        currentMusicData = musicData;
        musicSource.clip = musicData.Clip;
        musicSource.loop = musicData.IsLooping;

        if (musicSource.clip.length > 0f)
        {
            musicSource.time = Mathf.Clamp(startTime, 0f, musicSource.clip.length - 0.01f);
        }

        float targetVolume = CalculateTrackVolume(musicData);
        if (playImmediately || musicData.Delay <= 0f)
        {
            musicSource.volume = targetVolume;
            musicSource.Play();
            return;
        }

        MusicTransitionHelper.Instance.FadeIn(
            musicSource,
            targetVolume,
            musicData.Delay,
            TransitionCurve.ExponentialIn);
    }

    private void EnsureAudioSource()
    {
        if (musicSource == null)
        {
            musicSource = GetComponent<AudioSource>();
        }

        if (musicSource == null)
        {
            musicSource = gameObject.AddComponent<AudioSource>();
        }

        musicSource.playOnAwake = false;
        if (bgmGroup != null)
        {
            musicSource.outputAudioMixerGroup = bgmGroup;
        }
    }

    private void BuildTrackLookup()
    {
        tracksById.Clear();

        for (int i = 0; i < musicTracks.Count; i++)
        {
            RegisterTrack(musicTracks[i]);
        }

        if (loadResourcesOnAwake)
        {
            MusicClipData[] resourceTracks = Resources.LoadAll<MusicClipData>("Audio/Music");
            for (int i = 0; i < resourceTracks.Length; i++)
            {
                RegisterTrack(resourceTracks[i]);
            }
        }

        MusicClipData.MusicRegistry = new Dictionary<MusicIdentifier, MusicClipData>(tracksById);
    }

    private void RegisterTrack(MusicClipData track)
    {
        if (track == null || track.Clip == null)
        {
            return;
        }

        tracksById[track.MusicId] = track;
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
        musicVolume = GetBusVolume(config, musicBusPath);
        AudioClipData.MasterVolumeProperty = masterVolume;
        MusicClipData.MusicMasterVolumeProperty = musicVolume;
        ApplyCurrentTrackVolume();
    }

    private void ApplyCurrentTrackVolume()
    {
        if (musicSource == null || musicSource.clip == null)
        {
            return;
        }

        musicSource.volume = currentMusicData != null
            ? CalculateTrackVolume(currentMusicData)
            : masterVolume * musicVolume;
    }

    private float CalculateTrackVolume(MusicClipData musicData)
    {
        float clipVolume = musicData != null ? musicData.Volume : 1f;
        return Mathf.Clamp01(masterVolume * musicVolume * clipVolume);
    }

    private float GetBusVolume(ConfigData config, string busPath)
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

    private void RegisterWithServiceLocator()
    {
        if (ServiceLocator.TryResolve<MusicService>(out MusicService existingService)
            && existingService != null
            && existingService != this)
        {
            Debug.LogWarning(
                $"{nameof(MusicService)} on '{name}' replaced the previously registered {nameof(MusicService)} on '{existingService.name}'.",
                this);
        }

        ServiceLocator.Register<MusicService>(this);
    }
}

public enum MusicBeat
{
    Bar,
    Beat,
    HalfBeat,
    QuarterBeat
}
