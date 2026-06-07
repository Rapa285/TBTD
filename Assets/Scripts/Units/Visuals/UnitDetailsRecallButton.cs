using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Recall button for the currently selected roster-managed unit in the unit details menu.
/// </summary>
public sealed class UnitDetailsRecallButton : RecallHoldButtonBase
{
    [SerializeField, Tooltip("Button players hold to recall the selected deployed unit.")]
    private Button recallButton;

    [SerializeField, Tooltip("Optional root shown and hidden with recall availability. Defaults to the recall button object.")]
    private GameObject recallButtonRoot;

    [SerializeField, Tooltip("Optional visual fill controller for the recall hold progress.")]
    private RecallButtonFX recallButtonFX;

    [SerializeField, Tooltip("Player state source used to read the selected unit. Uses the scene service locator when empty.")]
    private PlayerStateController playerStateController;

    [SerializeField, Tooltip("Roster state source used to recall selected managed units. Uses the scene service locator when empty.")]
    private UnitStateManager unitStateManager;

    private RecallPointerForwarder recallPointerForwarder;
    private CanvasGroup selfVisibilityGroup;
    private bool subscribedToPlayerState;

    protected override void ResolveReferences()
    {
        base.ResolveReferences();

        if (recallButton == null)
        {
            recallButton = GetComponentInChildren<Button>(true);
        }

        if (recallButtonRoot == null && recallButton != null)
        {
            recallButtonRoot = recallButton.gameObject;
        }

        if (recallButtonFX == null)
        {
            recallButtonFX = GetComponentInChildren<RecallButtonFX>(true);
        }

        if (playerStateController == null)
        {
            ServiceLocator.TryResolve(out playerStateController);
        }

        if (unitStateManager == null)
        {
            ServiceLocator.TryResolve(out unitStateManager);
        }

        EnsurePointerForwarder();
    }

    protected override void Subscribe()
    {
        base.Subscribe();
        SubscribeToPlayerState();
    }

    protected override void Unsubscribe()
    {
        UnsubscribeFromPlayerState();
        base.Unsubscribe();
    }

    protected override bool TryGetRecallTarget(out string unitId, out UnitStateManager stateManager, out TowerEntity tower)
    {
        ResolveReferences();

        unitId = playerStateController != null ? playerStateController.SelectedUnitId : null;
        stateManager = unitStateManager;
        tower = null;

        if (string.IsNullOrWhiteSpace(unitId) || stateManager == null)
        {
            return false;
        }

        if (!stateManager.TryGetUnit(unitId, out UnitStateManager.OwnedUnitState unit) || !unit.IsDeployed)
        {
            return false;
        }

        tower = unit.CurrentRuntimeInstance;
        return tower != null;
    }

    protected override bool ShouldRefreshForUnit(string unitId)
    {
        return base.ShouldRefreshForUnit(unitId)
            || playerStateController != null
            && !string.IsNullOrWhiteSpace(unitId)
            && playerStateController.SelectedUnitId == unitId;
    }

    protected override void SetRecallAvailable(bool isAvailable)
    {
        base.SetRecallAvailable(isAvailable);

        GameObject target = GetVisibilityTarget();
        if (target == null)
        {
            return;
        }

        if (target == gameObject)
        {
            SetSelfVisible(isAvailable);
        }
        else if (target.activeSelf != isAvailable)
        {
            target.SetActive(isAvailable);
        }

        if (recallButton != null)
        {
            recallButton.interactable = isAvailable;
        }
    }

    protected override bool IsRecallVisible()
    {
        GameObject target = GetVisibilityTarget();
        if (target == null)
        {
            return false;
        }

        if (target == gameObject && selfVisibilityGroup != null)
        {
            return selfVisibilityGroup.alpha > 0f && selfVisibilityGroup.blocksRaycasts;
        }

        return target.activeInHierarchy;
    }

    protected override bool IsRecallInteractable()
    {
        return recallButton != null && recallButton.IsInteractable();
    }

    protected override void HandleHoldStarted(float duration)
    {
        recallButtonFX?.BeginHold(duration);
    }

    protected override void HandleHoldProgress(float normalizedProgress)
    {
        recallButtonFX?.SetFill(normalizedProgress);
    }

    protected override void HandleHoldCancelled()
    {
        recallButtonFX?.CancelHold();
    }

    protected override void HandleHoldCompleted()
    {
        recallButtonFX?.CompleteHold();
    }

    private void SubscribeToPlayerState()
    {
        if (subscribedToPlayerState)
        {
            return;
        }

        if (playerStateController == null)
        {
            ResolveReferences();
        }

        if (playerStateController == null)
        {
            return;
        }

        playerStateController.SelectionChanged += HandleSelectionChanged;
        subscribedToPlayerState = true;
    }

    private void UnsubscribeFromPlayerState()
    {
        if (!subscribedToPlayerState || playerStateController == null)
        {
            return;
        }

        playerStateController.SelectionChanged -= HandleSelectionChanged;
        subscribedToPlayerState = false;
    }

    private void HandleSelectionChanged(PlayerSelectionChangedEvent eventData)
    {
        RefreshState();
    }

    private GameObject GetVisibilityTarget()
    {
        return recallButtonRoot != null
            ? recallButtonRoot
            : recallButton != null ? recallButton.gameObject : null;
    }

    private void SetSelfVisible(bool isVisible)
    {
        if (selfVisibilityGroup == null)
        {
            selfVisibilityGroup = GetComponent<CanvasGroup>();
            if (selfVisibilityGroup == null && Application.isPlaying)
            {
                selfVisibilityGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }

        if (selfVisibilityGroup == null)
        {
            return;
        }

        selfVisibilityGroup.alpha = isVisible ? 1f : 0f;
        selfVisibilityGroup.interactable = isVisible;
        selfVisibilityGroup.blocksRaycasts = isVisible;
    }

    private void EnsurePointerForwarder()
    {
        if (recallButton == null || recallButton.gameObject == gameObject)
        {
            return;
        }

        if (recallPointerForwarder == null || recallPointerForwarder.gameObject != recallButton.gameObject)
        {
            recallPointerForwarder = recallButton.GetComponent<RecallPointerForwarder>();
            if (Application.isPlaying && recallPointerForwarder == null)
            {
                recallPointerForwarder = recallButton.gameObject.AddComponent<RecallPointerForwarder>();
            }
        }

        if (recallPointerForwarder != null)
        {
            recallPointerForwarder.Initialize(this);
        }
    }
}
