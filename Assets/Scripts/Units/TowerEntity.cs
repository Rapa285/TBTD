using System;
using System.Collections.Generic;
using UnityEngine;

public class TowerEntity : MonoBehaviour
{
    [Serializable]
    private struct EntityStat
    {
        public ENTITY_STATS stat;
        public float value;
    }

    [SerializeField] private UnitVision vision;
    [SerializeField] private AttackBehaviour attackBehaviour;
    [SerializeField] private bool deployed = true;
    [SerializeField] private List<EntityStat> baseStats = new List<EntityStat>
    {
        new EntityStat { stat = ENTITY_STATS.GlobalDamage, value = 1f },
        new EntityStat { stat = ENTITY_STATS.AttackSpeed, value = 1f },
        new EntityStat { stat = ENTITY_STATS.VisualRange, value = 5f },
        new EntityStat { stat = ENTITY_STATS.SetupTime, value = 0f }
    };
    [SerializeField] private List<UpgradeSO> upgrades = new List<UpgradeSO>();

    private readonly Dictionary<ENTITY_STATS, float> finalStats = new Dictionary<ENTITY_STATS, float>();
    private Transform currentTarget;
    private float nextAttackTime;
    private float activeAfterTime;
    private bool deploymentTimersInitialized;

    public bool Deployed => deployed;

    private void Awake()
    {
        if (vision == null)
        {
            vision = GetComponentInChildren<UnitVision>();
        }

        if (attackBehaviour == null)
        {
            attackBehaviour = GetComponent<AttackBehaviour>();
        }

        CompileFinalStats();
    }

    public void PrepareForDeploymentPreview()
    {
        deployed = false;
        deploymentTimersInitialized = false;
        currentTarget = null;
        activeAfterTime = float.PositiveInfinity;
        nextAttackTime = float.PositiveInfinity;

        if (vision != null)
        {
            vision.ClearTargets();
        }
    }

    public void Deploy()
    {
        deployed = true;
        currentTarget = null;

        if (vision != null)
        {
            vision.ClearTargets();
            vision.Range = GetStat(ENTITY_STATS.VisualRange);
        }

        InitializeDeploymentTimers();
    }

    private void Start()
    {
        if (deployed && !deploymentTimersInitialized)
        {
            InitializeDeploymentTimers();
        }
    }

    private void OnValidate()
    {
        CompileFinalStats();
    }

    private void Update()
    {
        if (!deployed || attackBehaviour == null || vision == null || Time.time < activeAfterTime)
        {
            return;
        }

        if (currentTarget == null || !vision.Contains(currentTarget))
        {
            currentTarget = vision.GetFirstValidTarget();
        }

        if (currentTarget == null || Time.time < nextAttackTime)
        {
            return;
        }

        attackBehaviour.Attack(currentTarget, GetStat(ENTITY_STATS.GlobalDamage));
        nextAttackTime = Time.time + GetAttackCooldown();
    }

    public float GetStat(ENTITY_STATS stat)
    {
        if (finalStats.Count == 0)
        {
            CompileFinalStats();
        }

        return finalStats.TryGetValue(stat, out float value) ? value : GetDefaultStat(stat);
    }

    public void SetStat(ENTITY_STATS stat, float value)
    {
        for (int i = 0; i < baseStats.Count; i++)
        {
            if (baseStats[i].stat != stat)
            {
                continue;
            }

            EntityStat entityStat = baseStats[i];
            entityStat.value = value;
            baseStats[i] = entityStat;

            CompileFinalStats();
            return;
        }

        baseStats.Add(new EntityStat { stat = stat, value = value });
        CompileFinalStats();
    }

    public void AddUpgrade(UpgradeSO upgrade)
    {
        if (upgrade == null || upgrades.Contains(upgrade))
        {
            return;
        }

        upgrades.Add(upgrade);
        CompileFinalStats();
    }

    public void RemoveUpgrade(UpgradeSO upgrade)
    {
        if (!upgrades.Remove(upgrade))
        {
            return;
        }

        CompileFinalStats();
    }

    public void CompileFinalStats()
    {
        finalStats.Clear();

        foreach (ENTITY_STATS stat in Enum.GetValues(typeof(ENTITY_STATS)))
        {
            finalStats[stat] = GetDefaultStat(stat);
        }

        for (int i = 0; i < baseStats.Count; i++)
        {
            finalStats[baseStats[i].stat] = baseStats[i].value;
        }

        Dictionary<ENTITY_STATS, float> addValues = new Dictionary<ENTITY_STATS, float>();
        Dictionary<ENTITY_STATS, float> multValues = new Dictionary<ENTITY_STATS, float>();

        foreach (ENTITY_STATS stat in Enum.GetValues(typeof(ENTITY_STATS)))
        {
            addValues[stat] = 0f;
            multValues[stat] = 1f;
        }

        for (int upgradeIndex = 0; upgradeIndex < upgrades.Count; upgradeIndex++)
        {
            UpgradeSO upgrade = upgrades[upgradeIndex];
            if (upgrade == null)
            {
                continue;
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
    }

    private float GetAttackCooldown()
    {
        return Mathf.Max(0.01f, GetStat(ENTITY_STATS.AttackSpeed));
    }

    private void InitializeDeploymentTimers()
    {
        activeAfterTime = Time.time + GetStat(ENTITY_STATS.SetupTime);
        nextAttackTime = activeAfterTime;
        deploymentTimersInitialized = true;

        if (vision != null)
        {
            vision.Range = GetStat(ENTITY_STATS.VisualRange);
            vision.ScanForTargetsOnce();
        }
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
            default:
                return 0f;
        }
    }
}
