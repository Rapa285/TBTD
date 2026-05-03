using UnityEngine;
using TMPro;

public class UIWaveDisplayer : MonoBehaviour
{
    [SerializeField]
    private TMP_Text waveCountText;
    [SerializeField]
    private TMP_Text enemyCountText;
    private WaveEventBus eventBus;
    private bool eventBusSubscribed;

    private void Awake()
    {
        ResolveReferences();
        RefreshDisplay();
    }

    private void Start()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        SubscribeToEventBus();
        RefreshDisplay();
    }

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
        RefreshDisplay(eventData.waveNumber, eventData.enemyCount);
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
        eventBusSubscribed = true;
    }

    private void UnsubscribeFromEventBus()
    {
        if (!eventBusSubscribed || eventBus == null)
        {
            return;
        }

        eventBus.NewWave -= HandleNewWave;
        eventBusSubscribed = false;
    }

    private void RefreshDisplay()
    {
        ResolveReferences();
        RefreshDisplay(0, 0);
    }

    private void RefreshDisplay(int waveCount, int enemyCount)
    {
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
