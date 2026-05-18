using TMPro;
using UnityEngine;

/// <summary>
/// Hand-placed canvas tooltip presenter controlled by <see cref="HoverController"/>.
/// </summary>
public class HoverToolTip : MonoBehaviour
{
    [SerializeField, Tooltip("Optional root toggled when the tooltip is visible. Defaults to this object.")]
    private GameObject root;

    [SerializeField, Tooltip("RectTransform moved by the hover controller. Defaults to this object's RectTransform.")]
    private RectTransform rectTransform;

    [SerializeField, Tooltip("TMP text used to display the hovered title.")]
    private TMP_Text titleText;

    [SerializeField, Tooltip("TMP text used to display the hovered description.")]
    private TMP_Text descriptionText;

    [SerializeField, Tooltip("Optional icon display shown only for upgrade hover items.")]
    private GenericIconDisplay iconDisplay;

    private HoverController controller;

    public RectTransform RectTransform => rectTransform;

    private void Awake()
    {
        ResolveReferences();
        ResolveController();
        Hide();
    }

    private void OnEnable()
    {
        ResolveReferences();
        ResolveController();
    }

    private void OnDestroy()
    {
        if (controller != null)
        {
            controller.UnregisterToolTip(this);
        }
    }

    private void OnValidate()
    {
        ResolveReferences();
    }

    public void SetController(HoverController controller)
    {
        this.controller = controller;
    }

    public void Bind(GenericHoverableItem item)
    {
        ResolveReferences();

        if (titleText != null)
        {
            titleText.text = item != null ? item.Title : string.Empty;
        }

        if (descriptionText != null)
        {
            descriptionText.text = item != null ? item.Description : string.Empty;
        }

        if (iconDisplay != null)
        {
            if (item is UpgradeHoverableItem upgradeHoverableItem)
            {
                iconDisplay.Bind(upgradeHoverableItem.IconSprite);
            }
            else
            {
                iconDisplay.Clear();
            }
        }
    }

    public void Show()
    {
        SetRootVisible(true);
    }

    public void Hide()
    {
        SetRootVisible(false);
    }

    private void ResolveReferences()
    {
        if (root == null)
        {
            root = gameObject;
        }

        if (rectTransform == null)
        {
            rectTransform = GetComponent<RectTransform>();
        }
    }

    private void ResolveController()
    {
        if (controller == null)
        {
            ServiceLocator.TryResolve(out controller);
        }

        if (controller != null)
        {
            controller.RegisterToolTip(this);
        }
    }

    private void SetRootVisible(bool isVisible)
    {
        GameObject target = root != null ? root : gameObject;
        if (target != null && target.activeSelf != isVisible)
        {
            target.SetActive(isVisible);
        }
    }
}
