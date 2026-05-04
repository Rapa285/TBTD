using System;
using System.Collections.Generic;
using ASCENTA.Events;
using UnityEngine;
using UnityEngine.UI;

public sealed class SettingsWorker : MonoBehaviour
{
    [Header("Volume Sliders")]
    [SerializeField] Slider masterVolumeSlider;
    [SerializeField] Slider sfxVolumeSlider;
    [SerializeField] Slider musicVolumeSlider;
    [SerializeField] string masterBusPath = "bus:/";
    [SerializeField] string sfxBusPath = "bus:/SFX";
    [SerializeField] string musicBusPath = "bus:/BGM";
    [SerializeField] bool enforceVolumeRange = true;

    [Header("Mouse Sensitivity")]
    [SerializeField] Slider mouseSensitivitySlider;
    [SerializeField] InputField mouseSensitivityInput;
    [SerializeField] float minMouseSensitivity = 0.01f;
    [SerializeField] float maxMouseSensitivity = 1.5f;
    [SerializeField] string mouseSensitivityFormat = "0.00";

    [Header("Buttons")]
    [SerializeField] Button cancelButton;
    [SerializeField] Button saveButton;

    ConfigData workingConfig;
    ConfigData savedSnapshot;
    bool suppressEvents;

    void OnEnable()
    {
        EnsureConfigLoaded();
        BindUI();
        LoadFromConfig();
    }

    void OnDisable()
    {
        UnbindUI();
    }

    void EnsureConfigLoaded()
    {
        if (ConfigWorker.Instance != null)
        {
            ConfigWorker.Instance.EnsureInitialized();
        }
    }

    void BindUI()
    {
        if (masterVolumeSlider != null)
        {
            masterVolumeSlider.onValueChanged.AddListener(HandleMasterVolumeChanged);
        }

        if (sfxVolumeSlider != null)
        {
            sfxVolumeSlider.onValueChanged.AddListener(HandleSfxVolumeChanged);
        }

        if (musicVolumeSlider != null)
        {
            musicVolumeSlider.onValueChanged.AddListener(HandleMusicVolumeChanged);
        }

        if (mouseSensitivitySlider != null)
        {
            mouseSensitivitySlider.onValueChanged.AddListener(HandleMouseSensitivitySliderChanged);
        }

        if (mouseSensitivityInput != null)
        {
            mouseSensitivityInput.onEndEdit.AddListener(HandleMouseSensitivityInputChanged);
        }

        if (cancelButton != null)
        {
            cancelButton.onClick.AddListener(CancelChanges);
        }

        if (saveButton != null)
        {
            saveButton.onClick.AddListener(SaveChanges);
        }
    }

    void UnbindUI()
    {
        if (masterVolumeSlider != null)
        {
            masterVolumeSlider.onValueChanged.RemoveListener(HandleMasterVolumeChanged);
        }

        if (sfxVolumeSlider != null)
        {
            sfxVolumeSlider.onValueChanged.RemoveListener(HandleSfxVolumeChanged);
        }

        if (musicVolumeSlider != null)
        {
            musicVolumeSlider.onValueChanged.RemoveListener(HandleMusicVolumeChanged);
        }

        if (mouseSensitivitySlider != null)
        {
            mouseSensitivitySlider.onValueChanged.RemoveListener(HandleMouseSensitivitySliderChanged);
        }

        if (mouseSensitivityInput != null)
        {
            mouseSensitivityInput.onEndEdit.RemoveListener(HandleMouseSensitivityInputChanged);
        }

        if (cancelButton != null)
        {
            cancelButton.onClick.RemoveListener(CancelChanges);
        }

        if (saveButton != null)
        {
            saveButton.onClick.RemoveListener(SaveChanges);
        }
    }

    void LoadFromConfig()
    {
        ConfigData current = ConfigWorker.Instance != null ? ConfigWorker.Instance.CurrentConfig : null;
        if (current == null)
        {
            current = new ConfigData();
        }

        savedSnapshot = CloneConfig(current);
        workingConfig = CloneConfig(current);

        RefreshUI(workingConfig);
    }

    void RefreshUI(ConfigData source)
    {
        suppressEvents = true;

        if (enforceVolumeRange)
        {
            ConfigureVolumeSlider(masterVolumeSlider);
            ConfigureVolumeSlider(sfxVolumeSlider);
            ConfigureVolumeSlider(musicVolumeSlider);
        }

        SetSliderValue(masterVolumeSlider, GetBusVolume(source, masterBusPath));
        SetSliderValue(sfxVolumeSlider, GetBusVolume(source, sfxBusPath));
        SetSliderValue(musicVolumeSlider, GetBusVolume(source, musicBusPath));

        SetMouseSensitivity(source != null ? source.mouseSensitivity : minMouseSensitivity);

        suppressEvents = false;
    }

    void ConfigureVolumeSlider(Slider slider)
    {
        if (slider == null)
        {
            return;
        }

        slider.minValue = 0f;
        slider.maxValue = 1f;
    }

    void SetSliderValue(Slider slider, float value)
    {
        if (slider == null)
        {
            return;
        }

        slider.value = Mathf.Clamp01(value);
    }

    void SetMouseSensitivity(float value)
    {
        float clamped = ClampSensitivity(value);

        if (mouseSensitivitySlider != null)
        {
            mouseSensitivitySlider.minValue = minMouseSensitivity;
            mouseSensitivitySlider.maxValue = maxMouseSensitivity;
            mouseSensitivitySlider.value = clamped;
        }

        if (mouseSensitivityInput != null)
        {
            mouseSensitivityInput.text = clamped.ToString(mouseSensitivityFormat);
        }
    }

    float ClampSensitivity(float value)
    {
        return Mathf.Clamp(value, minMouseSensitivity, maxMouseSensitivity);
    }

    void HandleMasterVolumeChanged(float value)
    {
        if (suppressEvents)
        {
            return;
        }

        EnsureWorkingConfig();
        SetBusVolume(workingConfig, masterBusPath, value);
    }

    void HandleSfxVolumeChanged(float value)
    {
        if (suppressEvents)
        {
            return;
        }

        EnsureWorkingConfig();
        SetBusVolume(workingConfig, sfxBusPath, value);
    }

    void HandleMusicVolumeChanged(float value)
    {
        if (suppressEvents)
        {
            return;
        }

        EnsureWorkingConfig();
        SetBusVolume(workingConfig, musicBusPath, value);
    }

    void HandleMouseSensitivitySliderChanged(float value)
    {
        if (suppressEvents)
        {
            return;
        }

        EnsureWorkingConfig();
        float clamped = ClampSensitivity(value);
        workingConfig.mouseSensitivity = clamped;

        suppressEvents = true;
        if (mouseSensitivityInput != null)
        {
            mouseSensitivityInput.text = clamped.ToString(mouseSensitivityFormat);
        }
        suppressEvents = false;
    }

    void HandleMouseSensitivityInputChanged(string text)
    {
        if (suppressEvents)
        {
            return;
        }

        EnsureWorkingConfig();
        if (!float.TryParse(text, out float parsed))
        {
            RefreshUI(workingConfig);
            return;
        }

        float clamped = ClampSensitivity(parsed);
        workingConfig.mouseSensitivity = clamped;

        suppressEvents = true;
        if (mouseSensitivitySlider != null)
        {
            mouseSensitivitySlider.value = clamped;
        }
        if (mouseSensitivityInput != null)
        {
            mouseSensitivityInput.text = clamped.ToString(mouseSensitivityFormat);
        }
        suppressEvents = false;
    }

    void CancelChanges()
    {
        if (savedSnapshot == null)
        {
            return;
        }

        workingConfig = CloneConfig(savedSnapshot);
        RefreshUI(workingConfig);
    }

    void SaveChanges()
    {
        if (workingConfig == null)
        {
            return;
        }

        if (ConfigWorker.Instance == null)
        {
            Debug.LogWarning($"{nameof(SettingsWorker)} cannot save because {nameof(ConfigWorker)} is missing.");
            return;
        }

        ConfigWorker.Instance.EnsureInitialized();
        ConfigData target = ConfigWorker.Instance.CurrentConfig ?? new ConfigData();
        ApplyWorkingToConfig(target, workingConfig);

        ConfigWorker.Instance.SaveConfig(target);
        ConfigWorker.Instance.ApplyConfig(target);

        savedSnapshot = CloneConfig(target);
        EventBus.Publish(new SettingsChangedEvent(target));
    }

    void EnsureWorkingConfig()
    {
        if (workingConfig == null)
        {
            workingConfig = new ConfigData();
        }

        if (workingConfig.mixerBuses == null)
        {
            workingConfig.mixerBuses = new List<MixerBusConfig>();
        }
    }

    void ApplyWorkingToConfig(ConfigData target, ConfigData source)
    {
        if (target == null || source == null)
        {
            return;
        }

        target.mouseSensitivity = source.mouseSensitivity;
        SetBusVolume(target, masterBusPath, GetBusVolume(source, masterBusPath));
        SetBusVolume(target, sfxBusPath, GetBusVolume(source, sfxBusPath));
        SetBusVolume(target, musicBusPath, GetBusVolume(source, musicBusPath));
    }

    static ConfigData CloneConfig(ConfigData source)
    {
        if (source == null)
        {
            return null;
        }

        ConfigData clone = new ConfigData
        {
            mouseSensitivity = source.mouseSensitivity,
            mixerBuses = new List<MixerBusConfig>()
        };

        if (source.mixerBuses != null)
        {
            foreach (MixerBusConfig bus in source.mixerBuses)
            {
                if (bus == null)
                {
                    continue;
                }

                clone.mixerBuses.Add(new MixerBusConfig
                {
                    busPath = bus.busPath,
                    volume = bus.volume
                });
            }
        }

        return clone;
    }

    static float GetBusVolume(ConfigData config, string busPath)
    {
        if (config == null || config.mixerBuses == null || string.IsNullOrWhiteSpace(busPath))
        {
            return 1f;
        }

        foreach (MixerBusConfig bus in config.mixerBuses)
        {
            if (bus == null)
            {
                continue;
            }

            if (string.Equals(bus.busPath, busPath, StringComparison.OrdinalIgnoreCase))
            {
                bus.ClampVolume();
                return bus.volume;
            }
        }

        return 1f;
    }

    static void SetBusVolume(ConfigData config, string busPath, float volume)
    {
        if (config == null || string.IsNullOrWhiteSpace(busPath))
        {
            return;
        }

        if (config.mixerBuses == null)
        {
            config.mixerBuses = new List<MixerBusConfig>();
        }

        float clamped = Mathf.Clamp01(volume);
        foreach (MixerBusConfig bus in config.mixerBuses)
        {
            if (bus == null)
            {
                continue;
            }

            if (string.Equals(bus.busPath, busPath, StringComparison.OrdinalIgnoreCase))
            {
                bus.volume = clamped;
                return;
            }
        }

        config.mixerBuses.Add(new MixerBusConfig
        {
            busPath = busPath,
            volume = clamped
        });
    }
}
