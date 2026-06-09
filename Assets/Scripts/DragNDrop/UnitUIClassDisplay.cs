using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Displays the current class icon for one managed unit item.
/// </summary>
[RequireComponent(typeof(UIUnitItem))]
public class UnitUIClassDisplay : UnitUIBehaviour
{
    [SerializeField, Tooltip("Image used to display the unit's current evolution or upgrade class icon.")]
    private Image iconImage;

    [SerializeField, Tooltip("Fallback icon displayed for managed units with no evolution or upgrade class icon.")]
    private Sprite baseClassIcon;

    [SerializeField, Tooltip("Optional root shown only when a class icon is available. Defaults to the image object.")]
    private GameObject iconRoot;

    protected override void Awake()
    {
        base.Awake();
        RefreshDisplay();
    }

    protected override void Start()
    {
        base.Start();
        RefreshDisplay();
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        RefreshDisplay();
    }

    protected override void OnDisable()
    {
        if (Application.isPlaying)
        {
            ClearDisplay();
        }

        base.OnDisable();
    }

    protected override void OnValidate()
    {
        base.OnValidate();
        ResolveDisplayReferences();
    }

    protected override void ResolveReferences()
    {
        base.ResolveReferences();
        ResolveDisplayReferences();
    }

    protected override void SubscribeToEvents(UnitEventBus eventBus)
    {
        eventBus.UnitUpgradeSelected += HandleUnitUpgradeSelected;
    }

    protected override void UnsubscribeFromEvents(UnitEventBus eventBus)
    {
        eventBus.UnitUpgradeSelected -= HandleUnitUpgradeSelected;
    }

    private void HandleUnitUpgradeSelected(UnitUpgradeSelectedEvent eventData)
    {
        if (IsMatchingUnit(eventData.UnitId))
        {
            RefreshDisplay();
        }
    }

    private void RefreshDisplay()
    {
        ResolveReferences();

        if (!TryGetManagedUnit(out UnitStateManager.OwnedUnitState unit)
            || !TryGetClassIcon(unit, out Sprite icon))
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

    private bool TryGetClassIcon(UnitStateManager.OwnedUnitState unit, out Sprite icon)
    {
        icon = null;
        if (unit == null)
        {
            return false;
        }

        UpgradeSO evolutionUpgrade = unit.SelectedEvolution != null
            ? unit.SelectedEvolution.ResolvedUpgrade
            : null;
        if (evolutionUpgrade != null && evolutionUpgrade.Icon != null)
        {
            icon = evolutionUpgrade.Icon;
            return true;
        }

        if (TryGetHighestMultiUpgradeIcon(unit, out icon))
        {
            return true;
        }

        icon = baseClassIcon;
        return icon != null;
    }

    private bool TryGetHighestMultiUpgradeIcon(UnitStateManager.OwnedUnitState unit, out Sprite icon)
    {
        icon = null;

        IReadOnlyList<UnitStateManager.AppliedMultiUpgradeState> appliedUpgrades = unit.AppliedMultiUpgrades;
        if (appliedUpgrades == null || appliedUpgrades.Count == 0)
        {
            return false;
        }

        MultiUpgradeSO selectedMultiUpgrade = null;
        int selectedLevel = 0;
        Sprite selectedIcon = null;

        for (int i = 0; i < appliedUpgrades.Count; i++)
        {
            UnitStateManager.AppliedMultiUpgradeState appliedUpgrade = appliedUpgrades[i];
            if (!TryResolveAppliedUpgradeIcon(appliedUpgrade, out Sprite candidateIcon))
            {
                continue;
            }

            int candidateLevel = appliedUpgrade.Level;
            MultiUpgradeSO candidateMultiUpgrade = appliedUpgrade.MultiUpgrade;
            if (candidateLevel > selectedLevel
                || (candidateLevel == selectedLevel
                    && ShouldPreferTie(unit, candidateMultiUpgrade, selectedMultiUpgrade)))
            {
                selectedMultiUpgrade = candidateMultiUpgrade;
                selectedLevel = candidateLevel;
                selectedIcon = candidateIcon;
            }
        }

        icon = selectedIcon;
        return icon != null;
    }

    private static bool TryResolveAppliedUpgradeIcon(
        UnitStateManager.AppliedMultiUpgradeState appliedUpgrade,
        out Sprite icon)
    {
        icon = null;

        if (appliedUpgrade == null
            || appliedUpgrade.MultiUpgrade == null
            || appliedUpgrade.Level <= 0
            || !appliedUpgrade.MultiUpgrade.TryGetLevelUpgrade(appliedUpgrade.Level, out UpgradeSO upgrade)
            || upgrade == null
            || upgrade.Icon == null)
        {
            return false;
        }

        icon = upgrade.Icon;
        return true;
    }

    private static bool ShouldPreferTie(
        UnitStateManager.OwnedUnitState unit,
        MultiUpgradeSO candidate,
        MultiUpgradeSO current)
    {
        if (candidate == null)
        {
            return false;
        }

        MultiUpgradeSO latestSelectedMultiUpgrade = unit.LatestSelectedMultiUpgrade;
        if (latestSelectedMultiUpgrade != null)
        {
            return candidate == latestSelectedMultiUpgrade
                && current != latestSelectedMultiUpgrade;
        }

        return true;
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

    private void ResolveDisplayReferences()
    {
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
