using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Area pulse weapon that damages every currently tracked target in its tower vision range.
/// </summary>
public sealed class AuraBehaviour : AttackBehaviour
{
    [Header("Aura Charge SFX")]
    [SerializeField, Tooltip("Optional sound played once when aura charging begins.")]
    private AudioClip chargeStartSound;

    [SerializeField, Tooltip("Optional looping sound played while aura charging is active.")]
    private AudioClip chargeLoopSound;

    [SerializeField, Tooltip("Optional sound played once when aura charging stops.")]
    private AudioClip chargeStopSound;

    [SerializeField, Tooltip("AudioSource used for the sustained aura charge loop. One is added at runtime when missing.")]
    private AudioSource chargeLoopSource;

    [SerializeField, Tooltip("Randomizes pitch for aura charge start and stop sounds.")]
    private bool randomizeChargeOneShotPitch;

    private bool chargeAudioActive;

    public override bool RequiresCooldownWhenTargetsFirstAvailable => true;

    protected override bool ExecuteAttack(Transform target, float damage)
    {
        UnitVision vision = OwnerTower != null ? OwnerTower.Vision : null;
        if (vision == null)
        {
            if (target == null || !TryApplyDamage(target, damage))
            {
                return false;
            }

            PlayAuraHitFX(target, damage);
            StopChargeAudio(true);
            return true;
        }

        if (!vision.HasValidTargets)
        {
            return false;
        }

        List<Transform> targets = new List<Transform>(vision.ValidTargets);
        bool damagedAnyTarget = false;
        for (int i = 0; i < targets.Count; i++)
        {
            Transform candidate = targets[i];
            if (candidate == null || !vision.Contains(candidate))
            {
                continue;
            }

            if (TryApplyDamage(candidate, damage))
            {
                PlayAuraHitFX(candidate, damage);
                damagedAnyTarget = true;
            }
        }

        if (damagedAnyTarget)
        {
            StopChargeAudio(true);
        }

        return damagedAnyTarget;
    }

    protected override void OnTowerTargetsAvailable()
    {
        StartChargeAudio();
    }

    protected override void OnTowerTargetsUnavailable()
    {
        StopChargeAudio(true);
    }

    protected override void OnTowerAttackBehaviourDeactivated()
    {
        StopChargeAudio(false);
    }

    private void PlayAuraHitFX(Transform target, float damage)
    {
        AttackFX?.PlayAttackFX(new AttackFXContext(
            this,
            OwnerTower,
            OwnerRoot,
            target,
            damage,
            transform.position,
            true,
            GetAimPoint(target),
            true,
            null));
    }

    private void StartChargeAudio()
    {
        if (chargeAudioActive)
        {
            return;
        }

        chargeAudioActive = true;
        PlayTowerSound(chargeStartSound, randomizeChargeOneShotPitch);

        if (chargeLoopSound == null)
        {
            return;
        }

        chargeLoopSource = EnsureTowerSFXSource(chargeLoopSource);
        chargeLoopSource.clip = chargeLoopSound;
        chargeLoopSource.loop = true;
        chargeLoopSource.pitch = 1f;
        chargeLoopSource.Play();
    }

    private void StopChargeAudio(bool playStopSound)
    {
        if (!chargeAudioActive)
        {
            return;
        }

        chargeAudioActive = false;

        if (chargeLoopSource != null && chargeLoopSource.isPlaying)
        {
            chargeLoopSource.Stop();
        }

        if (playStopSound)
        {
            PlayTowerSound(chargeStopSound, randomizeChargeOneShotPitch);
        }
    }
}
