using UnityEngine;

/// <summary>
/// Requests pooled VFX when the projectile this component is attached to reports a bullet hit.
/// </summary>
public sealed class VFXRequester : MonoBehaviour
{
    [SerializeField, Tooltip("VFX type requested when the attached projectile reports a hit.")]
    private VFXType hitVFXType = VFXType.None;

    [SerializeField, Tooltip("Projectile that raises OnBulletHit. Defaults to this GameObject.")]
    private BaseProjectile projectile;

    private VFXService vfxService;
    private bool warnedMissingService;

    private void Awake()
    {
        ResolveProjectile();
        ResolveService();
    }

    private void OnEnable()
    {
        ResolveProjectile();
        if (projectile != null)
        {
            projectile.OnBulletHit += HandleBulletHit;
        }
    }

    private void OnDisable()
    {
        if (projectile != null)
        {
            projectile.OnBulletHit -= HandleBulletHit;
        }
    }

    private void HandleBulletHit(Transform hitTransform)
    {
        if (hitVFXType == VFXType.None || hitTransform == null)
        {
            return;
        }

        ResolveService();
        if (vfxService == null)
        {
            WarnMissingService();
            return;
        }

        vfxService.HandleRequest(hitVFXType, hitTransform);
    }

    private void ResolveProjectile()
    {
        if (projectile == null)
        {
            projectile = GetComponent<BaseProjectile>();
        }
    }

    private void ResolveService()
    {
        if (vfxService == null)
        {
            ServiceLocator.TryResolve(out vfxService);
        }
    }

    private void WarnMissingService()
    {
        if (warnedMissingService)
        {
            return;
        }

        warnedMissingService = true;
        Debug.LogWarning(
            $"{nameof(VFXRequester)} on '{name}' could not find a {nameof(VFXService)}. No hit VFX was played.",
            this);
    }
}
