using UnityEngine;

public class PauseManager : MonoBehaviour
{
    public bool isPaused { get; private set; }

    private void OnDisable()
    {
        if (isPaused)
        {
            TimeService.Instance.ReleasePause(this);
            isPaused = false;
        }
    }

    public void PauseGame()
    {
        if (!isPaused)
        {
            TimeService.Instance.RequestPause(this, true);
            isPaused = true;
        }
    }

    public void ResumeGame()
    {
        if (isPaused)
        {
            TimeService.Instance.ReleasePause(this);
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
}
