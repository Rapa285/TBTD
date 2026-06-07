using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Displays one multi-upgrade line icon with the current unit level for that line.
/// </summary>
public class UpgradeIconLevelUI : MonoBehaviour
{
    [SerializeField, Tooltip("Optional root toggled when the display has valid data or placeholder content. Defaults to this object.")]
    private GameObject root;

    [SerializeField, Tooltip("Image used to display the resolved level upgrade icon.")]
    private Image iconImage;

    [SerializeField, Tooltip("Text used for the upgrade level label.")]
    private TMP_Text levelText;

    [SerializeField, Tooltip("Optional hover data source populated when this slot binds an upgrade or evolution.")]
    private UpgradeHoverableItem hoverableItem;

    private UnitStateManager unitStateManager;

    private void Awake()
    {
        ResolveReferences();
    }

    private void OnValidate()
    {
        ResolveReferences();
    }

    public void Bind(MultiUpgradeSO multiUpgrade, string unitId)
    {
        BindInternal(multiUpgrade, ResolveUnit(unitId), 0, false);
    }

    public void Bind(MultiUpgradeSO multiUpgrade, UnitStateManager.OwnedUnitState unit)
    {
        BindInternal(multiUpgrade, unit, 0, false);
    }

    public void BindRequirement(MultiUpgradeSO multiUpgrade, string unitId, int requiredLevel)
    {
        BindInternal(multiUpgrade, ResolveUnit(unitId), requiredLevel, true);
    }

    public void BindRequirement(
        MultiUpgradeSO multiUpgrade,
        UnitStateManager.OwnedUnitState unit,
        int requiredLevel)
    {
        BindInternal(multiUpgrade, unit, requiredLevel, true);
    }

    public void BindEvolution(EvolutionSO evolution)
    {
        BindEvolution(evolution, null);
    }

    public void BindEvolution(EvolutionSO evolution, UnitStateManager.OwnedUnitState unit)
    {
        ResolveReferences();

        if (evolution == null || !evolution.HasResolvedUpgrade)
        {
            Clear();
            return;
        }

        UpgradeSO resolvedUpgrade = evolution.ResolvedUpgrade;
        if (iconImage != null)
        {
            Sprite icon = resolvedUpgrade != null ? resolvedUpgrade.Icon : null;
            iconImage.sprite = icon;
            iconImage.enabled = icon != null;
        }

        if (levelText != null)
        {
            levelText.text = "EVO";
        }

        if (hoverableItem != null)
        {
            if (hoverableItem is DetailUpgradeHoverableItem detailHoverableItem)
            {
                detailHoverableItem.Bind(evolution, unit);
            }
            else
            {
                hoverableItem.Bind(evolution);
            }
        }

        SetRootVisible(true);
    }

    /// <summary>
    /// Shows this slot without an upgrade icon, for empty states such as an unevolved unit.
    /// </summary>
    public void BindPlaceholder(string label)
    {
        ResolveReferences();

        if (iconImage != null)
        {
            iconImage.sprite = null;
            iconImage.enabled = false;
        }

        if (levelText != null)
        {
            levelText.text = label ?? string.Empty;
        }

        if (hoverableItem != null)
        {
            ClearHoverableItem();
        }

        SetRootVisible(true);
    }

    public void Clear()
    {
        if (iconImage != null)
        {
            iconImage.sprite = null;
            iconImage.enabled = false;
        }

        if (levelText != null)
        {
            levelText.text = string.Empty;
        }

        if (hoverableItem != null)
        {
            ClearHoverableItem();
        }

        SetRootVisible(false);
    }

    public DetailUpgradeHoverableItem EnsureDetailHoverableItem()
    {
        ResolveReferences();

        if (hoverableItem is DetailUpgradeHoverableItem existingDetailHoverableItem)
        {
            return existingDetailHoverableItem;
        }

        if (!TryGetComponent(out DetailUpgradeHoverableItem detailHoverableItem))
        {
            detailHoverableItem = gameObject.AddComponent<DetailUpgradeHoverableItem>();
        }

        if (hoverableItem != null)
        {
            hoverableItem.enabled = false;
        }

        hoverableItem = detailHoverableItem;
        hoverableItem.enabled = true;
        return detailHoverableItem;
    }

    private void BindInternal(
        MultiUpgradeSO multiUpgrade,
        UnitStateManager.OwnedUnitState unit,
        int requiredLevel,
        bool isRequirement)
    {
        ResolveReferences();

        if (multiUpgrade == null)
        {
            Clear();
            return;
        }

        int currentLevel = unit != null ? unit.GetAppliedMultiUpgradeLevel(multiUpgrade) : 0;
        int targetIconLevel = isRequirement
            ? Mathf.Max(1, requiredLevel)
            : Mathf.Max(1, currentLevel);

        if (!multiUpgrade.TryGetLevelUpgrade(targetIconLevel, out UpgradeSO iconUpgrade)
            && !multiUpgrade.TryGetLevelUpgrade(1, out iconUpgrade))
        {
            iconUpgrade = null;
        }

        if (iconImage != null)
        {
            Sprite icon = iconUpgrade != null ? iconUpgrade.Icon : null;
            iconImage.sprite = icon;
            iconImage.enabled = icon != null;
        }

        if (levelText != null)
        {
            levelText.text = isRequirement
                ? $"LVL {currentLevel}/{Mathf.Max(1, requiredLevel)}"
                : $"LVL {currentLevel}";
        }

        if (hoverableItem != null)
        {
            if (hoverableItem is DetailUpgradeHoverableItem detailHoverableItem)
            {
                detailHoverableItem.Bind(multiUpgrade, targetIconLevel, currentLevel, unit);
            }
            else
            {
                hoverableItem.Bind(multiUpgrade, targetIconLevel, currentLevel);
            }
        }

        SetRootVisible(true);
    }

    private UnitStateManager.OwnedUnitState ResolveUnit(string unitId)
    {
        if (string.IsNullOrWhiteSpace(unitId))
        {
            return null;
        }

        if (unitStateManager == null)
        {
            ServiceLocator.TryResolve(out unitStateManager);
        }

        return unitStateManager != null
            && unitStateManager.TryGetUnit(unitId, out UnitStateManager.OwnedUnitState unit)
                ? unit
                : null;
    }

    private void ResolveReferences()
    {
        if (root == null)
        {
            root = gameObject;
        }

        if (iconImage == null)
        {
            iconImage = GetComponentInChildren<Image>(true);
        }

        if (levelText == null)
        {
            levelText = GetComponentInChildren<TMP_Text>(true);
        }

        if (hoverableItem == null)
        {
            hoverableItem = GetComponent<UpgradeHoverableItem>();
        }
    }

    private void ClearHoverableItem()
    {
        if (hoverableItem is DetailUpgradeHoverableItem detailHoverableItem)
        {
            detailHoverableItem.Clear();
            return;
        }

        hoverableItem.Clear();
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
