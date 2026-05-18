using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
public class GameOverPanel : MonoBehaviour
{
    public GameObject gameOverPanel;
    public Button gameOverPanelExitButton;
    public Button gameOverPanelRetryButton;


    private bool showingPanel = false;

    private void Awake()
    {
        gameOverPanelExitButton.onClick.RemoveAllListeners();
        gameOverPanelExitButton.onClick.AddListener(exit);

        gameOverPanelRetryButton.onClick.RemoveAllListeners();
        gameOverPanelRetryButton.onClick.AddListener(retry);

        
    }

    private void OnEnable()
    {
        GeneralEventBus<GameOverEvent>.Subscribe(ShowGameOverPanel);
    }

    private void OnDisable()
    {
        GeneralEventBus<GameOverEvent>.Unsubscribe(ShowGameOverPanel);
    }

    private void ShowGameOverPanel(GameOverEvent eventData)
    {
        gameOverPanel.SetActive(true);
        showingPanel = true;
    }

    private void closeGameOverPanel()
    {
        gameOverPanel.SetActive(false);
        showingPanel = false;

        // call event to change scene to main menu or sumthn
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