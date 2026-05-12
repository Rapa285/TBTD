using UnityEngine;
using UnityEngine.Splines;
using System.Collections;

public class WaveLineAnimation : MonoBehaviour
{
    public SplineAnimate animator;
    public TrailRenderer trail;
    public float delaySeconds = 2.0f;

    [SerializeField]
    private WaveEventBus eventBus;
    private bool eventBusSubscribed;

    private bool isLooping = false;

    void Start()
    {
        StartLooping();
    }

    private IEnumerator LoopRoutine()
    {
        while (isLooping)
        {
            trail.emitting = true;
            animator.Restart(false);
            trail.Clear();
            animator.Play();

            yield return new WaitUntil(() => animator.NormalizedTime >= 1.0f);

            trail.emitting = false;

            // Delay loop
            yield return new WaitForSeconds(delaySeconds);
        }

        // Disable object
        Debug.Log("WaveLineAnimation: Looping ended, disabling object.");
        gameObject.SetActive(false);
    }

    public void StartLooping()
    {
        if (!isLooping)
        {
            isLooping = true;
            StartCoroutine(LoopRoutine());
        }
    }

    public void StopLooping()
    {
        isLooping = false;
    }

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
        StopLooping();
    }

    private void ResolveReferences()
    {
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
        eventBusSubscribed = true;
    }

    private void UnsubscribeFromEventBus()
    {
        if (!eventBusSubscribed || eventBus == null)
        {
            return;
        }

        eventBus.GraceTimerEnded -= HandleGraceTimerEnded;
        eventBusSubscribed = false;
    }
}
