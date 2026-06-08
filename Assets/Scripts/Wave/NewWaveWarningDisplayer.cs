using System.Collections;
using UnityEngine;

public class NewWaveWarningDisplayer : UIWarningDisplayer
{
    [SerializeField]
    private WaveEventBus eventBus;
    private bool eventBusSubscribed;

    public void TriggerWarning()
    {
        if (!gameObject.activeInHierarchy)
        {
            return;
        }

        StartCoroutine(ShowWarningRoutine());
    }

    private void HandleNewWave(NewWaveEvent waveEvent)
    {
        if (!waveEvent.isSpecialWave)
        {
            TriggerWarning();
        }
    }

    protected override void SubscribeToEventBus()
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

        eventBus.NewWave += HandleNewWave;
        eventBusSubscribed = true;
    }

    protected override void UnsubscribeFromEventBus()
    {
        if (!eventBusSubscribed || eventBus == null)
        {
            return;
        }

        eventBus.NewWave -= HandleNewWave;
        eventBusSubscribed = false;
    }

    private void ResolveReferences()
    {
        if (eventBus == null)
        {
            ServiceLocator.TryResolve(out eventBus);
        }
    }
}
