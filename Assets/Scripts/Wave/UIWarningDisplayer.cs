using System.Collections;
using UnityEngine;
using TMPro;

public class UIWarningDisplayer : MonoBehaviour
{
    [SerializeField]
    private GameObject warningUI;
    [SerializeField]
    private CanvasGroup warningCanvasGroup;
    [SerializeField] private float displayDuration = 5f;
    [SerializeField]
    private WaveEventBus eventBus;
    private bool eventBusSubscribed;

    private Coroutine fadeCoroutine;
    [SerializeField] private float fadeDuration = 0.5f;

    void Start()
    {
        ResolveReferences();

        if (!Application.isPlaying)
        {
            return;
        }

        SubscribeToEventBus();
        
        if (warningUI != null)
        {
            warningUI.SetActive(false);
        }
    }

    public void TriggerBossWarning()
    {
        StartCoroutine(ShowWarningRoutine());
    }

    private IEnumerator ShowWarningRoutine()
    {
        warningUI.SetActive(true);
        
        // TODO: Play a warning siren audio clip here
        // AudioSource.PlayClipAtPoint(warningSiren, Camera.main.transform.position);

        // Start fading in 
        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(Fade(0f, 1f));

        yield return new WaitForSeconds(displayDuration);

        // Start fading out
        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(Fade(1f, 0f));

        yield return new WaitForSeconds(fadeDuration);

        warningUI.SetActive(false);
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

    private void HandleSpecialWave()
    {
        TriggerBossWarning();
    }

    private void ResolveReferences()
    {
        if (warningUI == null)
        {
            warningUI = GetComponent<GameObject>();
        }

        if (warningCanvasGroup == null)
        {
            warningCanvasGroup = warningUI.GetComponent<CanvasGroup>();
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

        eventBus.SpecialWave += HandleSpecialWave;
        eventBusSubscribed = true;
    }

    private void UnsubscribeFromEventBus()
    {
        if (!eventBusSubscribed || eventBus == null)
        {
            return;
        }

        eventBus.SpecialWave -= HandleSpecialWave;
        eventBusSubscribed = false;
    }

    private IEnumerator Fade(float startAlpha, float endAlpha)
    {
        float elapsedTime = 0f;
        warningCanvasGroup.alpha = startAlpha;

        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            warningCanvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, elapsedTime / fadeDuration);
            yield return null;
        }

        warningCanvasGroup.alpha = endAlpha;
    }
}
