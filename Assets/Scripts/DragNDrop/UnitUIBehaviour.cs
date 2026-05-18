using UnityEngine;

/// <summary>
/// Shared base for roster-card UI components that read one <see cref="UIUnitItem"/>.
/// </summary>
[RequireComponent(typeof(UIUnitItem))]
public abstract class UnitUIBehaviour : MonoBehaviour
{
    [SerializeField, Tooltip("Sibling unit item model used to resolve roster identity and runtime state.")]
    private UIUnitItem uiUnitItem;

    [SerializeField, Tooltip("Event bus used to refresh this unit UI component.")]
    private UnitEventBus eventBus;

    private bool eventBusSubscribed;

    protected UIUnitItem UnitItem => uiUnitItem;
    protected UnitEventBus EventBus => eventBus;
    protected bool IsSubscribedToEventBus => eventBusSubscribed;

    protected virtual void Awake()
    {
        ResolveReferences();
    }

    protected virtual void Start()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        SubscribeToEventBusIfNeeded();
    }

    protected virtual void OnEnable()
    {
        ResolveReferences();

        if (!Application.isPlaying)
        {
            return;
        }

        SubscribeToEventBusIfNeeded();
    }

    protected virtual void OnDisable()
    {
        UnsubscribeFromEventBusIfNeeded();
    }

    protected virtual void OnDestroy()
    {
        UnsubscribeFromEventBusIfNeeded();
    }

    protected virtual void OnValidate()
    {
        ResolveReferences();
    }

    protected virtual void ResolveReferences()
    {
        if (uiUnitItem == null)
        {
            uiUnitItem = GetComponent<UIUnitItem>();
        }

        if (eventBus == null)
        {
            ServiceLocator.TryResolve(out eventBus);
        }
    }

    protected bool TryGetManagedUnit(out UnitStateManager.OwnedUnitState unit)
    {
        ResolveReferences();

        if (uiUnitItem == null || !uiUnitItem.IsManagedUnitConfigured)
        {
            unit = null;
            return false;
        }

        return uiUnitItem.TryGetOwnedUnit(out unit);
    }

    protected bool IsMatchingUnit(string unitId)
    {
        return uiUnitItem != null
            && uiUnitItem.IsManagedUnit
            && uiUnitItem.UnitId == unitId;
    }

    protected bool SubscribeToEventBusIfNeeded()
    {
        if (eventBusSubscribed)
        {
            return true;
        }

        if (eventBus == null)
        {
            ResolveReferences();
        }

        if (eventBus == null)
        {
            return false;
        }

        SubscribeToEvents(eventBus);
        eventBusSubscribed = true;
        return true;
    }

    protected void UnsubscribeFromEventBusIfNeeded()
    {
        if (!eventBusSubscribed || eventBus == null)
        {
            eventBusSubscribed = false;
            return;
        }

        UnsubscribeFromEvents(eventBus);
        eventBusSubscribed = false;
    }

    protected virtual void SubscribeToEvents(UnitEventBus eventBus)
    {
    }

    protected virtual void UnsubscribeFromEvents(UnitEventBus eventBus)
    {
    }
}
