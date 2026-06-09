using System.Collections.Generic;
using System.Text;
using UnityEngine;

/// <summary>
/// Upgrade hover source that can populate richer tooltip panels using roster unit context.
/// </summary>
public class DetailUpgradeHoverableItem : UpgradeHoverableItem
{
    private readonly List<RankedEvolution> relatedEvolutions = new List<RankedEvolution>();
    private readonly StringBuilder builder = new StringBuilder();

    private UnitStateManager.OwnedUnitState unit;
    private UpgradesManager upgradesManager;

    public UnitStateManager.OwnedUnitState Unit => unit;

    public void Bind(UnitUpgradeOfferChoice choice, UnitStateManager.OwnedUnitState unit)
    {
        this.unit = unit;
        base.Bind(choice);
    }

    public void Bind(UpgradeSO upgrade, UnitStateManager.OwnedUnitState unit)
    {
        this.unit = unit;
        base.Bind(upgrade);
    }

    public void Bind(MultiUpgradeSO multiUpgrade, int targetLevel, int currentLevel, UnitStateManager.OwnedUnitState unit)
    {
        this.unit = unit;
        base.Bind(multiUpgrade, targetLevel, currentLevel);
    }

    public void Bind(EvolutionSO evolution, UnitStateManager.OwnedUnitState unit)
    {
        this.unit = unit;
        base.Bind(evolution);
    }

    public new void Clear()
    {
        unit = null;
        relatedEvolutions.Clear();
        base.Clear();
    }

    public void BindStats(UpgradeStatInfoUI statInfoUI)
    {
        if (statInfoUI == null)
        {
            return;
        }

        if (IsMultiUpgrade && MultiUpgrade != null && NextLevel > 0)
        {
            statInfoUI.Bind(MultiUpgrade, NextLevel);
            return;
        }

        statInfoUI.Bind(Upgrade);
    }

    public string BuildStatDetailsText()
    {
        builder.Clear();

        if (IsMultiUpgrade
            && MultiUpgrade != null
            && NextLevel > 0
            && MultiUpgrade.TryGetLevelUpgrade(NextLevel, out UpgradeSO nextUpgrade))
        {
            UpgradeSO currentUpgrade = null;
            if (NextLevel > 1)
            {
                MultiUpgrade.TryGetLevelUpgrade(NextLevel - 1, out currentUpgrade);
            }

            AppendStatDetails(currentUpgrade, nextUpgrade);
            return builder.ToString();
        }

        AppendStatDetails(null, Upgrade);
        return builder.ToString();
    }

    public int BindRelatedEvolutions(IReadOnlyList<UpgradeIconLevelUI> slots)
    {
        if (slots == null || slots.Count == 0)
        {
            return 0;
        }

        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i] != null)
            {
                slots[i].Clear();
            }
        }

        if (!IsMultiUpgrade
            || MultiUpgrade == null
            || unit == null
            || unit.HasSelectedEvolution)
        {
            return 0;
        }

        BuildRelatedEvolutions(MultiUpgrade);
        int visibleCount = 0;
        int slotIndex = 0;
        for (int i = 0; i < relatedEvolutions.Count && slotIndex < slots.Count; i++)
        {
            UpgradeIconLevelUI slot = slots[slotIndex];
            slotIndex++;
            if (slot == null)
            {
                continue;
            }

            slot.BindEvolution(relatedEvolutions[i].Evolution, unit);
            visibleCount++;
        }

        return visibleCount;
    }

    private void BuildRelatedEvolutions(MultiUpgradeSO sourceUpgrade)
    {
        relatedEvolutions.Clear();
        ResolveReferences();

        IReadOnlyList<EvolutionSO> evolutionPool = upgradesManager != null
            ? upgradesManager.EvolutionPool
            : null;

        if (evolutionPool == null)
        {
            return;
        }

        for (int i = 0; i < evolutionPool.Count; i++)
        {
            EvolutionSO evolution = evolutionPool[i];
            if (evolution == null || !evolution.HasResolvedUpgrade)
            {
                continue;
            }

            if (TryBuildRankedEvolution(evolution, sourceUpgrade, unit, i, out RankedEvolution rankedEvolution))
            {
                relatedEvolutions.Add(rankedEvolution);
            }
        }

        relatedEvolutions.Sort(CompareRankedEvolutions);
    }

    private void ResolveReferences()
    {
        if (upgradesManager == null)
        {
            ServiceLocator.TryResolve(out upgradesManager);
        }
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
        }

        if (!containsSource)
        {
            return false;
        }

        rankedEvolution = new RankedEvolution(evolution, missingLevels, metPrerequisiteCount, poolIndex);
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

    private void AppendStatDetails(UpgradeSO currentUpgrade, UpgradeSO nextUpgrade)
    {
        if (nextUpgrade == null)
        {
            return;
        }

        IReadOnlyList<UpgradeSO.StatEffect> nextEffects = nextUpgrade.StatEffects;
        for (int i = 0; i < nextEffects.Count; i++)
        {
            UpgradeSO.StatEffect nextEffect = nextEffects[i];
            if (builder.Length > 0)
            {
                builder.AppendLine();
            }

            builder.Append(GetStatLabel(nextEffect.stat));
            builder.Append(' ');

            if (TryFindComparableEffect(currentUpgrade, nextEffect, out UpgradeSO.StatEffect currentEffect))
            {
                builder.Append(FormatModifier(currentEffect));
                builder.Append(" >>> ");
            }

            builder.Append(FormatModifier(nextEffect));
        }
    }

    private static string FormatModifier(UpgradeSO.StatEffect effect)
    {
        switch (effect.type)
        {
            case STAT_TYPE.Add:
                return effect.value >= 0f ? $"+{effect.value:0.##}" : effect.value.ToString("0.##");
            case STAT_TYPE.Mult:
                float displayValue = effect.stat == ENTITY_STATS.AttackSpeed && !Mathf.Approximately(effect.value, 0f)
                    ? 1f / effect.value
                    : effect.value;
                return $"x{displayValue:0.##}";
            default:
                return effect.value.ToString("0.##");
        }
    }

    private static string GetStatLabel(ENTITY_STATS stat)
    {
        switch (stat)
        {
            case ENTITY_STATS.GlobalDamage:
                return "DMG";
            case ENTITY_STATS.AttackSpeed:
                return "ASP";
            case ENTITY_STATS.VisualRange:
                return "VIS";
            case ENTITY_STATS.AmmoUnits:
                return "AMO";
            case ENTITY_STATS.SetupTime:
                return "SET";
            case ENTITY_STATS.BulletSize:
                return "BUL";
            default:
                return stat.ToString();
        }
    }

    private static bool TryFindComparableEffect(
        UpgradeSO currentUpgrade,
        UpgradeSO.StatEffect nextEffect,
        out UpgradeSO.StatEffect currentEffect)
    {
        currentEffect = default;
        if (currentUpgrade == null)
        {
            return false;
        }

        IReadOnlyList<UpgradeSO.StatEffect> currentEffects = currentUpgrade.StatEffects;
        for (int i = 0; i < currentEffects.Count; i++)
        {
            UpgradeSO.StatEffect candidate = currentEffects[i];
            if (candidate.stat == nextEffect.stat && candidate.type == nextEffect.type)
            {
                currentEffect = candidate;
                return true;
            }
        }

        for (int i = 0; i < currentEffects.Count; i++)
        {
            UpgradeSO.StatEffect candidate = currentEffects[i];
            if (candidate.stat == nextEffect.stat)
            {
                currentEffect = candidate;
                return true;
            }
        }

        return false;
    }

    private readonly struct RankedEvolution
    {
        public EvolutionSO Evolution { get; }
        public int MissingLevels { get; }
        public int MetPrerequisiteCount { get; }
        public int PoolIndex { get; }

        public RankedEvolution(EvolutionSO evolution, int missingLevels, int metPrerequisiteCount, int poolIndex)
        {
            Evolution = evolution;
            MissingLevels = missingLevels;
            MetPrerequisiteCount = metPrerequisiteCount;
            PoolIndex = poolIndex;
        }
    }
}
