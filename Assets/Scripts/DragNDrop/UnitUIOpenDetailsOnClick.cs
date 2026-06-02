using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Opens the unit details panel for a managed roster card when clicked.
/// </summary>
[RequireComponent(typeof(UIUnitItem))]
public class UnitUIOpenDetailsOnClick : UnitUIBehaviour, IPointerClickHandler
{
    [SerializeField, Tooltip("Player state source used to select deployed or roster-only units. Uses the scene service locator when empty.")]
    private PlayerStateController playerStateController;

    protected override void ResolveReferences()
    {
        base.ResolveReferences();

        if (playerStateController == null)
        {
            ServiceLocator.TryResolve(out playerStateController);
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData == null || eventData.button != PointerEventData.InputButton.Left)
        {
            return;
        }

        TryOpenDetails();
    }

    public bool TryOpenDetails()
    {
        ResolveReferences();

        if (playerStateController == null || !TryGetManagedUnit(out UnitStateManager.OwnedUnitState unit))
        {
            return false;
        }

        TowerEntity runtimeTower = unit.CurrentRuntimeInstance;
        if (runtimeTower != null)
        {
            return playerStateController.TrySelectTower(runtimeTower);
        }

        return playerStateController.TrySelectRosterUnit(unit.UnitId);
    }
}
