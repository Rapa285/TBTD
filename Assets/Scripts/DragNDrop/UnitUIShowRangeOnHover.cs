using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Shows a deployed managed unit's range while its roster card is hovered.
/// </summary>
[RequireComponent(typeof(UIUnitItem))]
public class UnitUIShowRangeOnHover : UnitUIBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private TowerEntity hoveredTower;
    private bool isPointerHovered;

    protected override void Start()
    {
        base.Start();
        RefreshHoverRange();
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        RefreshHoverRange();
    }

    protected override void OnDisable()
    {
        ClearHoverRange();
        isPointerHovered = false;
        base.OnDisable();
    }

    protected override void OnDestroy()
    {
        ClearHoverRange();
        base.OnDestroy();
    }

    protected override void SubscribeToEvents(UnitEventBus eventBus)
    {
        eventBus.UnitDeployed += HandleUnitDeployed;
        eventBus.UnitRecalled += HandleUnitRecalled;
    }

    protected override void UnsubscribeFromEvents(UnitEventBus eventBus)
    {
        eventBus.UnitDeployed -= HandleUnitDeployed;
        eventBus.UnitRecalled -= HandleUnitRecalled;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isPointerHovered = true;
        RefreshHoverRange();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isPointerHovered = false;
        ClearHoverRange();
    }

    private void HandleUnitDeployed(UnitDeployedEvent eventData)
    {
        if (IsMatchingUnit(eventData.UnitId))
        {
            RefreshHoverRange();
        }
    }

    private void HandleUnitRecalled(UnitRecalledEvent eventData)
    {
        if (IsMatchingUnit(eventData.UnitId))
        {
            ClearHoverRange();
        }
    }

    private void RefreshHoverRange()
    {
        if (!isPointerHovered)
        {
            return;
        }

        TowerEntity tower = TryGetHoveredTower();
        if (tower == hoveredTower)
        {
            if (hoveredTower != null)
            {
                hoveredTower.SetRangeHoverVisible(this, true);
            }

            return;
        }

        ClearHoverRange();

        hoveredTower = tower;
        if (hoveredTower != null)
        {
            hoveredTower.SetRangeHoverVisible(this, true);
        }
    }

    private void ClearHoverRange()
    {
        if (hoveredTower != null)
        {
            hoveredTower.SetRangeHoverVisible(this, false);
            hoveredTower = null;
        }
    }

    private TowerEntity TryGetHoveredTower()
    {
        return TryGetManagedUnit(out UnitStateManager.OwnedUnitState unit)
            ? unit.CurrentRuntimeInstance
            : null;
    }
}
