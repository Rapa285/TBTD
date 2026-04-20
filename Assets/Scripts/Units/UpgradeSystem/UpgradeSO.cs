using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Declares how an upgrade intends to affect the tower weapon.
/// </summary>
public enum WEAPON_UPGRADE_TYPE
{
    None,
    Override,
    Augment
}

/// <summary>
/// Authored upgrade asset that can combine stat effects, weapon replacement, and on-hit effects.
/// </summary>
[CreateAssetMenu(fileName = "Upgrade", menuName = "TBTD/Upgrade")]
public sealed class UpgradeSO : ScriptableObject
{
    /// <summary>
    /// One additive or multiplicative modifier applied to a tower stat.
    /// </summary>
    [Serializable]
    public struct StatEffect
    {
        [Tooltip("Tower stat affected by this upgrade effect.")]
        public ENTITY_STATS stat;

        [Tooltip("Whether this effect is added to or multiplied into the stat.")]
        public STAT_TYPE type;

        [Tooltip("Modifier value used by the selected stat operation.")]
        public float value;
    }

    [SerializeField, Tooltip("Display name shown for this upgrade.")]
    private string upgradeName;

    [SerializeField, TextArea, Tooltip("Player-facing description of what this upgrade does.")]
    private string description;

    [SerializeField, Tooltip("Optional icon shown when this upgrade is offered in UI.")]
    private Sprite icon;

    [SerializeField, Tooltip("Weapon change mode. Only Override is currently applied at runtime.")]
    private WEAPON_UPGRADE_TYPE weaponUpgradeType = WEAPON_UPGRADE_TYPE.None;

    [SerializeField, Tooltip("AttackBehaviour prefab used when this upgrade replaces the active weapon.")]
    private AttackBehaviour weaponBehaviourPrefab;

    [SerializeField, Tooltip("On-hit effect prefabs added while this upgrade is applied.")]
    private List<OnHitEffectBehaviour> onHitEffectPrefabs = new List<OnHitEffectBehaviour>();

    [SerializeField, Tooltip("Stat modifiers applied while this upgrade is selected.")]
    private List<StatEffect> statEffects = new List<StatEffect>();

    public string UpgradeName => upgradeName;
    public string Description => description;
    public Sprite Icon => icon;
    public WEAPON_UPGRADE_TYPE WeaponUpgradeType => weaponUpgradeType;
    public AttackBehaviour WeaponBehaviourPrefab => weaponBehaviourPrefab;
    public IReadOnlyList<OnHitEffectBehaviour> OnHitEffectPrefabs => onHitEffectPrefabs;
    public IReadOnlyList<StatEffect> StatEffects => statEffects;

    public bool HasWeaponChange => weaponUpgradeType != WEAPON_UPGRADE_TYPE.None && weaponBehaviourPrefab != null;
    public bool HasWeaponReplacement => weaponUpgradeType == WEAPON_UPGRADE_TYPE.Override && weaponBehaviourPrefab != null;
}
