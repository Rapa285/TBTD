using UnityEngine;

public sealed class TitleScreenMusicRequester : MonoBehaviour
{
    [SerializeField] private MusicIdentifier musicToRequest = MusicIdentifier.InGameA;
    [SerializeField] private bool requestOnStart = true;

    private void Start()
    {
        if (requestOnStart)
        {
            RequestMusic();
        }
    }

    public void RequestMusic()
    {
        if (ServiceLocator.TryResolve(out MusicService musicService) && musicService != null)
        {
            musicService.PlayMusic(musicToRequest);
            return;
        }

        if (ServiceLocator.TryResolve(out PersistentEventBus persistentEventBus) && persistentEventBus != null)
        {
            persistentEventBus.RaiseMusic(musicToRequest);
            return;
        }

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayMusic(musicToRequest);
            return;
        }

        Debug.LogWarning($"{nameof(TitleScreenMusicRequester)} could not find a music service to request {musicToRequest}.", this);
    }
}
