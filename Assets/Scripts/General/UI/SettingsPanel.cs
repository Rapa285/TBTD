using UnityEngine;
using UnityEngine.UI;

public class SettingsPanel : MonoBehaviour
{
    public GameObject settingsPanel;
    public Button settingsButton;
    public Button settingsPanelCloseButton;

    private bool showingPanel = false;

    private void Awake()
    {
        settingsButton.onClick.RemoveAllListeners();
        settingsPanelCloseButton.onClick.RemoveAllListeners();


        settingsButton.onClick.AddListener(showPanel);
        settingsPanelCloseButton.onClick.AddListener(closePanel);
    }

    private void OnEnable()
    {
        // GeneralEventBus<>
    }

    private void OnDisable()
    {
        if (showingPanel)
        {
            TimeService.Instance.ReleasePause(this);
            showingPanel = false;
        }
    }

    private void showPanel()
    {
        TimeService.Instance.RequestPause(this, true);

        settingsPanel.SetActive(true);
        settingsButton.interactable = false;
        showingPanel = true;
    }

    private void closePanel()
    {
        TimeService.Instance.ReleasePause(this);

        settingsPanel.SetActive(false);
        settingsButton.interactable = true;
        showingPanel = false;

    }
}
