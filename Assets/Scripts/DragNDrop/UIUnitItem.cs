using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// UI drag/click entry point for starting direct prefab or roster-managed unit deployment.
/// </summary>
public class UIUnitItem : MonoBehaviour, IPointerUpHandler, IPointerDownHandler, IPointerExitHandler, IPointerEnterHandler, IBeginDragHandler
{
    [SerializeField, Tooltip("Fallback tower prefab used when this UI item is not bound to a roster unit.")]
    private GameObject unitToDeploy;

    [SerializeField, Tooltip("Roster manager used when deploying a persistent owned unit by ID.")]
    private UnitStateManager unitStateManager;

    [SerializeField, Tooltip("Stable ID of the owned unit to deploy from the roster manager.")]
    private string unitId;

    [SerializeField, Tooltip("Deployment controller that owns preview placement and final deployment.")]
    private UnitDeploymentController deploymentController;

    [SerializeField, Tooltip("Optional Selectable used to block deployment while the UI element is not interactable.")]
    private Selectable selectable;

    [SerializeField, Tooltip("Start deployment when Unity begins a drag gesture from this UI item.")]
    private bool beginDeploymentOnBeginDrag = true;

    [SerializeField, Tooltip("Start deployment when the pointer leaves this item while held down.")]
    private bool beginDeploymentOnPointerExit = true;

    [SerializeField, Tooltip("Start deployment immediately when the left pointer button is pressed.")]
    private bool beginDeploymentOnPointerDown;

    private bool isHovered;
    private bool isHeldDown;
    private bool warnedMissingUnitStateManager;

    public bool IsHovered => isHovered;
    public bool IsHeldDown => isHeldDown;
    public GameObject UnitToDeploy => unitToDeploy;
    public UnitStateManager UnitStateManager => unitStateManager;
    public string UnitId => unitId;
    public UnitDeploymentController DeploymentController => deploymentController;

    private void Awake()
    {
        if (selectable == null)
        {
            selectable = GetComponent<Selectable>();
        }

        ResolveDeploymentController();
        ResolveUnitStateManager();
        ResetPointerState();
    }

    private void OnValidate()
    {
        if (selectable == null)
        {
            selectable = GetComponent<Selectable>();
        }
    }

    /// <summary>
    /// Updates this item to deploy a direct prefab instead of a roster-managed unit.
    /// </summary>
    public void SetUnitToDeploy(GameObject unitPrefab)
    {
        unitToDeploy = unitPrefab;
        unitStateManager = null;
        unitId = null;
    }

    /// <summary>
    /// Updates this item to deploy a persistent roster unit.
    /// </summary>
    public void SetManagedUnit(UnitStateManager stateManager, string unitId)
    {
        unitStateManager = stateManager;
        this.unitId = unitId;
    }

    /// <summary>
    /// Assigns the deployment controller used by this UI item.
    /// </summary>
    public void SetDeploymentController(UnitDeploymentController controller)
    {
        deploymentController = controller;
    }

    /// <summary>
    /// Attempts to begin deployment using the managed-unit path when configured, otherwise the direct prefab path.
    /// </summary>
    public bool TryBeginDeployment()
    {
        if (!CanBeginDeployment())
        {
            return false;
        }

        if (HasManagedUnit())
        {
            return deploymentController.BeginDeployment(unitStateManager, unitId);
        }

        return deploymentController.BeginDeployment(unitToDeploy);
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
    }

    private bool CanBeginDeployment()
    {
        ResolveDeploymentController();
        ResolveUnitStateManager();

        return deploymentController != null
            && !deploymentController.IsDragging
            && (selectable == null || selectable.IsInteractable())
            && HasDeployableUnit();
    }

    private bool HasDeployableUnit()
    {
        if (HasManagedUnitId())
        {
            WarnIfMissingUnitStateManager();
            return unitStateManager != null && unitStateManager.CanDeploy(unitId);
        }

        return unitToDeploy != null && HasDeployableTowerPrefab();
    }

    private bool HasManagedUnit()
    {
        return unitStateManager != null && HasManagedUnitId();
    }

    private bool HasManagedUnitId()
    {
        return !string.IsNullOrWhiteSpace(unitId);
    }

    private bool HasDeployableTowerPrefab()
    {
        return unitToDeploy.GetComponent<TowerEntity>() != null
            || unitToDeploy.GetComponentInChildren<TowerEntity>(true) != null;
    }

    private void ResolveDeploymentController()
    {
        if (deploymentController != null)
        {
            return;
        }

        deploymentController = FindAnyObjectByType<UnitDeploymentController>();
    }

    private void ResolveUnitStateManager()
    {
        if (unitStateManager != null || !HasManagedUnitId())
        {
            return;
        }

        unitStateManager = FindAnyObjectByType<UnitStateManager>();
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

    private void ResetPointerState()
    {
        isHovered = false;
        isHeldDown = false;
    }
}
