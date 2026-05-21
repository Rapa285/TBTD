using UnityEngine;

public class PauseManager : MonoBehaviour
{
    public bool isPaused { get; private set; }
    private TimeService timeService;

    private void OnDisable()
    {
        if (isPaused)
        {
            ResolveTimeService();
            if (timeService != null)
            {
                timeService.ReleasePause(this);
            }
            isPaused = false;
        }
    }

    public void PauseGame()
    {
        if (!isPaused)
        {
            ResolveTimeService();
            if (timeService != null)
            {
                timeService.RequestPause(this, true);
            }
            isPaused = true;
        }
    }

    public void ResumeGame()
    {
        if (isPaused)
        {
            ResolveTimeService();
            if (timeService != null)
            {
                timeService.ReleasePause(this);
            }
            isPaused = false;
        }
    }

    public void TogglePause()
    {
        if (isPaused)
        {
            ResumeGame();
        }
        else
        {
            PauseGame();
        }
    }

    private void ResolveTimeService()
    {
        if (timeService == null)
        {
            ServiceLocator.TryResolve(out timeService);
        }
    }
}
