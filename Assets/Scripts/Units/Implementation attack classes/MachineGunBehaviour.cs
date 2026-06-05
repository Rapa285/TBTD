using UnityEngine;

/// <summary>
/// Spline-leading projectile weapon that winds up from a cold start into its full attack cadence.
/// </summary>
public sealed class MachineGunBehaviour : SplineLeadingAttackBehaviour
{
    [SerializeField, Tooltip("Projectile type expected to resolve to a BaseStraightProjectile.")]
    private ProjectileType projectileType = ProjectileType.Bullet;

    [SerializeField, Tooltip("Optional muzzle transform used as the projectile spawn origin.")]
    private Transform firePoint;

    [SerializeField, Tooltip("Number of attack ticks skipped before firing at each wind-up stage. The last stage should normally be zero.")]
    private int[] skippedTicksByStage = { 8, 6, 4, 2, 1, 0 };

    [SerializeField, Min(0f), Tooltip("Seconds after the last attack tick before spin begins falling.")]
    private float spinHoldDuration = 2f;

    [SerializeField, Min(0.01f), Tooltip("Seconds it takes for current spin to linearly fall back to zero after the hold duration.")]
    private float spinResetDuration = 2f;

    [Header("Spin SFX")]
    [SerializeField, Tooltip("Optional sound played once when machine gun spin begins.")]
    private AudioClip spinStartSound;

    [SerializeField, Tooltip("Optional looping sound played while machine gun spin is active.")]
    private AudioClip spinLoopSound;

    [SerializeField, Tooltip("Optional sound played once when machine gun spin stops.")]
    private AudioClip spinStopSound;

    [SerializeField, Tooltip("AudioSource used for the sustained machine gun spin loop. One is added at runtime when missing.")]
    private AudioSource spinLoopSource;

    [SerializeField, Tooltip("Randomizes pitch for machine gun spin start and stop sounds.")]
    private bool randomizeSpinOneShotPitch;

    private float lastAttackTickTime = float.NegativeInfinity;
    private float decayStartTime;
    private float decayStartProgress;
    private int windupStage;
    private int ticksSkippedAtCurrentStage;
    private bool decayActive;
    private bool spinAudioActive;

    protected override void OnValidate()
    {
        base.OnValidate();
        spinHoldDuration = Mathf.Max(0f, spinHoldDuration);
        spinResetDuration = Mathf.Max(0.01f, spinResetDuration);

        if (skippedTicksByStage == null || skippedTicksByStage.Length == 0)
        {
            skippedTicksByStage = new[] { 8, 6, 4, 2, 1, 0 };
        }

        for (int i = 0; i < skippedTicksByStage.Length; i++)
        {
            skippedTicksByStage[i] = Mathf.Max(0, skippedTicksByStage[i]);
        }
    }

    protected override Vector3 GetAttackOrigin()
    {
        return firePoint != null ? firePoint.position : transform.position;
    }

    protected override bool ExecuteAttack(Transform target, float damage)
    {
        if (target == null)
        {
            return false;
        }

        UpdateSpinDecay();
        StartSpinAudio();

        lastAttackTickTime = Time.time;
        decayActive = false;
        int skippedTicksRequired = GetSkippedTicksRequired();
        if (ticksSkippedAtCurrentStage < skippedTicksRequired)
        {
            ticksSkippedAtCurrentStage++;
            return false;
        }

        ticksSkippedAtCurrentStage = 0;
        AdvanceWindupStage();

        return FireProjectile(target, damage);
    }

    private bool FireProjectile(Transform target, float damage)
    {
        Vector3 spawnPosition = firePoint != null ? firePoint.position : transform.position;
        Quaternion spawnRotation = firePoint != null ? firePoint.rotation : transform.rotation;
        if (!TryRequestProjectile(projectileType, spawnPosition, spawnRotation, out BaseStraightProjectile projectile))
        {
            return false;
        }

        projectile.Initialize(damage, transform, OwnerTower, this, ProjectileModifiers);
        projectile.SetDirection(GetLeadPosition(target));

        if (!projectile.ReadyToFire())
        {
            projectile.CancelProjectile();
            return false;
        }

        projectile.Fire();
        return true;
    }

    private void UpdateSpinDecay()
    {
        if (float.IsNegativeInfinity(lastAttackTickTime))
        {
            return;
        }

        float idleDuration = Time.time - lastAttackTickTime;
        if (idleDuration <= spinHoldDuration)
        {
            decayActive = false;
            return;
        }

        if (windupStage <= 0)
        {
            if (idleDuration >= spinHoldDuration + spinResetDuration)
            {
                ResetWindupState();
            }

            return;
        }

        if (!decayActive)
        {
            decayActive = true;
            decayStartTime = lastAttackTickTime + spinHoldDuration;
            decayStartProgress = GetWindupProgress();
        }

        float decayProgress = Mathf.Clamp01((Time.time - decayStartTime) / spinResetDuration);
        SetWindupProgress(Mathf.Lerp(decayStartProgress, 0f, decayProgress));
        ticksSkippedAtCurrentStage = 0;
        if (windupStage <= 0 && decayProgress >= 1f)
        {
            ResetWindupState();
        }
    }

    private int GetSkippedTicksRequired()
    {
        if (skippedTicksByStage == null || skippedTicksByStage.Length == 0)
        {
            return 0;
        }

        int stage = Mathf.Clamp(windupStage, 0, skippedTicksByStage.Length - 1);
        return Mathf.Max(0, skippedTicksByStage[stage]);
    }

    private void AdvanceWindupStage()
    {
        int maxStage = skippedTicksByStage != null && skippedTicksByStage.Length > 0
            ? skippedTicksByStage.Length - 1
            : 0;

        windupStage = Mathf.Min(windupStage + 1, maxStage);
    }

    private float GetWindupProgress()
    {
        int maxStage = skippedTicksByStage != null && skippedTicksByStage.Length > 1
            ? skippedTicksByStage.Length - 1
            : 1;

        return Mathf.Clamp01((float)windupStage / maxStage);
    }

    private void SetWindupProgress(float value)
    {
        int maxStage = skippedTicksByStage != null && skippedTicksByStage.Length > 1
            ? skippedTicksByStage.Length - 1
            : 1;

        windupStage = Mathf.Clamp(Mathf.RoundToInt(Mathf.Clamp01(value) * maxStage), 0, maxStage);
    }

    private void ResetWindupState()
    {
        windupStage = 0;
        ticksSkippedAtCurrentStage = 0;
        decayActive = false;
        StopSpinAudio(true);
    }

    protected override void OnTowerTargetsUnavailable()
    {
        StopSpinAudio(true);
    }

    protected override void OnTowerAttackBehaviourDeactivated()
    {
        StopSpinAudio(false);
    }

    private void StartSpinAudio()
    {
        if (spinAudioActive)
        {
            return;
        }

        spinAudioActive = true;
        PlayTowerSound(spinStartSound, randomizeSpinOneShotPitch);

        if (spinLoopSound == null)
        {
            return;
        }

        spinLoopSource = EnsureTowerSFXSource(spinLoopSource);
        spinLoopSource.clip = spinLoopSound;
        spinLoopSource.loop = true;
        spinLoopSource.pitch = 1f;
        spinLoopSource.Play();
    }

    private void StopSpinAudio(bool playStopSound)
    {
        if (!spinAudioActive)
        {
            return;
        }

        spinAudioActive = false;

        if (spinLoopSource != null && spinLoopSource.isPlaying)
        {
            spinLoopSource.Stop();
        }

        if (playStopSound)
        {
            PlayTowerSound(spinStopSound, randomizeSpinOneShotPitch);
        }
    }
}
