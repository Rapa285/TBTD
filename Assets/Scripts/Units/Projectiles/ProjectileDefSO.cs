using UnityEngine;

public enum ProjectileType
{
    None,
    Bullet,
    Grenade,
    LaserBeam
}

/// <summary>
/// Authored projectile definition used by scene-level pooled projectile spawning.
/// </summary>
[CreateAssetMenu(fileName = "ProjectileDef", menuName = "TBTD/Projectile Definition")]
public sealed class ProjectileDefSO : ScriptableObject
{
    [SerializeField, Tooltip("Gameplay category for this projectile definition.")]
    private ProjectileType projectileType = ProjectileType.None;

    [SerializeField, Tooltip("Projectile prefab spawned by ProjectilePoolService when this type is requested.")]
    private BaseProjectile projectilePrefab;

    public ProjectileType ProjectileType => projectileType;
    public BaseProjectile ProjectilePrefab => projectilePrefab;
}
