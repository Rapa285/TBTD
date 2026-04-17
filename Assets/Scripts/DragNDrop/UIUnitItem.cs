using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class UIUnitItem : MonoBehaviour, IPointerUpHandler, IPointerDownHandler, IPointerExitHandler, IPointerEnterHandler, IBeginDragHandler
{
    [SerializeField] private GameObject unitToDeploy;
    [SerializeField] private UnitDeploymentController deploymentController;
    [SerializeField] private Selectable selectable;
    [SerializeField] private bool beginDeploymentOnBeginDrag = true;
    [SerializeField] private bool beginDeploymentOnPointerExit = true;
    [SerializeField] private bool beginDeploymentOnPointerDown;

    private bool isHovered;
    private bool isHeldDown;

    public bool IsHovered => isHovered;
    public bool IsHeldDown => isHeldDown;
    public GameObject UnitToDeploy => unitToDeploy;
    public UnitDeploymentController DeploymentController => deploymentController;

    private void Awake()
    {
        if (selectable == null)
        {
            selectable = GetComponent<Selectable>();
        }

        ResolveDeploymentController();
        ResetPointerState();
    }

    private void OnValidate()
    {
        if (selectable == null)
        {
            selectable = GetComponent<Selectable>();
        }
    }

    public void SetUnitToDeploy(GameObject unitPrefab)
    {
        unitToDeploy = unitPrefab;
    }

    public void SetDeploymentController(UnitDeploymentController controller)
    {
        deploymentController = controller;
    }

    public bool TryBeginDeployment()
    {
        if (!CanBeginDeployment())
        {
            return false;
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

        return unitToDeploy != null
            && HasDeployableTowerPrefab()
            && deploymentController != null
            && !deploymentController.IsDragging
            && (selectable == null || selectable.IsInteractable());
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

    private void ResetPointerState()
    {
        isHovered = false;
        isHeldDown = false;
    }
}
