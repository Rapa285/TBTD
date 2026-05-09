
using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Shows the closest weapon evolutions related to a focused multi-upgrade line.
/// </summary>
public class EvoHintUI : MonoBehaviour
{
    [Serializable]
    private sealed class EvolutionHintSlot
    {
        [SerializeField, Tooltip("Optional root toggled when this slot has data.")]
        private GameObject root;

        [SerializeField, Tooltip("Icon presenter for the evolution's resolved upgrade.")]
        private GenericIconDisplay evolutionIcon;

        [SerializeField, Tooltip("Level presenter for the other prerequisite multi-upgrade.")]
        private UpgradeIconLevelUI relatedUpgrade;

        public void Bind(EvolutionSO evolution, EvolutionSO.Prerequisite relatedPrerequisite, UnitStateManager.OwnedUnitState unit)
        {
            if (evolutionIcon != null)
            {
                evolutionIcon.Bind(evolution != null ? evolution.ResolvedUpgrade : null);
            }

            if (relatedUpgrade != null)
            {
                if (relatedPrerequisite.MultiUpgrade != null)
                {
                    relatedUpgrade.BindRequirement(
                        relatedPrerequisite.MultiUpgrade,
                        unit,
                        relatedPrerequisite.MinimumLevel);
                }
                else
                {
                    relatedUpgrade.Clear();
                }
            }

            SetVisible(evolution != null);
        }

        public void BindRelatedUpgrade(EvolutionSO.Prerequisite prerequisite, UnitStateManager.OwnedUnitState unit)
        {
            if (evolutionIcon != null)
            {
                evolutionIcon.Clear();
            }

            if (relatedUpgrade != null)
            {
                if (prerequisite.MultiUpgrade != null)
                {
                    relatedUpgrade.BindRequirement(
                        prerequisite.MultiUpgrade,
                        unit,
                        prerequisite.MinimumLevel);
                }
                else
                {
                    relatedUpgrade.Clear();
                }
            }

            SetVisible(prerequisite.MultiUpgrade != null);
        }

        public void Clear()
        {
            if (evolutionIcon != null)
            {
                evolutionIcon.Clear();
            }

            if (relatedUpgrade != null)
            {
                relatedUpgrade.Clear();
            }

            SetVisible(false);
        }

        private void SetVisible(bool isVisible)
        {
            if (root != null && root.activeSelf != isVisible)
            {
                root.SetActive(isVisible);
            }
        }
    }

    private readonly struct RankedEvolution
    {
        public EvolutionSO Evolution { get; }
        public EvolutionSO.Prerequisite RelatedPrerequisite { get; }
        public int MissingLevels { get; }
        public int MetPrerequisiteCount { get; }
        public int PoolIndex { get; }

        public RankedEvolution(
            EvolutionSO evolution,
            EvolutionSO.Prerequisite relatedPrerequisite,
            int missingLevels,
            int metPrerequisiteCount,
            int poolIndex)
        {
            Evolution = evolution;
            RelatedPrerequisite = relatedPrerequisite;
            MissingLevels = missingLevels;
            MetPrerequisiteCount = metPrerequisiteCount;
            PoolIndex = poolIndex;
        }
    }

    [SerializeField, Tooltip("Upgrade manager that owns the shared evolution pool.")]
    private UpgradesManager upgradesManager;

    [SerializeField, Tooltip("Roster manager used to read the active unit's current upgrade levels.")]
    private UnitStateManager unitStateManager;

    [SerializeField, Tooltip("Middle display for the currently hovered or focused multi-upgrade.")]
    private UpgradeIconLevelUI focusedUpgrade;

    [SerializeField, Tooltip("Target evolution icon shown when the focused choice is an evolution.")]
    private GenericIconDisplay targetEvo;

    [SerializeField, Tooltip("First displayed related evolution slot.")]
    private EvolutionHintSlot firstSlot = new EvolutionHintSlot();

    [SerializeField, Tooltip("Second displayed related evolution slot.")]
    private EvolutionHintSlot secondSlot = new EvolutionHintSlot();

    private void Awake()
    {
        ResolveReferences();
        Clear();
    }

    private void OnValidate()
    {
        ResolveReferences();
    }

    public void Bind(MultiUpgradeSO sourceUpgrade, string unitId)
    {
        ResolveReferences();

        if (sourceUpgrade == null || upgradesManager == null)
        {
            Clear();
            return;
        }

        UnitStateManager.OwnedUnitState unit = ResolveUnit(unitId);
        if (unit != null && unit.HasSelectedEvolution)
        {
            Clear();
            return;
        }

        if (focusedUpgrade != null)
        {
            focusedUpgrade.Bind(sourceUpgrade, unit);
        }

        if (targetEvo != null)
        {
            targetEvo.Clear();
        }

        List<RankedEvolution> relatedEvolutions = BuildRankedEvolutions(sourceUpgrade, unit);
        if (relatedEvolutions.Count == 0)
        {
            firstSlot.Clear();
            secondSlot.Clear();
            return;
        }

        firstSlot.Bind(relatedEvolutions[0].Evolution, relatedEvolutions[0].RelatedPrerequisite, unit);

        if (relatedEvolutions.Count > 1)
        {
            secondSlot.Bind(relatedEvolutions[1].Evolution, relatedEvolutions[1].RelatedPrerequisite, unit);
        }
        else
        {
            secondSlot.Clear();
        }
    }

    public void Bind(EvolutionSO evolution, string unitId)
    {
        ResolveReferences();

        if (evolution == null || !evolution.HasResolvedUpgrade)
        {
            Clear();
            return;
        }

        UnitStateManager.OwnedUnitState unit = ResolveUnit(unitId);

        if (focusedUpgrade != null)
        {
            focusedUpgrade.Clear();
        }

        if (targetEvo != null)
        {
            targetEvo.Bind(evolution.ResolvedUpgrade);
        }

        IReadOnlyList<EvolutionSO.Prerequisite> prerequisites = evolution.Prerequisites;
        if (prerequisites != null && prerequisites.Count > 0)
        {
            firstSlot.BindRelatedUpgrade(prerequisites[0], unit);
        }
        else
        {
            firstSlot.Clear();
        }

        if (prerequisites != null && prerequisites.Count > 1)
        {
            secondSlot.BindRelatedUpgrade(prerequisites[1], unit);
        }
        else
        {
            secondSlot.Clear();
        }
    }

    public void Clear()
    {
        if (focusedUpgrade != null)
        {
            focusedUpgrade.Clear();
        }

        if (targetEvo != null)
        {
            targetEvo.Clear();
        }

        firstSlot.Clear();
        secondSlot.Clear();
    }

    private List<RankedEvolution> BuildRankedEvolutions(
        MultiUpgradeSO sourceUpgrade,
        UnitStateManager.OwnedUnitState unit)
    {
        List<RankedEvolution> relatedEvolutions = new List<RankedEvolution>();
        IReadOnlyList<EvolutionSO> evolutionPool = upgradesManager.EvolutionPool;
        if (evolutionPool == null)
        {
            return relatedEvolutions;
        }

        for (int i = 0; i < evolutionPool.Count; i++)
        {
            EvolutionSO evolution = evolutionPool[i];
            if (evolution == null || !evolution.HasResolvedUpgrade)
            {
                continue;
            }

            if (TryBuildRankedEvolution(
                evolution,
                sourceUpgrade,
                unit,
                i,
                out RankedEvolution rankedEvolution))
            {
                relatedEvolutions.Add(rankedEvolution);
            }
        }

        relatedEvolutions.Sort(CompareRankedEvolutions);
        return relatedEvolutions;
    }

    private static bool TryBuildRankedEvolution(
        EvolutionSO evolution,
        MultiUpgradeSO sourceUpgrade,
        UnitStateManager.OwnedUnitState unit,
        int poolIndex,
        out RankedEvolution rankedEvolution)
    {
        rankedEvolution = default;

        bool containsSource = false;
        int missingLevels = 0;
        int metPrerequisiteCount = 0;
        EvolutionSO.Prerequisite relatedPrerequisite = default;

        IReadOnlyList<EvolutionSO.Prerequisite> prerequisites = evolution.Prerequisites;
        for (int i = 0; i < prerequisites.Count; i++)
        {
            EvolutionSO.Prerequisite prerequisite = prerequisites[i];
            MultiUpgradeSO prerequisiteUpgrade = prerequisite.MultiUpgrade;
            if (prerequisiteUpgrade == null)
            {
                missingLevels += prerequisite.MinimumLevel;
                continue;
            }

            int currentLevel = unit != null
                ? unit.GetAppliedMultiUpgradeLevel(prerequisiteUpgrade)
                : 0;
            int requiredLevel = prerequisite.MinimumLevel;

            if (currentLevel >= requiredLevel)
            {
                metPrerequisiteCount++;
            }
            else
            {
                missingLevels += requiredLevel - currentLevel;
            }

            if (prerequisiteUpgrade == sourceUpgrade)
            {
                containsSource = true;
            }
            else if (relatedPrerequisite.MultiUpgrade == null)
            {
                relatedPrerequisite = prerequisite;
            }
        }

        if (!containsSource)
        {
            return false;
        }

        rankedEvolution = new RankedEvolution(
            evolution,
            relatedPrerequisite,
            missingLevels,
            metPrerequisiteCount,
            poolIndex);
        return true;
    }

    private static int CompareRankedEvolutions(RankedEvolution left, RankedEvolution right)
    {
        int missingComparison = left.MissingLevels.CompareTo(right.MissingLevels);
        if (missingComparison != 0)
        {
            return missingComparison;
        }

        int metComparison = right.MetPrerequisiteCount.CompareTo(left.MetPrerequisiteCount);
        if (metComparison != 0)
        {
            return metComparison;
        }

        return left.PoolIndex.CompareTo(right.PoolIndex);
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
        if (upgradesManager == null)
        {
            ServiceLocator.TryResolve(out upgradesManager);
        }

        if (unitStateManager == null)
        {
            ServiceLocator.TryResolve(out unitStateManager);
        }
    }
}
