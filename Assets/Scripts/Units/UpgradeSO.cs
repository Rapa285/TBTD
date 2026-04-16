using System;
using System.Collections.Generic;
using UnityEngine;

public enum WEAPON_UPGRADE_TYPE
{
    None,
    Override,
    Augment
}

[CreateAssetMenu(fileName = "Upgrade", menuName = "TBTD/Upgrade")]
public sealed class UpgradeSO : ScriptableObject
{
    [Serializable]
    public struct StatEffect
    {
        public ENTITY_STATS stat;
        public STAT_TYPE type;
        public float value;
    }

    [SerializeField] private string upgradeName;
    [SerializeField, TextArea] private string description;
    [SerializeField] private WEAPON_UPGRADE_TYPE weaponUpgradeType = WEAPON_UPGRADE_TYPE.None;
    [SerializeField] private AttackBehaviour weaponBehaviourPrefab;
    [SerializeField] private List<StatEffect> statEffects = new List<StatEffect>();

    public string UpgradeName => upgradeName;
    public string Description => description;
    public WEAPON_UPGRADE_TYPE WeaponUpgradeType => weaponUpgradeType;
    public AttackBehaviour WeaponBehaviourPrefab => weaponBehaviourPrefab;
    public IReadOnlyList<StatEffect> StatEffects => statEffects;

    public bool HasWeaponChange => weaponUpgradeType != WEAPON_UPGRADE_TYPE.None && weaponBehaviourPrefab != null;
}
