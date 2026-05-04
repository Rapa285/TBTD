using TMPro;
using UnityEditor;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    [SerializeField]
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
        float healthValue = float.Parse(healthValueText.text);
        float newHealthValue = healthValue - eventData.DamageAmount;
        newHealthValue = Mathf.Max(0f, newHealthValue);
        
        healthValueText.text = newHealthValue.ToString();
    }

}
