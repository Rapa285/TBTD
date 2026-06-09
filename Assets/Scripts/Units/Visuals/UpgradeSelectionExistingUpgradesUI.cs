using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Displays the active upgrade selection unit's already purchased upgrades.
/// </summary>
public class UpgradeSelectionExistingUpgradesUI : MonoBehaviour
{
    [SerializeField, Tooltip("Optional root toggled when the active unit has existing upgrades. Defaults to this object.")]
    private GameObject root;

    [SerializeField, Tooltip("Parent transform for instantiated upgrade slots. Defaults to this transform.")]
    private Transform slotsRoot;

    [SerializeField, Tooltip("Slot prefab used for purchased multi-upgrade lines.")]
    private UpgradeIconLevelUI normalUpgradeSlotPrefab;

    [SerializeField, Tooltip("Slot prefab used for the selected evolution.")]
    private UpgradeIconLevelUI evolutionSlotPrefab;

    [SerializeField, Tooltip("Roster manager used to resolve active unit upgrade state.")]
    private UnitStateManager unitStateManager;

    private readonly List<UpgradeIconLevelUI> normalSlots = new List<UpgradeIconLevelUI>();
    private readonly List<UpgradeIconLevelUI> evolutionSlots = new List<UpgradeIconLevelUI>();

    private void Awake()
    {
        ResolveReferences();
        Clear();
    }

    private void OnValidate()
    {
        ResolveReferences();
    }

    public void Bind(string unitId)
    {
        ResolveReferences();
        ResolveTemplatePrefabs();
        ClearSlots();

        if (string.IsNullOrWhiteSpace(unitId)
            || unitStateManager == null
            || !unitStateManager.TryGetUnit(unitId, out UnitStateManager.OwnedUnitState unit))
        {
            SetRootVisible(false);
            return;
        }

        int visibleCount = BindMultiUpgrades(unit);
        if (unit.SelectedEvolution != null)
        {
            UpgradeIconLevelUI evolutionSlot = GetEvolutionSlot(0);
            if (evolutionSlot != null)
            {
                SetSlotActive(evolutionSlot, true);
                evolutionSlot.BindEvolution(unit.SelectedEvolution, unit);
                visibleCount++;
            }
        }

        SetRootVisible(visibleCount > 0);
    }

    public void Clear()
    {
        ResolveReferences();
        ClearSlots();
        SetRootVisible(false);
    }

    public void Configure(
        GameObject root,
        Transform slotsRoot,
        UpgradeIconLevelUI normalUpgradeSlotPrefab,
        UpgradeIconLevelUI evolutionSlotPrefab)
    {
        if (root != null)
        {
            this.root = root;
        }

        if (slotsRoot != null)
        {
            this.slotsRoot = slotsRoot;
        }

        if (normalUpgradeSlotPrefab != null)
        {
            this.normalUpgradeSlotPrefab = normalUpgradeSlotPrefab;
        }

        if (evolutionSlotPrefab != null)
        {
            this.evolutionSlotPrefab = evolutionSlotPrefab;
        }
    }

    private int BindMultiUpgrades(UnitStateManager.OwnedUnitState unit)
    {
        IReadOnlyList<UnitStateManager.AppliedMultiUpgradeState> appliedUpgrades = unit.AppliedMultiUpgrades;
        if (appliedUpgrades == null)
        {
            return 0;
        }

        int visibleCount = 0;
        for (int i = 0; i < appliedUpgrades.Count; i++)
        {
            UnitStateManager.AppliedMultiUpgradeState appliedUpgrade = appliedUpgrades[i];
            if (appliedUpgrade == null
                || appliedUpgrade.MultiUpgrade == null
                || appliedUpgrade.Level <= 0)
            {
                continue;
            }

            UpgradeIconLevelUI slot = GetNormalSlot(visibleCount);
            if (slot == null)
            {
                continue;
            }

            SetSlotActive(slot, true);
            slot.Bind(appliedUpgrade.MultiUpgrade, unit);
            visibleCount++;
        }

        return visibleCount;
    }

    private UpgradeIconLevelUI GetNormalSlot(int index)
    {
        return GetSlot(index, normalSlots, normalUpgradeSlotPrefab);
    }

    private UpgradeIconLevelUI GetEvolutionSlot(int index)
    {
        return GetSlot(index, evolutionSlots, evolutionSlotPrefab);
    }

    private UpgradeIconLevelUI GetSlot(
        int index,
        List<UpgradeIconLevelUI> slots,
        UpgradeIconLevelUI prefab)
    {
        while (slots.Count <= index)
        {
            if (prefab == null || slotsRoot == null)
            {
                return null;
            }

            UpgradeIconLevelUI slot = Instantiate(prefab, slotsRoot);
            slot.EnsureDetailHoverableItem();
            slot.gameObject.SetActive(false);
            slots.Add(slot);
        }

        return slots[index];
    }

    private void ClearSlots()
    {
        for (int i = 0; i < normalSlots.Count; i++)
        {
            ClearAndHideSlot(normalSlots[i]);
        }

        for (int i = 0; i < evolutionSlots.Count; i++)
        {
            ClearAndHideSlot(evolutionSlots[i]);
        }
    }

    private void ResolveReferences()
    {
        if (root == null)
        {
            root = gameObject;
        }

        if (slotsRoot == null)
        {
            slotsRoot = transform;
        }

        if (unitStateManager == null)
        {
            ServiceLocator.TryResolve(out unitStateManager);
        }
    }

    private void ResolveTemplatePrefabs()
    {
        if (normalUpgradeSlotPrefab != null && evolutionSlotPrefab != null)
        {
            return;
        }

        Transform searchRoot = transform.root != null ? transform.root : transform;
        UpgradeIconLevelUI[] candidateSlots = searchRoot.GetComponentsInChildren<UpgradeIconLevelUI>(true);
        for (int i = 0; i < candidateSlots.Length; i++)
        {
            UpgradeIconLevelUI candidate = candidateSlots[i];
            if (!IsTemplateCandidate(candidate))
            {
                continue;
            }

            string candidateName = candidate.gameObject.name;
            bool looksLikeEvolution = candidateName.Contains("Evolution") || candidateName.Contains("Evo");

            if (evolutionSlotPrefab == null && looksLikeEvolution)
            {
                evolutionSlotPrefab = candidate;
                continue;
            }

            if (normalUpgradeSlotPrefab == null && !looksLikeEvolution)
            {
                normalUpgradeSlotPrefab = candidate;
            }
        }

        for (int i = 0; i < candidateSlots.Length; i++)
        {
            UpgradeIconLevelUI candidate = candidateSlots[i];
            if (!IsTemplateCandidate(candidate))
            {
                continue;
            }

            if (normalUpgradeSlotPrefab == null)
            {
                normalUpgradeSlotPrefab = candidate;
            }

            if (evolutionSlotPrefab == null)
            {
                evolutionSlotPrefab = candidate;
            }

            if (normalUpgradeSlotPrefab != null && evolutionSlotPrefab != null)
            {
                return;
            }
        }
    }

    private bool IsTemplateCandidate(UpgradeIconLevelUI candidate)
    {
        if (candidate == null || candidate.transform == transform)
        {
            return false;
        }

        return slotsRoot == null || !candidate.transform.IsChildOf(slotsRoot);
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
