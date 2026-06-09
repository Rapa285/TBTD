
using System;
using System.Collections.Generic;
using UnityEngine;

public enum TransitionOffsetMode
{
    None,
    Relative,
    Inverted
}

[CreateAssetMenu(fileName = "MusicClip", menuName = "Audio/Music", order = 0)]
public class MusicClipData : AudioClipData
{
    [SerializeField] private MusicIdentifier musicNameEnum;

    [SerializeField] private bool loop;

    [SerializeField] private float delay = 0f;
    [SerializeField] private float exitDelay = 0f;

    public float ExitDelay => exitDelay;

    [Header("Transition")]
    public int bpm;
    public TransitionOffsetMode transitionOffset = TransitionOffsetMode.None;
    public int beatsPerBar = 4;
    public float SecondsPerBeat => 60f / bpm;
    public float SecondsPerBar => SecondsPerBeat * beatsPerBar;


    public bool IsLooping
    {
        get
        {
            return loop;
        }
    }


    public MusicIdentifier MusicId
    {
        get
        {
            return musicNameEnum;
        }
    }

    public float Delay
    {
        get
        {
            return delay;
        }
    }

    #region Static Methods


    private static float _s_music_masterVolume = 1;
    public static float MusicMasterVolumeProperty
    {
        get
        {
            return _s_music_masterVolume;
        }
        set
        {
            if (value <= 0)
            {
                Debug.LogWarning("Music master set to under negative number. Setting volume to 0 instead");
                _s_music_masterVolume = 0;
                return;
            }
            if (value > 1)
            {
                value /= 100;
            }
            _s_music_masterVolume = value;
        }

    }

    private static Dictionary<MusicIdentifier, MusicClipData> _musicRegistry = new();

    public static Dictionary<MusicIdentifier, MusicClipData> MusicRegistry
    {
        get
        {
            return _musicRegistry;
        }
        set
        {
            foreach (var item in value)
            {
                if (_musicRegistry.ContainsKey(item.Key))
                {
                    Debug.LogWarning("SFXClipData with id " + item.Key + " is Already Registered. Overwriting it.");
                    _musicRegistry[item.Key] = item.Value;
                }

                else
                {
                    _musicRegistry.Add(item.Key, item.Value);
                }
            }
        }
    }

    // Static method to retrieve a MusicClipData instance by its MusicIdentifier.
    public static MusicClipData GetMusicClipDataById(MusicIdentifier id)
    {
        if (_musicRegistry.TryGetValue(id, out MusicClipData data))
        {
            return data;
        }
        Debug.LogWarning("No MusicClipData found for id: " + id);
        return null;
    }

    public static float CalculateMusicVolume(float clipVolume)
    {
        float calculatedVolume = MasterVolumeProperty * MusicMasterVolumeProperty * clipVolume;
        if (calculatedVolume == 0)
        {
            Debug.LogWarning("Music Volume Player is 0");
        }
        return calculatedVolume;
    }
    #endregion
}