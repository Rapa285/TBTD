using UnityEngine;

/// <summary>
/// Recall button for a deployed tower child UI. It is available only while that tower is selected.
/// </summary>
public sealed class TowerRecallButton : RecallHoldButtonBase
{
    [SerializeField, Tooltip("Owning tower. If empty, the first parent TowerEntity is used.")]
    private TowerEntity tower;

    [SerializeField, Tooltip("Roster state source used to recall managed towers. Uses the scene service locator when empty.")]
    private UnitStateManager unitStateManager;

    [SerializeField, Tooltip("Optional root shown while this world recall control is available. Leave as this object to hide renderers/collider without disabling the component.")]
    private GameObject recallRoot;

    [SerializeField, Tooltip("Collider used by EventSystem PhysicsRaycaster pointer events.")]
    private Collider recallCollider;

    [SerializeField, Tooltip("Optional sprite-based programmatic visual feedback for hover and hold progress.")]
    private RecallWorldSpriteFX recallSpriteFX;

    private TowerEntity subscribedTower;
    private bool recallAvailable;

    protected override void ResolveReferences()
    {
        base.ResolveReferences();

        if (tower == null)
        {
            tower = GetComponentInParent<TowerEntity>(true);
        }

        if (unitStateManager == null)
        {
            ServiceLocator.TryResolve(out unitStateManager);
        }

        if (recallCollider == null)
        {
            recallCollider = GetComponent<Collider>();
        }

        if (recallCollider == null)
        {
            recallCollider = GetComponentInChildren<Collider>(true);
        }

        if (recallSpriteFX == null)
        {
            recallSpriteFX = GetComponentInChildren<RecallWorldSpriteFX>(true);
        }

        if (recallRoot == null && recallSpriteFX != null)
        {
            recallRoot = recallSpriteFX.gameObject;
        }

        if (recallRoot == null && recallCollider != null)
        {
            recallRoot = recallCollider.gameObject;
        }
    }

    protected override void Subscribe()
    {
        base.Subscribe();
        SubscribeToTower();
    }

    protected override void Unsubscribe()
    {
        UnsubscribeFromTower();
        base.Unsubscribe();
    }

    protected override bool TryGetRecallTarget(out string unitId, out UnitStateManager stateManager, out TowerEntity targetTower)
    {
        ResolveReferences();

        unitId = tower != null ? tower.UnitId : null;
        stateManager = unitStateManager;
        targetTower = tower;

        if (tower == null || !tower.IsSelected || string.IsNullOrWhiteSpace(unitId) || stateManager == null)
        {
            return false;
        }

        return stateManager.TryGetUnit(unitId, out UnitStateManager.OwnedUnitState unit)
            && unit.IsDeployed
            && unit.CurrentRuntimeInstance == tower;
    }

    protected override bool ShouldRefreshForUnit(string unitId)
    {
        return base.ShouldRefreshForUnit(unitId)
            || tower != null
            && !string.IsNullOrWhiteSpace(unitId)
            && tower.UnitId == unitId;
    }

    protected override void SetRecallAvailable(bool isAvailable)
    {
        base.SetRecallAvailable(isAvailable);

        recallAvailable = isAvailable;

        if (recallRoot != null && recallRoot != gameObject && recallRoot.activeSelf != isAvailable)
        {
            recallRoot.SetActive(isAvailable);
        }

        if (recallCollider != null)
        {
            recallCollider.enabled = isAvailable;
        }

        if (recallSpriteFX != null)
        {
            recallSpriteFX.SetAvailable(isAvailable);
        }
    }

    protected override bool IsRecallVisible()
    {
        if (!recallAvailable || !isActiveAndEnabled)
        {
            return false;
        }

        return recallRoot == null
            || recallRoot == gameObject
            || recallRoot.activeInHierarchy;
    }

    protected override bool IsRecallInteractable()
    {
        return recallCollider != null
            && recallCollider.enabled
            && recallCollider.gameObject.activeInHierarchy;
    }

    protected override void HandlePointerHoverChanged(bool isHovered)
    {
        recallSpriteFX?.SetHovered(isHovered);
    }

    protected override void HandleHoldStarted(float duration)
    {
        recallSpriteFX?.BeginHold(duration);
    }

    protected override void HandleHoldProgress(float normalizedProgress)
    {
        recallSpriteFX?.SetHoldProgress(normalizedProgress);
    }

    protected override void HandleHoldCancelled()
    {
        recallSpriteFX?.CancelHold();
    }

    protected override void HandleHoldCompleted()
    {
        recallSpriteFX?.CompleteHold();
    }

    private void SubscribeToTower()
    {
        if (tower == null)
        {
            ResolveReferences();
        }

        if (subscribedTower == tower)
        {
            return;
        }

        UnsubscribeFromTower();

        if (tower == null)
        {
            return;
        }

        subscribedTower = tower;
        subscribedTower.Selected += HandleTowerSelectionChanged;
        subscribedTower.Deselected += HandleTowerSelectionChanged;
    }

    private void UnsubscribeFromTower()
    {
        if (subscribedTower == null)
        {
            return;
        }

        subscribedTower.Selected -= HandleTowerSelectionChanged;
        subscribedTower.Deselected -= HandleTowerSelectionChanged;
        subscribedTower = null;
    }

    private void HandleTowerSelectionChanged()
    {
        RefreshState();
    }
}
