using UnityEngine;

/// <summary>
/// UI identity and status model for one unit item.
/// </summary>
public class UIUnitItem : MonoBehaviour
{
    [SerializeField, Tooltip("Fallback tower prefab used when this UI item is not bound to a roster unit.")]
    private GameObject unitToDeploy;

    [SerializeField, Tooltip("Roster manager used when deploying a persistent owned unit by ID.")]
    private UnitStateManager unitStateManager;

    [SerializeField, Tooltip("Stable ID of the owned unit to deploy from the roster manager.")]
    private string unitId;

    private bool warnedMissingUnitStateManager;

    public GameObject UnitToDeploy => unitToDeploy;
    public UnitStateManager UnitStateManager => unitStateManager;
    public string UnitId => unitId;
    public bool IsManagedUnit => !string.IsNullOrWhiteSpace(unitId);
    public bool IsManagedUnitConfigured => IsManagedUnit && ResolveManagedUnitStateManager() != null;
    public bool IsDeployed => TryGetOwnedUnit(out UnitStateManager.OwnedUnitState unit) && unit.IsDeployed;
    public bool CanDeploy => EvaluateCanDeploy();

    private void Awake()
    {
        ResolveUnitStateManager();
    }

    /// <summary>
    /// Updates this item to deploy a direct prefab instead of a roster-managed unit.
    /// </summary>
    public void SetUnitToDeploy(GameObject unitPrefab)
    {
        unitToDeploy = unitPrefab;
        unitStateManager = null;
        unitId = null;
        warnedMissingUnitStateManager = false;
    }

    /// <summary>
    /// Updates this item to deploy a persistent roster unit.
    /// </summary>
    public void SetManagedUnit(UnitStateManager stateManager, string unitId)
    {
        unitStateManager = stateManager;
        this.unitId = unitId;
        warnedMissingUnitStateManager = false;
    }

    /// <summary>
    /// Returns whether this item currently represents a deployable unit.
    /// </summary>
    public bool HasDeployableUnit()
    {
        ResolveUnitStateManager();
        return EvaluateCanDeploy();
    }

    /// <summary>
    /// Tries to get the roster entry represented by this item.
    /// </summary>
    public bool TryGetOwnedUnit(out UnitStateManager.OwnedUnitState unit)
    {
        UnitStateManager resolvedUnitStateManager = ResolveManagedUnitStateManager();
        if (resolvedUnitStateManager == null)
        {
            unit = null;
            return false;
        }

        return resolvedUnitStateManager.TryGetUnit(unitId, out unit);
    }

    /// <summary>
    /// Recalls the managed unit represented by this item.
    /// </summary>
    public bool TryRecall()
    {
        UnitStateManager resolvedUnitStateManager = ResolveManagedUnitStateManager();
        return resolvedUnitStateManager != null && resolvedUnitStateManager.RecallUnit(unitId);
    }

    private bool EvaluateCanDeploy()
    {
        if (IsManagedUnit)
        {
            UnitStateManager resolvedUnitStateManager = ResolveManagedUnitStateManager();
            return resolvedUnitStateManager != null && resolvedUnitStateManager.CanDeploy(unitId);
        }

        return unitToDeploy != null && HasDeployableTowerPrefab();
    }

    private bool HasDeployableTowerPrefab()
    {
        return unitToDeploy.GetComponent<TowerEntity>() != null
            || unitToDeploy.GetComponentInChildren<TowerEntity>(true) != null;
    }

    private void ResolveUnitStateManager()
    {
        ResolveManagedUnitStateManager();
    }

    private UnitStateManager ResolveManagedUnitStateManager()
    {
        if (unitStateManager != null || !IsManagedUnit)
        {
            return unitStateManager;
        }

        unitStateManager = FindAnyObjectByType<UnitStateManager>();
        if (unitStateManager == null)
        {
            WarnIfMissingUnitStateManager();
        }

        return unitStateManager;
    }

    private void WarnIfMissingUnitStateManager()
    {
        if (unitStateManager != null || warnedMissingUnitStateManager)
        {
            return;
        }

        warnedMissingUnitStateManager = true;
        Debug.LogWarning(
            $"{nameof(UIUnitItem)} is configured with unitId '{unitId}' but no {nameof(UnitStateManager)} was found. The unit cannot deploy through roster progression.",
            this);
    }
}
