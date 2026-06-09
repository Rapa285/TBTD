using System.Globalization;
using UnityEngine;

/// <summary>
/// Displays deployed setup/preparing time in the shared ammo UI slot for one managed unit item.
/// </summary>
[RequireComponent(typeof(UIUnitItem))]
[RequireComponent(typeof(UnitUIAmmoDisplay))]
public class UnitUISetupDisplay : UnitUIBehaviour
{
    [SerializeField, Tooltip("Ammo display slot driven while this unit is preparing. Defaults to the sibling UnitUIAmmoDisplay.")]
    private UnitUIAmmoDisplay ammoDisplay;

    [SerializeField, Tooltip("Fill color used while the deployed tower is preparing during setup time.")]
    private Color preparingColor = Color.cyan;

    private bool isShowingSetupTime;

    protected override void Awake()
    {
        base.Awake();
        ClearSetupDisplay(false);
    }

    protected override void Start()
    {
        base.Start();
        RefreshDisplay();
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        RefreshDisplay();
    }

    private void Update()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        if (!IsSubscribedToEventBus)
        {
            SubscribeToEventBusIfNeeded();
        }

        if (isShowingSetupTime)
        {
            RefreshDisplay();
        }
    }

    protected override void OnDisable()
    {
        if (Application.isPlaying)
        {
            ClearSetupDisplay(false);
        }

        base.OnDisable();
    }

    protected override void OnValidate()
    {
        base.OnValidate();
        ResolveSetupReferences();
    }

    protected override void ResolveReferences()
    {
        base.ResolveReferences();
        ResolveSetupReferences();
    }

    protected override void SubscribeToEvents(UnitEventBus eventBus)
    {
        eventBus.UnitDeployed += HandleUnitDeployed;
        eventBus.UnitRecalled += HandleUnitRecalled;
        eventBus.TowerModified += HandleTowerModified;
        eventBus.UnitDeploymentPreviewEnded += HandleUnitDeploymentPreviewEnded;
    }

    protected override void UnsubscribeFromEvents(UnitEventBus eventBus)
    {
        eventBus.UnitDeployed -= HandleUnitDeployed;
        eventBus.UnitRecalled -= HandleUnitRecalled;
        eventBus.TowerModified -= HandleTowerModified;
        eventBus.UnitDeploymentPreviewEnded -= HandleUnitDeploymentPreviewEnded;
    }

    private void HandleUnitDeployed(UnitDeployedEvent eventData)
    {
        if (IsMatchingUnit(eventData.UnitId))
        {
            RefreshDisplay();
        }
    }

    private void HandleUnitRecalled(UnitRecalledEvent eventData)
    {
        if (IsMatchingUnit(eventData.UnitId))
        {
            ClearSetupDisplay(false);
        }
    }

    private void HandleTowerModified(TowerModifiedEvent eventData)
    {
        if (IsMatchingUnit(eventData.UnitId))
        {
            RefreshDisplay();
        }
    }

    private void HandleUnitDeploymentPreviewEnded(UnitDeploymentPreviewEndedEvent eventData)
    {
        if (eventData.WasCompleted && IsMatchingUnit(eventData.UnitId))
        {
            RefreshDisplay();
        }
    }

    private void RefreshDisplay()
    {
        ResolveReferences();

        if (!TryGetRuntimeTower(out TowerEntity tower) || !tower.IsInSetupTime)
        {
            ClearSetupDisplay(true);
            return;
        }

        if (ammoDisplay == null)
        {
            return;
        }

        string remainingText = $"{tower.SetupTimeRemaining.ToString("0.0", CultureInfo.InvariantCulture)}s";
        ammoDisplay.ShowExternalStatus(remainingText, tower.SetupTimeNormalizedRemaining, preparingColor);
        isShowingSetupTime = true;
    }

    private bool TryGetRuntimeTower(out TowerEntity tower)
    {
        tower = null;

        if (!TryGetManagedUnit(out UnitStateManager.OwnedUnitState unit))
        {
            return false;
        }

        tower = unit.CurrentRuntimeInstance;
        return tower != null && tower.Deployed;
    }

    private void ClearSetupDisplay(bool refreshAmmoDisplay)
    {
        isShowingSetupTime = false;
        ammoDisplay?.ClearExternalStatus(refreshAmmoDisplay);
    }

    private void ResolveSetupReferences()
    {
        if (ammoDisplay == null)
        {
            ammoDisplay = GetComponent<UnitUIAmmoDisplay>();
        }
    }
}
