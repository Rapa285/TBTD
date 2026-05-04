using TMPro;
using UnityEditor;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    [SerializeField]
    public TextMeshProUGUI healthValueText;

    public GameObject settingsPanel;
    public GameObject settingsButton;

    public GameObject settings;
    public GameObject gameOverPanel;

    private void OnEnable()
    {
        // Mulai mendengarkan event BaseDamagedEvent
        GeneralEventBus<BaseDamagedEvent>.Subscribe(DamageBase);
        GeneralEventBus<GameOverEvent>.Subscribe(ShowGameOverUI);
        GeneralEventBus<SettingsEvent>.Subscribe(ShowSettingsUI);

    }

    private void OnDisable()
    {
        // Mulai mendengarkan event BaseDamagedEvent
        GeneralEventBus<BaseDamagedEvent>.Unsubscribe(DamageBase);
        GeneralEventBus<GameOverEvent>.Unsubscribe(ShowGameOverUI);

    }

    private void DamageBase(BaseDamagedEvent eventData)
    {
        float healthValue = float.Parse(healthValueText.text);
        float newHealthValue = healthValue - eventData.DamageAmount;
        newHealthValue = Mathf.Max(0f, newHealthValue);
        
        healthValueText.text = newHealthValue.ToString();
    }

    private void ShowSettingsUI(SettingsEvent eventData)
    {
        settingsPanel.SetActive(true);
    }

    private void ShowGameOverUI(GameOverEvent eventData)
    {
        gameOverPanel.SetActive(true);
    }

}
