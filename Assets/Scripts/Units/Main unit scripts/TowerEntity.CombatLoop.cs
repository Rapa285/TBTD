using UnityEngine;

/// <summary>
/// Target validation, deployment timers, and attack tick logic for <see cref="TowerEntity"/>.
/// </summary>
public partial class TowerEntity
{
    public bool IsInSetupTime => deployed && deploymentTimersInitialized && SetupTimeRemaining > 0f;
    public float SetupTimeDuration => Mathf.Max(0f, GetStat(ENTITY_STATS.SetupTime));
    public float SetupTimeRemaining => deployed && deploymentTimersInitialized
        ? Mathf.Max(0f, activeAfterTime - Time.time)
        : 0f;
    public float SetupTimeNormalizedRemaining => SetupTimeDuration > 0f
        ? Mathf.Clamp01(SetupTimeRemaining / SetupTimeDuration)
        : 0f;

    private float GetAttackCooldown()
    {
        return Mathf.Max(0.01f, GetStat(ENTITY_STATS.AttackSpeed));
    }

    private float GetTargetRefreshPeriod()
    {
        return Mathf.Max(0.01f, targetRefreshPeriod);
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

    private void ClearDeploymentRuntimeState()
    {
        deploymentTimersInitialized = false;
        currentTarget = null;
        activeAfterTime = float.PositiveInfinity;
        nextAttackTime = float.PositiveInfinity;
        nextEnemyPollTime = float.PositiveInfinity;
        nextTargetRefreshTime = float.PositiveInfinity;
        targetSelectionDirty = true;
        hadValidTargets = false;

        if (vision != null)
        {
            vision.ClearTargets();
        }
    }

    private void InitializeDeploymentRuntime()
    {
        currentTarget = null;
        activeAfterTime = Time.time + GetStat(ENTITY_STATS.SetupTime);
        nextAttackTime = activeAfterTime;
        nextTargetRefreshTime = activeAfterTime;
        targetSelectionDirty = true;
        hadValidTargets = false;
        deploymentTimersInitialized = true;

        if (vision != null)
        {
            vision.Range = GetStat(ENTITY_STATS.VisualRange);
            vision.ClearTargets();
            vision.ScanForTargetsOnce();
        }

        SynchronizeTargetPresenceState();
        nextEnemyPollTime = Time.time + GetEnemyPollPeriod();
    }

    private void RefreshDeploymentRuntime()
    {
        if (!deploymentTimersInitialized)
        {
            InitializeDeploymentRuntime();
            return;
        }

        if (vision != null)
        {
            vision.Range = GetStat(ENTITY_STATS.VisualRange);
            vision.ScanForTargetsOnce();
        }

        if (currentTarget != null && !IsAttackTargetStillValid(currentTarget))
        {
            currentTarget = null;
        }

        targetSelectionDirty = true;
        nextTargetRefreshTime = Time.time;
        SynchronizeTargetPresenceState();
        nextEnemyPollTime = Time.time + GetEnemyPollPeriod();
    }

    private void UpdateTargetSelectionIfNeeded()
    {
        if (vision == null)
        {
            currentTarget = null;
            hadValidTargets = false;
            return;
        }

        bool hasValidTargets = vision.HasValidTargets;
        if (hasValidTargets && !hadValidTargets)
        {
            ApplyFirstTargetCooldownIfNeeded();
        }

        hadValidTargets = hasValidTargets;

        if (!hasValidTargets)
        {
            currentTarget = null;
            targetSelectionDirty = true;
            nextTargetRefreshTime = float.PositiveInfinity;
            return;
        }

        if (currentTarget != null && !vision.Contains(currentTarget))
        {
            currentTarget = null;
        }

        if (!targetSelectionDirty && currentTarget != null && Time.time < nextTargetRefreshTime)
        {
            return;
        }

        currentTarget = vision.GetFrontMostValidTarget();
        targetSelectionDirty = false;
        nextTargetRefreshTime = Time.time + GetTargetRefreshPeriod();
    }

    private void SynchronizeTargetPresenceState()
    {
        if (vision == null)
        {
            hadValidTargets = false;
            return;
        }

        bool hasValidTargets = vision.HasValidTargets;
        if (hasValidTargets && !hadValidTargets)
        {
            ApplyFirstTargetCooldownIfNeeded();
        }

        hadValidTargets = hasValidTargets;
    }

    private void ApplyFirstTargetCooldownIfNeeded()
    {
        AttackBehaviour activeBehaviour = GetActiveAttackBehaviour();
        if (activeBehaviour == null || !activeBehaviour.RequiresCooldownWhenTargetsFirstAvailable)
        {
            return;
        }

        float earliestStartTime = Mathf.Max(Time.time, activeAfterTime);
        nextAttackTime = Mathf.Max(nextAttackTime, earliestStartTime + GetAttackCooldown());
    }

    private void SubscribeToVisionEventsIfNeeded()
    {
        if (subscribedVision == vision)
        {
            return;
        }

        UnsubscribeFromVisionEvents();
        subscribedVision = vision;

        if (subscribedVision == null)
        {
            return;
        }

        subscribedVision.TargetAdded += HandleVisionTargetAdded;
        subscribedVision.TargetRemoved += HandleVisionTargetRemoved;
        subscribedVision.TargetsChanged += HandleVisionTargetsChanged;
    }

    private void UnsubscribeFromVisionEvents()
    {
        if (subscribedVision == null)
        {
            return;
        }

        subscribedVision.TargetAdded -= HandleVisionTargetAdded;
        subscribedVision.TargetRemoved -= HandleVisionTargetRemoved;
        subscribedVision.TargetsChanged -= HandleVisionTargetsChanged;
        subscribedVision = null;
    }

    private void HandleVisionTargetAdded(Transform target)
    {
        HandleVisionTargetsChanged();
    }

    private void HandleVisionTargetRemoved(Transform target)
    {
        HandleVisionTargetsChanged();
    }

    private void HandleVisionTargetsChanged()
    {
        targetSelectionDirty = true;
        nextTargetRefreshTime = Time.time;
        SynchronizeTargetPresenceState();
    }

    private void PollEnemiesForDebugIfNeeded()
    {
        if (!activelyPollEnemies || Time.time < nextEnemyPollTime)
        {
            return;
        }

        // Debug polling supports spawned/test enemies that may not enter vision through trigger events.
        vision.ScanForTargetsOnce();
        targetSelectionDirty = true;
        nextTargetRefreshTime = Time.time;
        SynchronizeTargetPresenceState();
        nextEnemyPollTime = Time.time + GetEnemyPollPeriod();
    }

    private float GetEnemyPollPeriod()
    {
        return Mathf.Max(0.01f, enemyPollPeriod);
    }
}
