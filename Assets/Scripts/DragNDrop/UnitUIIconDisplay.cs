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

    [SerializeField, Tooltip("Optional root used to resolve the UI graphic tinted by deployment state.")]
    private GameObject tintTargetRoot;

    [SerializeField, Tooltip("Graphic tinted by deployment state. Supports Image, RawImage, and any UI Graphic.")]
    private Graphic tintGraphic;

    [SerializeField, Tooltip("Color applied while the managed unit is not deployed.")]
    private Color undeployedColor = Color.gray;

    [SerializeField, Tooltip("Color applied while the managed unit is deployed.")]
    private Color deployedColor = Color.white;

    [SerializeField, Tooltip("Event bus used to refresh tint when deployment state changes.")]
    private UnitEventBus eventBus;

    private bool eventBusSubscribed;
    private bool hasCapturedDefaultIconSprite;
    private Sprite defaultIconSprite;

    private void Awake()
    {
        ResolveReferences();
        RefreshDisplay();
    }

    private void Start()
    {
        if (Application.isPlaying)
        {
            SubscribeToEventBusIfNeeded();
        }

        RefreshDisplay();
    }

    private void OnEnable()
    {
        ResolveReferences();

        if (Application.isPlaying)
        {
            SubscribeToEventBusIfNeeded();
        }

        RefreshDisplay();
    }

    private void OnDisable()
    {
        UnsubscribeFromEventBusIfNeeded();
    }

    private void OnDestroy()
    {
        UnsubscribeFromEventBusIfNeeded();
    }

    private void OnValidate()
    {
        ResolveReferences();
    }

    public void RefreshDisplay()
    {
        ResolveReferences();

        if (!TryGetDisplayUnit(out UnitStateManager.OwnedUnitState unit))
        {
            ClearDisplay();
            return;
        }

        SetIconVisible(true);
        ApplyDeploymentTint();

        if (iconImage != null)
        {
            iconImage.sprite = unit.Icon != null ? unit.Icon : defaultIconSprite;
            iconImage.enabled = true;
        }
    }

    private bool TryGetDisplayUnit(out UnitStateManager.OwnedUnitState unit)
    {
        if (uiUnitItem == null
            || !uiUnitItem.IsManagedUnitConfigured
            || !uiUnitItem.TryGetOwnedUnit(out unit))
        {
            unit = null;
            return false;
        }

        return true;
    }

    private void ClearDisplay()
    {
        SetIconVisible(false);
        ApplyTint(deployedColor);

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

        CaptureDefaultIconSprite();

        if (iconRoot == null && iconImage != null)
        {
            iconRoot = iconImage.gameObject;
        }

        if (tintGraphic == null)
        {
            if (tintTargetRoot != null)
            {
                tintGraphic = tintTargetRoot.GetComponent<Graphic>();
            }

            if (tintGraphic == null)
            {
                tintGraphic = iconImage;
            }
        }

        if (eventBus == null)
        {
            ServiceLocator.TryResolve(out eventBus);
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

    private void ApplyDeploymentTint()
    {
        Color color = uiUnitItem != null && uiUnitItem.IsDeployed
            ? deployedColor
            : undeployedColor;

        ApplyTint(color);
    }

    private void ApplyTint(Color color)
    {
        if (tintGraphic != null)
        {
            tintGraphic.color = color;
        }
    }

    private void SubscribeToEventBusIfNeeded()
    {
        if (eventBusSubscribed)
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

        eventBus.UnitDeployed += HandleUnitDeployed;
        eventBus.UnitRecalled += HandleUnitRecalled;
        eventBusSubscribed = true;
    }

    private void UnsubscribeFromEventBusIfNeeded()
    {
        if (!eventBusSubscribed || eventBus == null)
        {
            eventBusSubscribed = false;
            return;
        }

        eventBus.UnitDeployed -= HandleUnitDeployed;
        eventBus.UnitRecalled -= HandleUnitRecalled;
        eventBusSubscribed = false;
    }

    private void HandleUnitDeployed(UnitDeployedEvent eventData)
    {
        if (IsMatchingUnit(eventData.UnitId))
        {
            RefreshDisplay();
        }
    }

    private void HandleUnitRecalled(UnitRecalledEvent eventData)
    {
        if (IsMatchingUnit(eventData.UnitId))
        {
            RefreshDisplay();
        }
    }

    private bool IsMatchingUnit(string unitId)
    {
        return uiUnitItem != null
            && uiUnitItem.IsManagedUnit
            && uiUnitItem.UnitId == unitId;
    }

    private void CaptureDefaultIconSprite()
    {
        if (hasCapturedDefaultIconSprite || iconImage == null)
        {
            return;
        }

        defaultIconSprite = iconImage.sprite;
        hasCapturedDefaultIconSprite = true;
    }
}
