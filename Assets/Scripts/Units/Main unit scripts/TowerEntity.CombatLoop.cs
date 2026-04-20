using UnityEngine;

/// <summary>
/// Target validation, deployment timers, and attack tick logic for <see cref="TowerEntity"/>.
/// </summary>
public partial class TowerEntity
{
    private float GetAttackCooldown()
    {
        return Mathf.Max(0.01f, GetStat(ENTITY_STATS.AttackSpeed));
    }

    private void AttackWithActiveBehaviours(Transform target, float damageMultiplier)
    {
        AttackBehaviour primaryAttackBehaviour = GetActiveAttackBehaviour();
        if (primaryAttackBehaviour == null)
        {
            return;
        }

        primaryAttackBehaviour.Attack(target, damageMultiplier);

        for (int i = 0; i < runtimeAugmentAttackBehaviours.Count; i++)
        {
            if (!IsAttackTargetStillValid(target))
            {
                break;
            }

            AttackBehaviour augmentAttackBehaviour = runtimeAugmentAttackBehaviours[i];
            if (augmentAttackBehaviour != null)
            {
                augmentAttackBehaviour.Attack(target, damageMultiplier);
            }
        }
    }

    private bool IsAttackTargetStillValid(Transform target)
    {
        return target != null
            && target.gameObject.activeInHierarchy
            && (vision == null || vision.Contains(target));
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

        nextEnemyPollTime = Time.time + GetEnemyPollPeriod();
    }

    private void PollEnemiesForDebugIfNeeded()
    {
        if (!activelyPollEnemies || Time.time < nextEnemyPollTime)
        {
            return;
        }

        // Debug polling supports spawned/test enemies that may not enter vision through trigger events.
        vision.ScanForTargetsOnce();
        nextEnemyPollTime = Time.time + GetEnemyPollPeriod();
    }

    private float GetEnemyPollPeriod()
    {
        return Mathf.Max(0.01f, enemyPollPeriod);
    }
}
