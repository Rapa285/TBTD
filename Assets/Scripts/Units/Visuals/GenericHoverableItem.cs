using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Generic UI hover source for tooltip title and description content.
/// </summary>
public class GenericHoverableItem : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerMoveHandler, ISelectHandler, IDeselectHandler
{
    [SerializeField, Tooltip("Title shown in the hover tooltip.")]
    private string title;

    [SerializeField, TextArea, Tooltip("Description shown in the hover tooltip.")]
    private string description;

    private HoverController hoverController;
    private bool isHovered;

    public virtual string Title => title;
    public virtual string Description => description;

    protected bool IsHovered => isHovered;

    protected virtual void Awake()
    {
        ResolveHoverController();
    }

    protected virtual void OnDisable()
    {
        HideTooltip();
        isHovered = false;
    }

    protected virtual void OnValidate()
    {
    }

    public void Bind(string title, string description)
    {
        this.title = title;
        this.description = description;
        RefreshTooltipIfHovered();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovered = true;
        ShowTooltip(eventData.position);
    }

    public void OnPointerMove(PointerEventData eventData)
    {
        if (!isHovered)
        {
            return;
        }

        ResolveHoverController();
        if (hoverController != null)
        {
            hoverController.UpdatePointerPosition(this, eventData.position);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        HideTooltip();
        isHovered = false;
    }

    public void OnSelect(BaseEventData eventData)
    {
        isHovered = true;
        ShowTooltip(Input.mousePosition);
    }

    public void OnDeselect(BaseEventData eventData)
    {
        HideTooltip();
        isHovered = false;
    }

    protected void RefreshTooltipIfHovered()
    {
        if (!isHovered)
        {
            return;
        }

        ShowTooltip(Input.mousePosition);
    }

    private void ShowTooltip(Vector2 pointerPosition)
    {
        ResolveHoverController();
        if (hoverController != null)
        {
            hoverController.Show(this, pointerPosition);
        }
    }

    private void HideTooltip()
    {
        if (hoverController != null)
        {
            hoverController.Hide(this);
        }
    }

    private void ResolveHoverController()
    {
        if (hoverController == null)
        {
            ServiceLocator.TryResolve(out hoverController);
        }
    }
}
