using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

[DefaultExecutionOrder(-820)]
public sealed class UISFXService : MonoBehaviour
{
    [Header("Routing")]
    [SerializeField] private AudioSource uiSfxSource;
    [SerializeField] private AudioMixerGroup uiSfxGroup;

    [Header("UI SFX Data")]
    [SerializeField] private List<UISFXDefSO> uiSfxDefinitions = new List<UISFXDefSO>();
    [SerializeField] private bool loadResourcesOnAwake = true;
    [SerializeField] private string resourcesPath = "Audio/UISFX";

    [Header("Prototype Fallbacks")]
    [SerializeField] private bool enableBuiltInResourceFallbacks = true;
    [SerializeField] private string waveAlertFallbackResourcePath = "Audio/UISFX/wave_alert";

    [Header("Config Bus Paths")]
    [SerializeField] private string masterBusPath = "bus:/";
    [SerializeField] private string sfxBusPath = "bus:/SFX";

    private readonly Dictionary<UISFXID, UISFXDefSO> definitionsById = new Dictionary<UISFXID, UISFXDefSO>();
    private readonly Dictionary<UISFXID, AudioClip> fallbackClipsById = new Dictionary<UISFXID, AudioClip>();
    private PersistentEventBus persistentEventBus;
    private bool subscribedToPersistentEventBus;
    private float masterVolume = 1f;
    private float sfxVolume = 1f;

    private void Awake()
    {
        RegisterWithServiceLocator();
        EnsureAudioSource();
        BuildDefinitionLookup();
        ApplyCurrentConfig();
    }

    private void OnEnable()
    {
        GeneralEventBus<SettingsChangedEvent>.Subscribe(HandleSettingsChanged);
        SubscribeToPersistentEventBus();
    }

    private void Start()
    {
        SubscribeToPersistentEventBus();
    }

    private void OnDisable()
    {
        GeneralEventBus<SettingsChangedEvent>.Unsubscribe(HandleSettingsChanged);
        UnsubscribeFromPersistentEventBus();
    }

    private void OnDestroy()
    {
        UnsubscribeFromPersistentEventBus();
        ServiceLocator.Unregister<UISFXService>(this);
    }

    public void RefreshConfig()
    {
        ApplyCurrentConfig();
    }

    public void ConfigureAudioSource(AudioSource source, AudioMixerGroup outputGroup)
    {
        if (source != null)
        {
            uiSfxSource = source;
        }

        if (outputGroup != null)
        {
            uiSfxGroup = outputGroup;
        }

        EnsureAudioSource();
    }

    public void PlayUISFX(UISFXID id, bool randomizePitch = false, float volumeScale = 1f)
    {
        if (id == UISFXID.None)
        {
            return;
        }

        if (!TryResolveClip(id, out AudioClip clip, out float clipVolume))
        {
            Debug.LogWarning($"{nameof(UISFXService)} could not find UI SFX data for {id}.", this);
            return;
        }

        EnsureAudioSource();
        if (uiSfxSource == null)
        {
            return;
        }

        uiSfxSource.pitch = randomizePitch ? Random.Range(0.95f, 1.05f) : 1f;
        uiSfxSource.PlayOneShot(clip, Mathf.Clamp01(masterVolume * sfxVolume * clipVolume * Mathf.Max(0f, volumeScale)));
    }

    private void HandleUISFXRequested(UISFXRequestEvent eventData)
    {
        PlayUISFX(eventData.SfxId, eventData.RandomizePitch, eventData.VolumeScale);
    }

    private void EnsureAudioSource()
    {
        if (uiSfxSource == null)
        {
            uiSfxSource = gameObject.AddComponent<AudioSource>();
        }

        uiSfxSource.playOnAwake = false;
        if (uiSfxGroup != null)
        {
            uiSfxSource.outputAudioMixerGroup = uiSfxGroup;
        }
    }

    private void BuildDefinitionLookup()
    {
        definitionsById.Clear();
        fallbackClipsById.Clear();

        for (int i = 0; i < uiSfxDefinitions.Count; i++)
        {
            RegisterDefinition(uiSfxDefinitions[i]);
        }

        if (loadResourcesOnAwake && !string.IsNullOrWhiteSpace(resourcesPath))
        {
            UISFXDefSO[] resourceDefinitions = Resources.LoadAll<UISFXDefSO>(resourcesPath);
            for (int i = 0; i < resourceDefinitions.Length; i++)
            {
                RegisterDefinition(resourceDefinitions[i]);
            }
        }

        if (loadResourcesOnAwake && enableBuiltInResourceFallbacks)
        {
            RegisterFallbackClip(UISFXID.WaveAlert, waveAlertFallbackResourcePath);
        }
    }

    private void RegisterDefinition(UISFXDefSO definition)
    {
        if (definition == null)
        {
            return;
        }

        if (definition.SfxId == UISFXID.None)
        {
            Debug.LogWarning($"{nameof(UISFXService)} ignored '{definition.name}' because its UI SFX ID is None.", definition);
            return;
        }

        if (definition.Clip == null)
        {
            Debug.LogWarning($"{nameof(UISFXService)} ignored '{definition.name}' because it has no audio clip.", definition);
            return;
        }

        if (definitionsById.ContainsKey(definition.SfxId))
        {
            Debug.LogWarning($"{nameof(UISFXService)} found duplicate UI SFX ID {definition.SfxId}. '{definition.name}' will replace the previous definition.", definition);
        }

        definitionsById[definition.SfxId] = definition;
    }

    private void RegisterFallbackClip(UISFXID id, string resourcePath)
    {
        if (id == UISFXID.None || string.IsNullOrWhiteSpace(resourcePath) || definitionsById.ContainsKey(id))
        {
            return;
        }

        AudioClip clip = Resources.Load<AudioClip>(resourcePath);
        if (clip != null)
        {
            fallbackClipsById[id] = clip;
        }
    }

    private bool TryResolveClip(UISFXID id, out AudioClip clip, out float clipVolume)
    {
        if (definitionsById.TryGetValue(id, out UISFXDefSO definition) && definition != null)
        {
            clip = definition.Clip;
            clipVolume = definition.Volume;
            return clip != null;
        }

        if (fallbackClipsById.TryGetValue(id, out clip) && clip != null)
        {
            clipVolume = 1f;
            return true;
        }

        clipVolume = 1f;
        return false;
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

    private void SubscribeToPersistentEventBus()
    {
        if (subscribedToPersistentEventBus)
        {
            return;
        }

        if (persistentEventBus == null)
        {
            ServiceLocator.TryResolve(out persistentEventBus);
        }

        if (persistentEventBus == null)
        {
            return;
        }

        persistentEventBus.UISFXRequested += HandleUISFXRequested;
        subscribedToPersistentEventBus = true;
    }

    private void UnsubscribeFromPersistentEventBus()
    {
        if (!subscribedToPersistentEventBus || persistentEventBus == null)
        {
            return;
        }

        persistentEventBus.UISFXRequested -= HandleUISFXRequested;
        subscribedToPersistentEventBus = false;
    }

    private void RegisterWithServiceLocator()
    {
        if (ServiceLocator.TryResolve<UISFXService>(out UISFXService existingService)
            && existingService != null
            && existingService != this)
        {
            Debug.LogWarning(
                $"{nameof(UISFXService)} on '{name}' replaced the previously registered {nameof(UISFXService)} on '{existingService.name}'.",
                this);
        }

        ServiceLocator.Register<UISFXService>(this);
    }
}
