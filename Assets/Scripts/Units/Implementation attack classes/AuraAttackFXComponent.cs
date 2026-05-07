using UnityEngine;

public sealed class AuraAttackFXComponent : MonoBehaviour, AttackFXComponent
{
    [SerializeField, Tooltip("Particle system used to emit one aura hit particle per damaged target.")]
    private ParticleSystem auraParticleSystem;

    private void Awake()
    {
        CacheParticleSystem();
    }

    private void OnValidate()
    {
        CacheParticleSystem();
    }

    public void PlayAttackFX(AttackFXContext context)
    {
        if (auraParticleSystem == null || !context.HasHitPosition)
        {
            return;
        }

        ParticleSystem.EmitParams emitParams = new ParticleSystem.EmitParams
        {
            position = auraParticleSystem.transform.worldToLocalMatrix.MultiplyPoint3x4(context.HitPosition)
        };

        auraParticleSystem.Emit(emitParams, 1);
    }

    private void CacheParticleSystem()
    {
        if (auraParticleSystem == null)
        {
            auraParticleSystem = GetComponent<ParticleSystem>();
        }
    }
}
