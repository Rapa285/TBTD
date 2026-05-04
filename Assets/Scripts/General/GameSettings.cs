using TMPro;
using UnityEditor;
using UnityEngine;

public class GameSettings : MonoBehaviour
{
    [SerializeField]

    public GameObject settingsPanel;
    public GameObject settingsButton;

    public GameObject settings;

    private void OnEnable()
    {
        // Mulai mendengarkan event BaseDamagedEvent
        GeneralEventBus<SettingsEvent>.Subscribe(ShowSettingsPanel);

    }

    private void OnDisable()
    {
        // Mulai mendengarkan event BaseDamagedEvent
        GeneralEventBus<SettingsEvent>.Subscribe(ShowSettingsPanel);

    }


    private void ShowSettingsPanel(SettingsEvent eventData)
    {
        // settingsButton
        settingsPanel.SetActive(true);
    }

    private void CloseSettingsPanel()
    {
        
    }

}
