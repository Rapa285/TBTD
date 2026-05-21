using UnityEngine;
using UnityEngine.UI;

public class SettingsPanel : MonoBehaviour
{
    public GameObject settingsPanel;
    public Button settingsButton;
    public Button settingsPanelHomeButton;
    public Button settingsPanelRetryButton;


    public Button settingsPanelCloseButton;
    public Button settingsPanelContinueButton;

    private bool showingPanel = false;
    private TimeService timeService;

    private void Awake()
    {
        settingsButton.onClick.RemoveAllListeners();
        settingsPanelCloseButton.onClick.RemoveAllListeners();
        settingsPanelHomeButton.onClick.RemoveAllListeners();
        settingsPanelRetryButton.onClick.RemoveAllListeners();
        settingsPanelContinueButton.onClick.RemoveAllListeners();


        settingsButton.onClick.AddListener(showPanel);
        settingsPanelCloseButton.onClick.AddListener(closePanel);
        settingsPanelHomeButton.onClick.AddListener(HomeButtonClicked);
        settingsPanelRetryButton.onClick.AddListener(RetryButtonClicked);
        settingsPanelContinueButton.onClick.AddListener(ContinueButtonClicked);
    }

    private void OnEnable()
    {
        // GeneralEventBus<>
    }

    private void OnDisable()
    {
        if (showingPanel)
        {
            ResolveTimeService();
            if (timeService != null)
            {
                timeService.ReleasePause(this);
            }
            showingPanel = false;
        }
    }

    private void showPanel()
    {
        ResolveTimeService();
        if (timeService != null)
        {
            timeService.RequestPause(this, true);
        }

        settingsPanel.SetActive(true);
        settingsButton.interactable = false;
        showingPanel = true;
    }

    private void closePanel()
    {
        ResolveTimeService();
        if (timeService != null)
        {
            timeService.ReleasePause(this);
        }

        settingsPanel.SetActive(false);
        settingsButton.interactable = true;
        showingPanel = false;
    }

    private void HomeButtonClicked()
    {
        ResolveTimeService();
        if (timeService != null)
        {
            timeService.ReleasePause(this);
        }
        GeneralEventBus<ExitToMainMenuEvent>.Publish(new ExitToMainMenuEvent { });
    }

    private void RetryButtonClicked()
    {
        ResolveTimeService();
        if (timeService != null)
        {
            timeService.ReleasePause(this);
        }
        GeneralEventBus<RetryGameEvent>.Publish(new RetryGameEvent { });
    }

    private void ContinueButtonClicked()
    {
        ResolveTimeService();
        if (timeService != null)
        {
            timeService.ReleasePause(this);
        }
        settingsPanel.SetActive(false);
        settingsButton.interactable = true;
        showingPanel = false;
    }

    private void ResolveTimeService()
    {
        if (timeService == null)
        {
            ServiceLocator.TryResolve(out timeService);
        }
    }}
