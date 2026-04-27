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

    [SerializeField, Tooltip("Start deployment when Unity begins a drag gesture from this UI item.")]
    private bool beginDeploymentOnBeginDrag = true;

    [SerializeField, Tooltip("Start deployment when the pointer leaves this item while held down.")]
    private bool beginDeploymentOnPointerExit = true;

    [SerializeField, Tooltip("Start deployment immediately when the left pointer button is pressed.")]
    private bool beginDeploymentOnPointerDown;

    private bool isHovered;
    private bool isHeldDown;

    public bool IsHovered => isHovered;
    public bool IsHeldDown => isHeldDown;
    public UnitDeploymentController DeploymentController => ResolveDeploymentController();

    private void Awake()
    {
        ResolveReferences();
        ResetPointerState();
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
        ResolveReferences();

        return uiUnitItem != null
            && deploymentController != null
            && !deploymentController.IsDragging
            && (selectable == null || selectable.IsInteractable())
            && uiUnitItem.HasDeployableUnit();
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
    }

    private UnitDeploymentController ResolveDeploymentController()
    {
        if (deploymentController == null)
        {
            ServiceLocator.TryResolve(out deploymentController);
        }

        return deploymentController;
    }

    private void ResetPointerState()
    {
        isHovered = false;
        isHeldDown = false;
    }
}
