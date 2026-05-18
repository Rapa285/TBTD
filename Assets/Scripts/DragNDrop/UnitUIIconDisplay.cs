using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Displays the roster icon for one managed unit UI item.
/// </summary>
[RequireComponent(typeof(UIUnitItem))]
public class UnitUIIconDisplay : MonoBehaviour
{
    [SerializeField, Tooltip("Sibling unit item model used to resolve roster identity and icon.")]
    private UIUnitItem uiUnitItem;

    [SerializeField, Tooltip("Image used to display the owned unit's roster icon.")]
    private Image iconImage;

    [SerializeField, Tooltip("Optional root shown only when the roster unit has an icon. Defaults to the image object.")]
    private GameObject iconRoot;

    private void Awake()
    {
        ResolveReferences();
        RefreshDisplay();
    }

    private void Start()
    {
        RefreshDisplay();
    }

    private void OnEnable()
    {
        ResolveReferences();
        RefreshDisplay();
    }

    private void OnValidate()
    {
        ResolveReferences();
    }

    public void RefreshDisplay()
    {
        ResolveReferences();

        if (!TryGetIcon(out Sprite icon))
        {
            ClearDisplay();
            return;
        }

        SetIconVisible(true);

        if (iconImage != null)
        {
            iconImage.sprite = icon;
            iconImage.enabled = true;
        }
    }

    private bool TryGetIcon(out Sprite icon)
    {
        icon = null;

        if (uiUnitItem == null
            || !uiUnitItem.IsManagedUnitConfigured
            || !uiUnitItem.TryGetOwnedUnit(out UnitStateManager.OwnedUnitState unit))
        {
            return false;
        }

        icon = unit.Icon;
        return icon != null;
    }

    private void ClearDisplay()
    {
        SetIconVisible(false);

        if (iconImage != null)
        {
            iconImage.sprite = null;
            iconImage.enabled = false;
        }
    }

    private void ResolveReferences()
    {
        if (uiUnitItem == null)
        {
            uiUnitItem = GetComponent<UIUnitItem>();
        }

        if (iconImage == null)
        {
            iconImage = GetComponentInChildren<Image>(true);
        }

        if (iconRoot == null && iconImage != null)
        {
            iconRoot = iconImage.gameObject;
        }
    }

    private void SetIconVisible(bool isVisible)
    {
        GameObject target = iconRoot != null
            ? iconRoot
            : iconImage != null ? iconImage.gameObject : null;

        if (target != null && target != gameObject && target.activeSelf != isVisible)
        {
            target.SetActive(isVisible);
        }
    }
}
