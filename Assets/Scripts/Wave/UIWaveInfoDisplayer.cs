using UnityEngine;
using TMPro;

public class UIWaveInfoDisplayer : MonoBehaviour
{
    [SerializeField]
    private TMP_Text waveCountText;
    [SerializeField]
    private TMP_Text enemyCountText;
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
        RefreshDisplay();
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

    private void HandleNewWave(NewWaveEvent eventData)
    {
        Debug.Log($"Received NewWaveEvent: Wave {eventData.waveNumber} with {eventData.enemyCount} enemies.");
        RefreshDisplay(eventData.waveNumber, eventData.enemyCount);
    }

    private void HandleInfiniteRoundTriggered()
    {
        if (waveCountText != null)
        {
            waveCountText.text = "Infinite Round";
        }

        if (enemyCountText != null)
        {
            enemyCountText.text = "∞";
        }
     }

    private void ResolveReferences()
    {
        if (waveCountText == null)
        {
            waveCountText = GetComponent<TMP_Text>();
        }

        if (enemyCountText == null)
        {
            enemyCountText = GetComponent<TMP_Text>();
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

        eventBus.NewWave += HandleNewWave;
        eventBus.InfiniteRoundTriggered += HandleInfiniteRoundTriggered;
        eventBusSubscribed = true;
    }

    private void UnsubscribeFromEventBus()
    {
        if (!eventBusSubscribed || eventBus == null)
        {
            return;
        }

        eventBus.NewWave -= HandleNewWave;
        eventBus.InfiniteRoundTriggered -= HandleInfiniteRoundTriggered;
        eventBusSubscribed = false;
    }

    private void RefreshDisplay()
    {
        ResolveReferences();
        RefreshDisplay(0, 0);
    }

    private void RefreshDisplay(int waveCount, int enemyCount)
    {
        Debug.Log($"Refreshing UIWaveInfoDisplayer: Wave {waveCount}, Enemies {enemyCount}");
        if (waveCountText != null)
        {
            waveCountText.text = $"{waveCount}";
        }

        if (enemyCountText != null)
        {
            enemyCountText.text = $"{enemyCount}";
        }
    }
}
