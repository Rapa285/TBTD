using System;
using System.Collections.Generic;

[Serializable]
public sealed class ConfigData
{
    public float mouseSensitivity = 0.15f;
    public List<MixerBusConfig> mixerBuses = new List<MixerBusConfig>();
}

[Serializable]
public sealed class MixerBusConfig
{
    public string busPath = string.Empty;
    public float volume = 1f;

    public void ClampVolume()
    {
        volume = UnityEngine.Mathf.Clamp01(volume);
    }
}
