using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Scene-level tooltip controller registered through the ServiceLocator.
/// </summary>
[DefaultExecutionOrder(-900)]
public class HoverController : MonoBehaviour
{
    [SerializeField, Tooltip("Tooltip presenter controlled by this controller. Can be assigned or registered by the tooltip.")]
    private HoverToolTip toolTip;

    [SerializeField, Tooltip("Screen-space cursor offset used when the tooltip appears to the right of the pointer.")]
    private Vector2 rightSideOffset = new Vector2(24f, -24f);

    [SerializeField, Tooltip("Screen-space cursor offset used when the tooltip must appear to the left of the pointer.")]
    private Vector2 leftSideOffset = new Vector2(-24f, -24f);

    [SerializeField, Min(0f), Tooltip("Minimum screen padding kept around the tooltip.")]
    private float screenPadding = 12f;

    private GenericHoverableItem currentItem;
    private Vector2 currentPointerPosition;

    private void Awake()
    {
        RegisterWithServiceLocator();
        if (toolTip != null)
        {
            toolTip.SetController(this);
            toolTip.Hide();
        }
    }

    private void OnDestroy()
    {
        ServiceLocator.Unregister<HoverController>(this);
    }

    private void OnValidate()
    {
        screenPadding = Mathf.Max(0f, screenPadding);
    }

    public void RegisterToolTip(HoverToolTip toolTip)
    {
        if (toolTip == null)
        {
            return;
        }

        this.toolTip = toolTip;
        toolTip.SetController(this);
        toolTip.Hide();
    }

    public void UnregisterToolTip(HoverToolTip toolTip)
    {
        if (this.toolTip != toolTip)
        {
            return;
        }

        this.toolTip = null;
        currentItem = null;
    }

    public void Show(GenericHoverableItem item, Vector2 pointerPosition)
    {
        if (item == null)
        {
            Hide(null);
            return;
        }

        ResolveToolTip();
        if (toolTip == null)
        {
            return;
        }

        currentItem = item;
        currentPointerPosition = pointerPosition;
        toolTip.Bind(item);
        PositionToolTip(pointerPosition);
        toolTip.Show();
    }

    public void UpdatePointerPosition(GenericHoverableItem item, Vector2 pointerPosition)
    {
        if (currentItem != item || toolTip == null)
        {
            return;
        }

        currentPointerPosition = pointerPosition;
        PositionToolTip(pointerPosition);
    }

    public void Hide(GenericHoverableItem item)
    {
        if (item != null && currentItem != item)
        {
            return;
        }

        currentItem = null;
        if (toolTip != null)
        {
            toolTip.Hide();
        }
    }

    private void LateUpdate()
    {
        if (currentItem != null && toolTip != null)
        {
            PositionToolTip(currentPointerPosition);
        }
    }

    private void PositionToolTip(Vector2 pointerPosition)
    {
        if (toolTip == null || toolTip.RectTransform == null)
        {
            return;
        }

        RectTransform rectTransform = toolTip.RectTransform;
        LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);

        Vector2 size = rectTransform.rect.size;
        Vector2 pivot = rectTransform.pivot;
        float rightSidePivotX = pointerPosition.x + rightSideOffset.x + (size.x * pivot.x);
        float rightSideEdgeX = rightSidePivotX + (size.x * (1f - pivot.x));
        bool shouldFlipLeft = rightSideEdgeX + screenPadding > Screen.width;

        float pivotX = shouldFlipLeft
            ? pointerPosition.x + leftSideOffset.x - (size.x * (1f - pivot.x))
            : rightSidePivotX;

        float verticalOffset = shouldFlipLeft ? leftSideOffset.y : rightSideOffset.y;
        float pivotY = pointerPosition.y + verticalOffset - (size.y * (1f - pivot.y));

        float minPivotX = screenPadding + (size.x * pivot.x);
        float maxPivotX = Screen.width - screenPadding - (size.x * (1f - pivot.x));
        float minPivotY = screenPadding + (size.y * pivot.y);
        float maxPivotY = Screen.height - screenPadding - (size.y * (1f - pivot.y));

        Vector2 screenPosition = new Vector2(
            Mathf.Clamp(pivotX, minPivotX, Mathf.Max(minPivotX, maxPivotX)),
            Mathf.Clamp(pivotY, minPivotY, Mathf.Max(minPivotY, maxPivotY)));

        RectTransform parentRect = rectTransform.parent as RectTransform;
        if (parentRect == null)
        {
            rectTransform.position = screenPosition;
            return;
        }

        Canvas canvas = rectTransform.GetComponentInParent<Canvas>();
        Camera canvasCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? canvas.worldCamera
            : null;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, screenPosition, canvasCamera, out Vector2 localPoint))
        {
            rectTransform.anchoredPosition = localPoint;
        }
    }

    private void ResolveToolTip()
    {
        if (toolTip == null)
        {
            toolTip = FindAnyObjectByType<HoverToolTip>(FindObjectsInactive.Include);
            if (toolTip != null)
            {
                toolTip.SetController(this);
            }
        }
    }

    private void RegisterWithServiceLocator()
    {
        if (ServiceLocator.TryResolve<HoverController>(out HoverController existingController)
            && existingController != null
            && existingController != this)
        {
            Debug.LogWarning(
                $"{nameof(HoverController)} on '{name}' replaced the previously registered {nameof(HoverController)} on '{existingController.name}'.",
                this);
        }

        ServiceLocator.Register<HoverController>(this);
    }
}
