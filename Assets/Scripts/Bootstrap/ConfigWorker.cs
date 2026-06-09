using System;
using System.Collections;
using System.IO;
using System.Collections.Generic;
// using FMOD.Studio;
// using FMODUnity;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class ConfigWorker : MonoBehaviour
{
    [Header("Storage")]
    [SerializeField] string fileName = "config.json";
    [SerializeField] bool writeDefaultsIfMissing = true;

    [Header("Defaults")]
    [SerializeField] float defaultMouseSensitivity = 0.15f;
    [SerializeField] List<MixerBusConfig> defaultMixerBuses = new List<MixerBusConfig>();

    [Header("Behavior")]
    [SerializeField] bool applyOnSceneLoad = true;

    public static ConfigWorker Instance { get; private set; }
    public ConfigData CurrentConfig { get; private set; }
    public bool IsInitialized { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        // WarnIfNotUnderServiceRegistry();
        EnsureInitialized();
    }

    public IEnumerator InitializeService()
    {
        EnsureInitialized();
        yield break;
    }

    void OnEnable()
    {
        if (applyOnSceneLoad)
        {
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    public void EnsureInitialized()
    {
        if (IsInitialized)
        {
            return;
        }

        CurrentConfig = LoadConfig();
        ApplyConfig(CurrentConfig);
        IsInitialized = true;
    }

    // void WarnIfNotUnderServiceRegistry()
    // {
    //     if (ServiceRegistry.Instance == null)
    //     {
    //         return;
    //     }

    //     if (!transform.IsChildOf(ServiceRegistry.Instance.transform))
    //     {
    //         Debug.LogWarning("ConfigWorker is not parented under ServiceRegistry. Consider spawning it via ServiceRegistry.", this);
    //     }
    // }

    public void ApplyConfig(ConfigData config)
    {
        if (config == null)
        {
            return;
        }

        // ApplyMouseSensitivity(config.mouseSensitivity);
        // ApplyMixerBuses(config.mixerBuses);
    }

    void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (CurrentConfig == null)
        {
            return;
        }

        // ApplyMouseSensitivity(CurrentConfig.mouseSensitivity);
    }

    // void ApplyMouseSensitivity(float sensitivity)
    // {
    //     MovementController[] controllers = FindObjectsByType<MovementController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
    //     foreach (MovementController controller in controllers)
    //     {
    //         if (controller == null)
    //         {
    //             continue;
    //         }

    //         controller.SetLookSensitivity(sensitivity);
    //     }
    // }

    // void ApplyMixerBuses(List<MixerBusConfig> mixerBuses)
    // {
    //     if (mixerBuses == null || mixerBuses.Count == 0)
    //     {
    //         return;
    //     }

    //     foreach (MixerBusConfig busConfig in mixerBuses)
    //     {
    //         if (busConfig == null || string.IsNullOrWhiteSpace(busConfig.busPath))
    //         {
    //             continue;
    //         }

    //         busConfig.ClampVolume();
    //         FMOD.RESULT result = RuntimeManager.StudioSystem.getBus(busConfig.busPath, out Bus bus);
    //         if (result != FMOD.RESULT.OK)
    //         {
    //             Debug.LogWarning($"ConfigWorker could not find FMOD bus '{busConfig.busPath}' (Result: {result}).", this);
    //             continue;
    //         }

    //         bus.setVolume(busConfig.volume);
    //     }
    // }

    ConfigData LoadConfig()
    {
        string path = GetConfigPath();
        if (!File.Exists(path))
        {
            ConfigData defaults = CreateDefaultConfig();
            if (writeDefaultsIfMissing)
            {
                SaveConfig(defaults);
            }

            return defaults;
        }

        try
        {
            string json = File.ReadAllText(path);
            if (string.IsNullOrWhiteSpace(json))
            {
                return CreateDefaultConfig();
            }

            ConfigData loaded = JsonUtility.FromJson<ConfigData>(json);
            if (loaded == null)
            {
                return CreateDefaultConfig();
            }

            if (loaded.mixerBuses == null)
            {
                loaded.mixerBuses = new List<MixerBusConfig>();
            }
            else
            {
                for (int i = 0; i < loaded.mixerBuses.Count; i++)
                {
                    MixerBusConfig bus = loaded.mixerBuses[i];
                    if (bus != null)
                    {
                        bus.ClampVolume();
                    }
                }
            }

            return loaded;
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"ConfigWorker failed to read config at '{path}'. {ex.Message}", this);
            return CreateDefaultConfig();
        }
    }

    public void SaveConfig(ConfigData config)
    {
        if (config == null)
        {
            return;
        }

        string path = GetConfigPath();
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            string json = JsonUtility.ToJson(config, true);
            File.WriteAllText(path, json);
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"ConfigWorker failed to write config at '{path}'. {ex.Message}", this);
        }
    }

    ConfigData CreateDefaultConfig()
    {
        ConfigData defaults = new ConfigData
        {
            mouseSensitivity = defaultMouseSensitivity,
            mixerBuses = new List<MixerBusConfig>()
        };

        if (defaultMixerBuses != null && defaultMixerBuses.Count > 0)
        {
            defaults.mixerBuses.AddRange(defaultMixerBuses);
        }
        else
        {
            defaults.mixerBuses.Add(new MixerBusConfig { busPath = "bus:/", volume = 1f });
            defaults.mixerBuses.Add(new MixerBusConfig { busPath = "bus:/SFX", volume = 1f });
            defaults.mixerBuses.Add(new MixerBusConfig { busPath = "bus:/BGM", volume = 1f });
            defaults.mixerBuses.Add(new MixerBusConfig { busPath = "bus:/Ambience", volume = 1f });
        }

        return defaults;
    }

    string GetConfigPath()
    {
        string safeName = string.IsNullOrWhiteSpace(fileName) ? "config.json" : fileName;
        return Path.Combine(Application.persistentDataPath, safeName);
    }
}
