using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Authored upgrade line that resolves one active level into a normal <see cref="UpgradeSO"/>.
/// </summary>
[CreateAssetMenu(fileName = "MultiUpgrade", menuName = "TBTD/Multi Upgrade")]
public sealed class MultiUpgradeSO : ScriptableObject
{
    [SerializeField, Tooltip("Ordered upgrade levels. Level numbers are 1-based by list order.")]
    private List<UpgradeSO> levelUpgrades = new List<UpgradeSO>();

    public IReadOnlyList<UpgradeSO> LevelUpgrades => levelUpgrades;
    public int MaxLevel => levelUpgrades != null ? levelUpgrades.Count : 0;
    public bool HasValidLevels => TryGetLevelUpgrade(1, out _);

    public bool TryGetLevelUpgrade(int level, out UpgradeSO upgrade)
    {
        int index = level - 1;
        if (levelUpgrades != null && index >= 0 && index < levelUpgrades.Count)
        {
            upgrade = levelUpgrades[index];
            return upgrade != null;
        }

        upgrade = null;
        return false;
    }

    public bool TryGetNextLevelUpgrade(int currentLevel, out int nextLevel, out UpgradeSO upgrade)
    {
        nextLevel = Mathf.Max(0, currentLevel) + 1;
        return TryGetLevelUpgrade(nextLevel, out upgrade);
    }
}
