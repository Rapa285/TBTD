using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Displays active multi-upgrade lines for one owned unit. Evolution display belongs to UnitDetailsUI.
/// </summary>
public class UnitUpgradeListUI : MonoBehaviour
{
    [SerializeField, Tooltip("Optional root toggled when the selected unit has at least one visible multi-upgrade. Defaults to this object.")]
    private GameObject root;

    [SerializeField, Tooltip("Optional explicit slots used for multi-upgrade levels only. If empty, child slots are resolved automatically.")]
    private UpgradeIconLevelUI[] multiUpgradeSlots;

    private readonly List<UpgradeIconLevelUI> resolvedMultiUpgradeSlots = new List<UpgradeIconLevelUI>();

    private void Awake()
    {
        ResolveReferences();
    }

    private void OnValidate()
    {
        ResolveReferences();
    }

    public void Bind(UnitStateManager.OwnedUnitState unit)
    {
        ResolveReferences();
        ClearSlots();

        if (unit == null)
        {
            SetRootVisible(false);
            return;
        }

        SetRootVisible(BindMultiUpgrades(unit));
    }

    public void Clear()
    {
        ResolveReferences();
        ClearSlots();
        SetRootVisible(false);
    }

    private bool BindMultiUpgrades(UnitStateManager.OwnedUnitState unit)
    {
        IReadOnlyList<UnitStateManager.AppliedMultiUpgradeState> appliedUpgrades = unit.AppliedMultiUpgrades;
        int slotIndex = 0;
        bool hasVisibleUpgrade = false;

        if (appliedUpgrades == null)
        {
            return false;
        }

        for (int i = 0; i < appliedUpgrades.Count; i++)
        {
            UnitStateManager.AppliedMultiUpgradeState appliedUpgrade = appliedUpgrades[i];
            if (appliedUpgrade == null
                || appliedUpgrade.MultiUpgrade == null
                || appliedUpgrade.Level <= 0)
            {
                continue;
            }

            if (slotIndex >= resolvedMultiUpgradeSlots.Count)
            {
                break;
            }

            UpgradeIconLevelUI slot = resolvedMultiUpgradeSlots[slotIndex];
            if (slot == null)
            {
                continue;
            }

            SetSlotActive(slot, true);
            slot.Bind(appliedUpgrade.MultiUpgrade, unit);
            slotIndex++;
            hasVisibleUpgrade = true;
        }

        return hasVisibleUpgrade;
    }

    private void ClearSlots()
    {
        for (int i = 0; i < resolvedMultiUpgradeSlots.Count; i++)
        {
            ClearAndHideSlot(resolvedMultiUpgradeSlots[i]);
        }
    }

    private void ResolveReferences()
    {
        if (root == null)
        {
            root = gameObject;
        }

        RebuildResolvedMultiUpgradeSlots();
    }

    private void RebuildResolvedMultiUpgradeSlots()
    {
        resolvedMultiUpgradeSlots.Clear();

        if (multiUpgradeSlots != null && multiUpgradeSlots.Length > 0)
        {
            for (int i = 0; i < multiUpgradeSlots.Length; i++)
            {
                AddMultiUpgradeSlotIfValid(multiUpgradeSlots[i]);
            }

            return;
        }

        UpgradeIconLevelUI[] childSlots = GetComponentsInChildren<UpgradeIconLevelUI>(true);
        for (int i = 0; i < childSlots.Length; i++)
        {
            AddMultiUpgradeSlotIfValid(childSlots[i]);
        }
    }

    private void AddMultiUpgradeSlotIfValid(UpgradeIconLevelUI slot)
    {
        if (slot == null || resolvedMultiUpgradeSlots.Contains(slot))
        {
            return;
        }

        resolvedMultiUpgradeSlots.Add(slot);
    }

    private static void ClearAndHideSlot(UpgradeIconLevelUI slot)
    {
        if (slot == null)
        {
            return;
        }

        slot.Clear();
        SetSlotActive(slot, false);
    }

    private static void SetSlotActive(UpgradeIconLevelUI slot, bool isActive)
    {
        if (slot != null && slot.gameObject.activeSelf != isActive)
        {
            slot.gameObject.SetActive(isActive);
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
