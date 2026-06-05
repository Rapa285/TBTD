/// <summary>
/// Stats compiled by <see cref="TowerEntity"/> from base values and applied upgrades.
/// </summary>
public enum ENTITY_STATS
{
    GlobalDamage, // Global damage mult
    AttackSpeed, // Cooldown seconds between attack ticks; UI may display its inverse as ASP frequency
    VisualRange,
    SetupTime, // Time taken from units deployment until actively attacking
    AmmoEffectiveness, // Multiplies attacksPerAmmo on the primary weapon
    AmmoUnits, // Deployment-time ammo pool for finite primary weapons
    DeploymentCooldown, // Time before a recalled unit can deploy again
    DeploymentCost, // Currency cost paid when roster-managed units deploy
    BulletSize // Multiplier applied to projectile hit size
}

/// <summary>
/// Stat modifier operation used by <see cref="UpgradeSO.StatEffect"/>.
/// </summary>
public enum STAT_TYPE
{
    Mult = 0,
    Add = 1
}
