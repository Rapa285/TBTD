
public enum ENTITY_STATS
{
    GlobalDamage, // Global damage mult
    AttackSpeed, // Time to aim and between firings
    VisualRange,
    SetupTime, // Time taken from units deployment until actively attacking
    AmmoEffectiveness, // Multiplies attacksPerAmmo on the primary weapon
    AmmoUnits // Deployment-time ammo pool for finite primary weapons
}

public enum STAT_TYPE
{
    Mult = 0,
    Add = 1
}
