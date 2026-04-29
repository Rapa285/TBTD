using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Stat and upgrade compilation for <see cref="TowerEntity"/>.
/// </summary>
public partial class TowerEntity
{
    /// <summary>
    /// Calculates one final stat from this tower's authored stats and the supplied upgrades without mutating runtime state.
    /// </summary>
    public float CalculateFinalStat(ENTITY_STATS stat, IReadOnlyList<UpgradeSO> additionalUpgrades)
    {
        float baseValue = GetBaseStat(stat);
        float addValue = 0f;
        float multValue = 1f;
        HashSet<UpgradeSO> processedUpgrades = new HashSet<UpgradeSO>();

        AccumulateStatEffects(upgrades, stat, ref addValue, ref multValue, processedUpgrades);
        AccumulateStatEffects(additionalUpgrades, stat, ref addValue, ref multValue, processedUpgrades);

        return (baseValue + addValue) * multValue;
    }

    /// <summary>
    /// Compiles base stats plus applied upgrades into final runtime stats and combat composition.
    /// </summary>
    public void CompileFinalStats()
    {
        finalStats.Clear();
        compiledProjectileModifierPrefabs.Clear();
        compiledAugmentWeaponPrefabs.Clear();

        foreach (ENTITY_STATS stat in Enum.GetValues(typeof(ENTITY_STATS)))
        {
            finalStats[stat] = GetDefaultStat(stat);
        }

        for (int i = 0; i < baseStats.Count; i++)
        {
            finalStats[baseStats[i].stat] = baseStats[i].value;
        }

        // Stat effects are accumulated separately so every final stat uses (base + add) * mult.
        Dictionary<ENTITY_STATS, float> addValues = new Dictionary<ENTITY_STATS, float>();
        Dictionary<ENTITY_STATS, float> multValues = new Dictionary<ENTITY_STATS, float>();

        foreach (ENTITY_STATS stat in Enum.GetValues(typeof(ENTITY_STATS)))
        {
            addValues[stat] = 0f;
            multValues[stat] = 1f;
        }

        // Weapon replacement is latest-wins, while augments and modifiers are additive.
        AttackBehaviour latestReplacementPrefab = null;

        for (int upgradeIndex = 0; upgradeIndex < upgrades.Count; upgradeIndex++)
        {
            UpgradeSO upgrade = upgrades[upgradeIndex];
            if (upgrade == null)
            {
                continue;
            }

            if (upgrade.HasWeaponReplacement)
            {
                latestReplacementPrefab = upgrade.WeaponBehaviourPrefab;
            }
            else if (upgrade.HasWeaponAugment)
            {
                compiledAugmentWeaponPrefabs.Add(upgrade.WeaponBehaviourPrefab);
            }

            IReadOnlyList<ProjectileModifierBehaviour> projectileModifierPrefabs = upgrade.ProjectileModifierPrefabs;
            for (int modifierIndex = 0; modifierIndex < projectileModifierPrefabs.Count; modifierIndex++)
            {
                ProjectileModifierBehaviour modifierPrefab = projectileModifierPrefabs[modifierIndex];
                if (modifierPrefab != null)
                {
                    compiledProjectileModifierPrefabs.Add(modifierPrefab);
                }
            }

            IReadOnlyList<UpgradeSO.StatEffect> statEffects = upgrade.StatEffects;
            for (int effectIndex = 0; effectIndex < statEffects.Count; effectIndex++)
            {
                UpgradeSO.StatEffect effect = statEffects[effectIndex];
                switch (effect.type)
                {
                    case STAT_TYPE.Add:
                        addValues[effect.stat] += effect.value;
                        break;
                    case STAT_TYPE.Mult:
                        multValues[effect.stat] *= effect.value;
                        break;
                }
            }
        }

        foreach (ENTITY_STATS stat in Enum.GetValues(typeof(ENTITY_STATS)))
        {
            finalStats[stat] = (finalStats[stat] + addValues[stat]) * multValues[stat];
        }

        if (vision != null)
        {
            vision.Range = GetStat(ENTITY_STATS.VisualRange);
        }

        ApplyRuntimeComposition(
            latestReplacementPrefab,
            compiledAugmentWeaponPrefabs,
            compiledProjectileModifierPrefabs);
    }

    private float GetDefaultStat(ENTITY_STATS stat)
    {
        switch (stat)
        {
            case ENTITY_STATS.GlobalDamage:
                return 1f;
            case ENTITY_STATS.AttackSpeed:
                return 1f;
            case ENTITY_STATS.VisualRange:
                return 5f;
            case ENTITY_STATS.SetupTime:
                return 0f;
            case ENTITY_STATS.AmmoEffectiveness:
                return 1f;
            case ENTITY_STATS.AmmoUnits:
                return 10f;
            case ENTITY_STATS.DeploymentCooldown:
                return 10f;
            case ENTITY_STATS.DeploymentCost:
                return 100f;
            default:
                return 0f;
        }
    }

    private float GetBaseStat(ENTITY_STATS stat)
    {
        if (baseStats == null)
        {
            return GetDefaultStat(stat);
        }

        for (int i = 0; i < baseStats.Count; i++)
        {
            if (baseStats[i].stat == stat)
            {
                return baseStats[i].value;
            }
        }

        return GetDefaultStat(stat);
    }

    private static void AccumulateStatEffects(
        IReadOnlyList<UpgradeSO> upgradeList,
        ENTITY_STATS stat,
        ref float addValue,
        ref float multValue,
        HashSet<UpgradeSO> processedUpgrades)
    {
        if (upgradeList == null)
        {
            return;
        }

        for (int upgradeIndex = 0; upgradeIndex < upgradeList.Count; upgradeIndex++)
        {
            UpgradeSO upgrade = upgradeList[upgradeIndex];
            if (upgrade == null || !processedUpgrades.Add(upgrade))
            {
                continue;
            }

            IReadOnlyList<UpgradeSO.StatEffect> statEffects = upgrade.StatEffects;
            for (int effectIndex = 0; effectIndex < statEffects.Count; effectIndex++)
            {
                UpgradeSO.StatEffect effect = statEffects[effectIndex];
                if (effect.stat != stat)
                {
                    continue;
                }

                switch (effect.type)
                {
                    case STAT_TYPE.Add:
                        addValue += effect.value;
                        break;
                    case STAT_TYPE.Mult:
                        multValue *= effect.value;
                        break;
                }
            }
        }
    }
}
