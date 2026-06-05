using UnityEngine;
using UnityEngine.Audio;


public class AudioClipData : ScriptableObject
{
    private static float _s_masterVolume = 1f;

    public static float MasterVolumeProperty
    {
        get { return _s_masterVolume; }
        set
        {
            if (value < 0)
            {
                Debug.LogWarning("Master volume set to a negative value. Setting volume to 0 instead.");
                _s_masterVolume = 0;
                return;
        }
            if (value > 1)
            {
                //Normalize the value
                value /= 100;
            }
            _s_masterVolume = value;
        }
    }

    [SerializeField] private AudioClip audioClip;
    [Range(0, 1)]
    [SerializeField] private float clipVolume = 1;

    // Public read-only properties for external access
    public AudioClip Clip => audioClip;
    public float Volume => clipVolume;
}
