using UnityEngine;
using TMPro;

public class UIWaveDisplayer : MonoBehaviour
{
    [SerializeField]
    private TMP_Text waveTimerText;
    [SerializeField]
    private WaveEventBus eventBus;
    private bool eventBusSubscribed;

    private void OnEnable()
    {
        ResolveReferences();

        if (!Application.isPlaying)
        {
            return;
        }

        SubscribeToEventBus();
    }

    private void OnDisable()
    {
        UnsubscribeFromEventBus();
    }

    private void OnDestroy()
    {
        UnsubscribeFromEventBus();
    }

    private void OnValidate()
    {
        ResolveReferences();
    }

    private void HandleWaveTimerTick(WaveTimerTickEvent eventData)
    {
        if (waveTimerText != null)
        {
            int seconds = Mathf.CeilToInt(eventData.timeRemaining);
            waveTimerText.text = $"{seconds}";
        }
    }

    private void ResolveReferences()
    {
        if (waveTimerText == null)
        {
            waveTimerText = GetComponent<TMP_Text>();
        }

        if (eventBus == null)
        {
            ServiceLocator.TryResolve(out eventBus);
        }
    }

    private void SubscribeToEventBus()
    {
        if (eventBusSubscribed)
        {
            return;
        }

        if (eventBus == null)
        {
            ResolveReferences();
        }

        if (eventBus == null)
        {
            return;
        }

        eventBus.WaveTimerTick += HandleWaveTimerTick;
        eventBusSubscribed = true;
    }

    private void UnsubscribeFromEventBus()
    {
        if (!eventBusSubscribed || eventBus == null)
        {
            return;
        }

        eventBus.WaveTimerTick -= HandleWaveTimerTick;
        eventBusSubscribed = false;
    }
}
