using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
public class GameOverPanel : MonoBehaviour
{
    public GameObject gameOverPanel;
    public Button gameOverPanelExitButton;
    public Button gameOverPanelRetryButton;
    private TimeService timeService;

    private bool showingPanel = false;

    private void Awake()
    {
        gameOverPanelExitButton.onClick.RemoveAllListeners();
        gameOverPanelExitButton.onClick.AddListener(exit);

        gameOverPanelRetryButton.onClick.RemoveAllListeners();
        gameOverPanelRetryButton.onClick.AddListener(retry);

        ResolveTimeService();
    }

    private void OnEnable()
    {
        GeneralEventBus<GameOverEvent>.Subscribe(ShowGameOverPanel);
    }

    private void OnDisable()
    {
        GeneralEventBus<GameOverEvent>.Unsubscribe(ShowGameOverPanel);
    }

    private void ResolveTimeService()
    {
        if (timeService == null)
        {
            ServiceLocator.TryResolve(out timeService);
        }
    }

    private void ShowGameOverPanel(GameOverEvent eventData)
    {
        ResolveTimeService();
        if (timeService != null)
        {
            timeService.RequestPause(this, true);
        }
        gameOverPanel.SetActive(true);
        showingPanel = true;
    }

    private void retry()
    {
        GeneralEventBus<RetryGameEvent>.Publish(new RetryGameEvent{});
    }

    private void exit()
    {
        GeneralEventBus<ExitToMainMenuEvent>.Publish(new ExitToMainMenuEvent{});
    }
}