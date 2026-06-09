using System.Collections;
using UnityEngine;

public abstract class UIWarningDisplayer : MonoBehaviour
{
    [SerializeField]
    private GameObject warningUI;
    [SerializeField]
    private CanvasGroup warningCanvasGroup;
    [SerializeField] private float displayDuration = 5f;

    [SerializeField] private UISFXID warningSfx = UISFXID.WaveAlert;

    private Coroutine fadeCoroutine;
    [SerializeField] private float fadeDuration = 0.5f;

    protected abstract void SubscribeToEventBus();

    protected abstract void UnsubscribeFromEventBus();

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

    protected IEnumerator ShowWarningRoutine()
    {
        ResolveReferences();

        if (warningUI != null)
        {
            warningUI.SetActive(true);
        }

        RequestWarningSFX();

        // Start fading in 
        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        if (warningCanvasGroup != null)
        {
            fadeCoroutine = StartCoroutine(Fade(0f, 1f));
        }

        yield return new WaitForSeconds(displayDuration);

        // Start fading out
        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        if (warningCanvasGroup != null)
        {
            fadeCoroutine = StartCoroutine(Fade(1f, 0f));
        }

        yield return new WaitForSeconds(fadeDuration);

        if (warningUI != null)
        {
            warningUI.SetActive(false);
        }
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

    private void ResolveReferences()
    {
        if (warningUI == null)
        {
            warningUI = gameObject;
        }

        if (warningCanvasGroup == null && warningUI != null)
        {
            warningCanvasGroup = warningUI.GetComponent<CanvasGroup>();
        }
    }

    private void RequestWarningSFX()
    {
        if (warningSfx == UISFXID.None)
        {
            return;
        }

        if (ServiceLocator.TryResolve(out PersistentEventBus persistentEventBus) && persistentEventBus != null)
        {
            persistentEventBus.RaiseUISFX(warningSfx);
            return;
        }

        if (ServiceLocator.TryResolve(out UISFXService uiSfxService) && uiSfxService != null)
        {
            uiSfxService.PlayUISFX(warningSfx);
        }
    }

    private IEnumerator Fade(float startAlpha, float endAlpha)
    {
        if (warningCanvasGroup == null)
        {
            yield break;
        }

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
