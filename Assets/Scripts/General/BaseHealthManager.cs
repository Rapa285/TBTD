using UnityEditor;
using UnityEngine;
using TMPro;

public class BaseHealthManager : MonoBehaviour
{
    [SerializeField]
    private float baseHealth = 100f;
    public TextMeshProUGUI healthValueText;

    [SerializeField]
    private bool infinitHealth = false;

    private void Start()
    {
        healthValueText.text = baseHealth.ToString();
    }
    
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
        if (infinitHealth) return;
        baseHealth -= eventData.DamageAmount;
        if (baseHealth <= 0)
        {
            healthValueText.text = "0";
            GeneralEventBus<GameOverEvent>.Publish(new GameOverEvent{});
            return;
        }
        healthValueText.text = baseHealth.ToString();

    }


}
