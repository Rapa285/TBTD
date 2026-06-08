using System.Collections;
using UnityEngine;

public class SpecialWaveWarningDisplayer : UIWarningDisplayer
{
    [SerializeField]
    private WaveEventBus eventBus;
    private bool eventBusSubscribed;

    public void TriggerBossWarning()
    {
        if (!gameObject.activeInHierarchy)
        {
            return;
        }

        StartCoroutine(ShowWarningRoutine());
    }

    private void HandleSpecialWave()
    {
        TriggerBossWarning();
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

        eventBus.SpecialWave += HandleSpecialWave;
        eventBusSubscribed = true;
    }

    protected override void UnsubscribeFromEventBus()
    {
        if (!eventBusSubscribed || eventBus == null)
        {
            return;
        }

        eventBus.SpecialWave -= HandleSpecialWave;
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
