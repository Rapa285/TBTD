using UnityEngine;

public sealed class MusicPlayer : MonoBehaviour
{
    [SerializeField] private MusicIdentifier musicToPlay = MusicIdentifier.MainMenuBGM;
    [SerializeField] private bool playOnEnable = true;

    private void OnEnable()
    {
        if (playOnEnable)
        {
            Play();
        }
    }

    public void Play()
    {
        if (ServiceLocator.TryResolve(out PersistentEventBus persistentEventBus) && persistentEventBus != null)
        {
            persistentEventBus.RaiseMusic(musicToPlay);
            return;
        }

        if (ServiceLocator.TryResolve(out MusicService musicService) && musicService != null)
        {
            musicService.PlayMusic(musicToPlay);
            return;
        }

        Debug.LogWarning($"{nameof(MusicPlayer)} could not find a {nameof(PersistentEventBus)} or {nameof(MusicService)}.", this);
    }
}
