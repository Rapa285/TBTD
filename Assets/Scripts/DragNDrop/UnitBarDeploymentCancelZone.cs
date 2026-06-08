using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Cancels an active deployment preview when the pointer re-enters the unit bar.
/// </summary>
[DisallowMultipleComponent]
public class UnitBarDeploymentCancelZone : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField, Tooltip("Deployment controller that owns the active preview. Resolved from services when unset.")]
    private UnitDeploymentController deploymentController;

    [SerializeField, Tooltip("Optional root shown over the unit bar while deployment preview cancellation is available.")]
    private GameObject cancelOverlayRoot;

    [SerializeField, Min(0f), Tooltip("Delay before the cancel hint overlay begins fading in.")]
    private float hintShowDelay = 0.5f;

    [SerializeField, Min(0f), Tooltip("Duration used when fading the cancel hint overlay in or out.")]
    private float hintFadeDuration = 0.5f;

    private bool isPointerInside;
    private readonly DelayedOverlayFader cancelOverlayFader = new DelayedOverlayFader();
    private UnitEventBus eventBus;
    private bool subscribedToEventBus;

    public bool IsPointerInside => isPointerInside;

    private void Awake()
    {
        ResolveDeploymentController();
        ForceHideCancelOverlay();
    }

    private void OnEnable()
    {
        ResolveDeploymentController();
        SubscribeToEventBus();
        RefreshCancelOverlay();
    }

    private void Update()
    {
        if (!Application.isPlaying || subscribedToEventBus)
        {
            return;
        }

        SubscribeToEventBus();
        RefreshCancelOverlay();
    }

    private void OnDisable()
    {
        isPointerInside = false;
        ForceHideCancelOverlay();
        UnsubscribeFromEventBus();
    }

    private void OnDestroy()
    {
        ForceHideCancelOverlay();
        UnsubscribeFromEventBus();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        bool wasPointerInside = isPointerInside;
        isPointerInside = true;

        if (!Application.isPlaying || wasPointerInside)
        {
            return;
        }

        UnitDeploymentController resolvedDeploymentController = ResolveDeploymentController();
        if (resolvedDeploymentController != null && resolvedDeploymentController.IsDragging)
        {
            resolvedDeploymentController.CancelDeployment();
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isPointerInside = false;
    }

    private void HandleUnitDeploymentPreviewStarted(UnitDeploymentPreviewStartedEvent eventData)
    {
        SetCancelOverlayVisible(true);
    }

    private void HandleUnitDeploymentPreviewEnded(UnitDeploymentPreviewEndedEvent eventData)
    {
        SetCancelOverlayVisible(false);
    }

    private UnitDeploymentController ResolveDeploymentController()
    {
        if (deploymentController == null)
        {
            ServiceLocator.TryResolve(out deploymentController);
        }

        return deploymentController;
    }

    private void SubscribeToEventBus()
    {
        if (subscribedToEventBus)
        {
            return;
        }

        if (eventBus == null)
        {
            ServiceLocator.TryResolve(out eventBus);
        }

        if (eventBus == null)
        {
            return;
        }

        eventBus.UnitDeploymentPreviewStarted += HandleUnitDeploymentPreviewStarted;
        eventBus.UnitDeploymentPreviewEnded += HandleUnitDeploymentPreviewEnded;
        subscribedToEventBus = true;
    }

    private void UnsubscribeFromEventBus()
    {
        if (!subscribedToEventBus || eventBus == null)
        {
            return;
        }

        eventBus.UnitDeploymentPreviewStarted -= HandleUnitDeploymentPreviewStarted;
        eventBus.UnitDeploymentPreviewEnded -= HandleUnitDeploymentPreviewEnded;
        subscribedToEventBus = false;
    }

    private void RefreshCancelOverlay()
    {
        UnitDeploymentController resolvedDeploymentController = ResolveDeploymentController();
        SetCancelOverlayVisible(resolvedDeploymentController != null && resolvedDeploymentController.IsDragging);
    }

    private void SetCancelOverlayVisible(bool isVisible)
    {
        cancelOverlayFader.RequestVisible(cancelOverlayRoot, isVisible, hintShowDelay, hintFadeDuration);
    }

    private void ForceHideCancelOverlay()
    {
        cancelOverlayFader.ForceHide(cancelOverlayRoot);
    }
}

internal sealed class DelayedOverlayFader
{
    private GameObject root;
    private CanvasGroup canvasGroup;
    private Tween activeTween;
    private bool requestedVisible;

    public void RequestVisible(GameObject overlayRoot, bool visible, float showDelay, float duration)
    {
        SetRoot(overlayRoot);
        if (root == null)
        {
            return;
        }

        EnsureCanvasGroup();
        if (requestedVisible == visible)
        {
            return;
        }

        requestedVisible = visible;
        KillActiveTween();

        if (visible)
        {
            BeginDelayedShow(Mathf.Max(0f, showDelay), Mathf.Max(0f, duration));
            return;
        }

        BeginHide(Mathf.Max(0f, duration));
    }

    public void ForceHide(GameObject overlayRoot)
    {
        SetRoot(overlayRoot);
        if (root == null)
        {
            return;
        }

        EnsureCanvasGroup();
        KillActiveTween();
        requestedVisible = false;
        canvasGroup.alpha = 0f;
        root.SetActive(false);
    }

    private void SetRoot(GameObject overlayRoot)
    {
        if (root == overlayRoot)
        {
            return;
        }

        KillActiveTween();
        root = overlayRoot;
        canvasGroup = null;
        requestedVisible = false;
    }

    private void BeginDelayedShow(float showDelay, float duration)
    {
        canvasGroup.alpha = 0f;
        root.SetActive(false);

        if (showDelay > 0f)
        {
            activeTween = DOVirtual.DelayedCall(showDelay, () => BeginFadeIn(duration), false)
                .SetUpdate(true);
            return;
        }

        BeginFadeIn(duration);
    }

    private void BeginHide(float duration)
    {
        if (!root.activeSelf || canvasGroup.alpha <= 0f)
        {
            canvasGroup.alpha = 0f;
            root.SetActive(false);
            return;
        }

        if (duration <= 0f)
        {
            canvasGroup.alpha = 0f;
            root.SetActive(false);
            return;
        }

        activeTween = canvasGroup
            .DOFade(0f, duration)
            .SetUpdate(true)
            .OnComplete(() =>
            {
                if (!requestedVisible && root != null)
                {
                    root.SetActive(false);
                }
            });
    }

    private void BeginFadeIn(float duration)
    {
        if (!requestedVisible || root == null || canvasGroup == null)
        {
            return;
        }

        ConfigureCanvasGroup();
        root.SetActive(true);

        if (duration <= 0f)
        {
            canvasGroup.alpha = 1f;
            return;
        }

        canvasGroup.alpha = 0f;
        activeTween = canvasGroup
            .DOFade(1f, duration)
            .SetUpdate(true);
    }

    private void EnsureCanvasGroup()
    {
        if (canvasGroup == null && root != null)
        {
            canvasGroup = root.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = root.AddComponent<CanvasGroup>();
            }
        }

        ConfigureCanvasGroup();
    }

    private void ConfigureCanvasGroup()
    {
        if (canvasGroup == null)
        {
            return;
        }

        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }

    private void KillActiveTween()
    {
        if (activeTween == null)
        {
            return;
        }

        activeTween.Kill();
        activeTween = null;
    }
}
