using System;
using UnityEngine;

public readonly struct UISFXRequestEvent
{
    public readonly UISFXID SfxId;
    public readonly bool RandomizePitch;
    public readonly float VolumeScale;

    public UISFXRequestEvent(UISFXID sfxId, bool randomizePitch, float volumeScale)
    {
        SfxId = sfxId;
        RandomizePitch = randomizePitch;
        VolumeScale = volumeScale;
    }
}

public readonly struct MusicRequestEvent
{
    public readonly MusicIdentifier MusicId;

    public MusicRequestEvent(MusicIdentifier musicId)
    {
        MusicId = musicId;
    }
}

[DefaultExecutionOrder(-900)]
public sealed class PersistentEventBus : MonoBehaviour
{
    public event Action<UISFXRequestEvent> UISFXRequested;
    public event Action<MusicRequestEvent> MusicRequested;
    public event Action MusicStopRequested;

    private void Awake()
    {
        RegisterWithServiceLocator();
    }

    private void OnDestroy()
    {
        ServiceLocator.Unregister<PersistentEventBus>(this);
    }

    public void RaiseUISFX(UISFXID id, bool randomizePitch = false, float volumeScale = 1f)
    {
        UISFXRequested?.Invoke(new UISFXRequestEvent(id, randomizePitch, volumeScale));
    }

    public void RaiseMusic(MusicIdentifier id)
    {
        MusicRequested?.Invoke(new MusicRequestEvent(id));
    }

    public void RaiseStopMusic()
    {
        MusicStopRequested?.Invoke();
    }

    private void RegisterWithServiceLocator()
    {
        if (ServiceLocator.TryResolve<PersistentEventBus>(out PersistentEventBus existingEventBus)
            && existingEventBus != null
            && existingEventBus != this)
        {
            Debug.LogWarning(
                $"{nameof(PersistentEventBus)} on '{name}' replaced the previously registered {nameof(PersistentEventBus)} on '{existingEventBus.name}'.",
                this);
        }

        ServiceLocator.Register<PersistentEventBus>(this);
    }
}
