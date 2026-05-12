using UnityEngine;
using TMPro;

public class UIGraceTimerDisplayer : MonoBehaviour
{
    [SerializeField]
    private TMP_Text graceTimerText;
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

    private void HandleGraceTimerEnded()
    {
        gameObject.SetActive(false);
    }

    private void HandleGraceTimerTick(GraceTimerTickEvent eventData)
    {
        if (graceTimerText != null)
        {
            int seconds = Mathf.CeilToInt(eventData.timeRemaining);
            graceTimerText.text = $"{seconds}";
        }
    }

    private void ResolveReferences()
    {
        if (graceTimerText == null)
        {
            graceTimerText = GetComponent<TMP_Text>();
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

        eventBus.GraceTimerEnded += HandleGraceTimerEnded;
        eventBus.GraceTimerTick += HandleGraceTimerTick;
        eventBusSubscribed = true;
    }

    private void UnsubscribeFromEventBus()
    {
        if (!eventBusSubscribed || eventBus == null)
        {
            return;
        }

        eventBus.GraceTimerEnded -= HandleGraceTimerEnded;
        eventBus.GraceTimerTick -= HandleGraceTimerTick;
        eventBusSubscribed = false;
    }
}
