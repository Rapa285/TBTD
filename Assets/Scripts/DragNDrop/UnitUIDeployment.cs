using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// UI input and deployment behavior for one unit item.
/// </summary>
[RequireComponent(typeof(UIUnitItem))]
public class UnitUIDeployment : MonoBehaviour, IPointerUpHandler, IPointerDownHandler, IPointerExitHandler, IPointerEnterHandler, IBeginDragHandler
{
    [SerializeField, Tooltip("Sibling unit item model used to resolve deployment identity and state.")]
    private UIUnitItem uiUnitItem;

    [SerializeField, Tooltip("Deployment controller that owns preview placement and final deployment.")]
    private UnitDeploymentController deploymentController;

    [SerializeField, Tooltip("Optional Selectable used to block deployment while the UI element is not interactable.")]
    private Selectable selectable;

    [SerializeField, Tooltip("Optional root shown while this UI item can currently start deployment.")]
    private GameObject deployableIndicatorRoot;

    [SerializeField, Tooltip("Start deployment when Unity begins a drag gesture from this UI item.")]
    private bool beginDeploymentOnBeginDrag = true;

    [SerializeField, Tooltip("Start deployment when the pointer leaves this item while held down.")]
    private bool beginDeploymentOnPointerExit = true;

    [SerializeField, Tooltip("Start deployment immediately when the left pointer button is pressed.")]
    private bool beginDeploymentOnPointerDown;

    private bool isHovered;
    private bool isHeldDown;
    private UnitEventBus eventBus;
    private CurrencyManager currencyManager;
    private bool subscribedToEventBus;

    public bool IsHovered => isHovered;
    public bool IsHeldDown => isHeldDown;
    public UnitDeploymentController DeploymentController => ResolveDeploymentController();

    private void Awake()
    {
        ResolveReferences();
        ResetPointerState();
    }

    private void Start()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        SubscribeToEventBus();
        RefreshDeployableIndicator();
    }

    private void OnEnable()
    {
        ResolveReferences();

        if (!Application.isPlaying)
        {
            return;
        }

        SubscribeToEventBus();
        RefreshDeployableIndicator();
    }

    private void Update()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        if (!subscribedToEventBus)
        {
            SubscribeToEventBus();
        }

        // RefreshDeployableIndicator();
    }

    private void OnDisable()
    {
        if (Application.isPlaying)
        {
            SetDeployableIndicatorVisible(false);
        }

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

    public bool TryBeginDeployment()
    {
        if (!CanBeginDeployment())
        {
            return false;
        }

        if (uiUnitItem.IsManagedUnitConfigured)
        {
            return deploymentController.BeginDeployment(uiUnitItem.UnitStateManager, uiUnitItem.UnitId);
        }

        return deploymentController.BeginDeployment(uiUnitItem.UnitToDeploy);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovered = true;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isHeldDown = false;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left || !CanBeginDeployment())
        {
            return;
        }

        isHeldDown = true;

        if (beginDeploymentOnPointerDown && TryBeginDeployment())
        {
            ResetPointerState();
            RefreshDeployableIndicator();
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!isHeldDown || eventData.button != PointerEventData.InputButton.Left || !beginDeploymentOnBeginDrag)
        {
            return;
        }

        if (TryBeginDeployment())
        {
            ResetPointerState();
            RefreshDeployableIndicator();
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;

        if (isHeldDown && beginDeploymentOnPointerExit)
        {
            TryBeginDeployment();
        }

        ResetPointerState();
        RefreshDeployableIndicator();
    }

    private bool CanBeginDeployment()
    {
        ResolveReferences();

        return uiUnitItem != null
            && deploymentController != null
            && !deploymentController.IsDragging
            && (selectable == null || selectable.IsInteractable())
            && uiUnitItem.HasDeployableUnit()
            && HasAffordableDeploymentCost();
    }

    private void HandleCurrencyChanged(CurrencyChangedEvent eventData)
    {
        RefreshDeployableIndicator();
    }

    private void HandleUnitDeploymentCostCompiled(UnitDeploymentCostCompiledEvent eventData)
    {
        if (IsMatchingUnit(eventData.UnitId))
        {
            RefreshDeployableIndicator();
        }
    }

    private void HandleUnitDeployed(UnitDeployedEvent eventData)
    {
        if (IsMatchingUnit(eventData.UnitId))
        {
            RefreshDeployableIndicator();
        }
    }

    private void HandleUnitRecalled(UnitRecalledEvent eventData)
    {
        if (IsMatchingUnit(eventData.UnitId))
        {
            RefreshDeployableIndicator();
        }
    }

    private void HandleUnitCooldownEnded(UnitCooldownEndedEvent eventData)
    {
        if (IsMatchingUnit(eventData.UnitId))
        {
            RefreshDeployableIndicator();
        }
    }

    private void ResolveReferences()
    {
        if (uiUnitItem == null)
        {
            uiUnitItem = GetComponent<UIUnitItem>();
        }

        if (selectable == null)
        {
            selectable = GetComponent<Selectable>();
        }

        if (deploymentController == null)
        {
            ResolveDeploymentController();
        }

        if (eventBus == null)
        {
            ServiceLocator.TryResolve(out eventBus);
        }

        if (currencyManager == null)
        {
            ServiceLocator.TryResolve(out currencyManager);
        }
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
            ResolveReferences();
        }

        if (eventBus == null)
        {
            return;
        }

        eventBus.CurrencyChanged += HandleCurrencyChanged;
        eventBus.UnitDeploymentCostCompiled += HandleUnitDeploymentCostCompiled;
        eventBus.UnitDeployed += HandleUnitDeployed;
        eventBus.UnitRecalled += HandleUnitRecalled;
        eventBus.UnitCooldownEnded += HandleUnitCooldownEnded;
        subscribedToEventBus = true;
    }

    private void UnsubscribeFromEventBus()
    {
        if (!subscribedToEventBus || eventBus == null)
        {
            return;
        }

        eventBus.CurrencyChanged -= HandleCurrencyChanged;
        eventBus.UnitDeploymentCostCompiled -= HandleUnitDeploymentCostCompiled;
        eventBus.UnitDeployed -= HandleUnitDeployed;
        eventBus.UnitRecalled -= HandleUnitRecalled;
        eventBus.UnitCooldownEnded -= HandleUnitCooldownEnded;
        subscribedToEventBus = false;
    }

    private bool HasAffordableDeploymentCost()
    {
        if (uiUnitItem == null || !uiUnitItem.IsManagedUnitConfigured)
        {
            return true;
        }

        UnitStateManager stateManager = uiUnitItem.UnitStateManager;
        if (stateManager == null || !stateManager.TryGetDeploymentCost(uiUnitItem.UnitId, out int cost))
        {
            return true;
        }

        if (currencyManager == null)
        {
            ResolveReferences();
        }

        return currencyManager == null || currencyManager.CanAfford(cost);
    }

    private bool IsMatchingUnit(string unitId)
    {
        return uiUnitItem != null
            && uiUnitItem.IsManagedUnit
            && uiUnitItem.UnitId == unitId;
    }

    private void RefreshDeployableIndicator()
    {
        SetDeployableIndicatorVisible(CanBeginDeployment());
    }

    private void SetDeployableIndicatorVisible(bool isVisible)
    {
        if (deployableIndicatorRoot != null && deployableIndicatorRoot.activeSelf != isVisible)
        {
            deployableIndicatorRoot.SetActive(isVisible);
        }
    }

    private void ResetPointerState()
    {
        isHovered = false;
        isHeldDown = false;
    }
}
