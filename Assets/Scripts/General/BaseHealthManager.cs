using UnityEditor;
using UnityEngine;
using TMPro;

public class BaseHealthManager : MonoBehaviour
{
    [SerializeField]
    private float baseHealth = 100f;
    public TextMeshProUGUI healthValueText;

    private void OnEnable()
    {
        // Mulai mendengarkan event BaseDamagedEvent
        GeneralEventBus<BaseDamagedEvent>.Subscribe(DamageBase);
    }

    private void OnDisable()
    {
        // Mulai mendengarkan event BaseDamagedEvent
        GeneralEventBus<BaseDamagedEvent>.Unsubscribe(DamageBase);

    }

    private void DamageBase(BaseDamagedEvent eventData)
    {
        baseHealth -= eventData.DamageAmount;
        healthValueText.text = baseHealth.ToString();
        if (baseHealth <= 0)
        {
            healthValueText.text = "0";
            GeneralEventBus<GameOverEvent>.Publish(new GameOverEvent{});
        }
    }


}
