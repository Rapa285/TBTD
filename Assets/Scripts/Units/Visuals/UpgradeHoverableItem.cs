using UnityEngine;

/// <summary>
/// Hover source that resolves tooltip content from upgrade, multi-upgrade, or evolution data.
/// </summary>
public class UpgradeHoverableItem : GenericHoverableItem
{
    [SerializeField, Tooltip("Direct upgrade shown by this hover item.")]
    private UpgradeSO upgrade;

    [SerializeField, Tooltip("Optional multi-upgrade line this hover represents.")]
    private MultiUpgradeSO multiUpgrade;

    [SerializeField, Tooltip("Optional evolution this hover represents.")]
    private EvolutionSO evolution;

    [SerializeField, Min(0), Tooltip("Current selected multi-upgrade level, when this item represents an offer.")]
    private int currentLevel;

    [SerializeField, Min(0), Tooltip("Target or next multi-upgrade level represented by this item.")]
    private int nextLevel;

    [SerializeField, Min(0), Tooltip("Maximum level for the represented multi-upgrade line.")]
    private int maxLevel;

    public UpgradeSO Upgrade => ResolveUpgrade();
    public MultiUpgradeSO MultiUpgrade => multiUpgrade;
    public EvolutionSO Evolution => evolution;
    public int CurrentLevel => currentLevel;
    public int NextLevel => nextLevel;
    public int MaxLevel => maxLevel;
    public Sprite IconSprite => Upgrade != null ? Upgrade.Icon : null;
    public bool IsEvolution => evolution != null;
    public bool IsMultiUpgrade => multiUpgrade != null;

    public override string Title
    {
        get
        {
            if (multiUpgrade != null && !string.IsNullOrWhiteSpace(multiUpgrade.UpgradeName))
            {
                return multiUpgrade.UpgradeName;
            }

            UpgradeSO resolvedUpgrade = Upgrade;
            if (resolvedUpgrade == null)
            {
                return base.Title;
            }

            if (!string.IsNullOrWhiteSpace(resolvedUpgrade.UpgradeName))
            {
                return resolvedUpgrade.UpgradeName;
            }

            return resolvedUpgrade.name;
        }
    }

    public override string Description
    {
        get
        {
            if (multiUpgrade != null && !string.IsNullOrWhiteSpace(multiUpgrade.Description))
            {
                return multiUpgrade.Description;
            }

            UpgradeSO resolvedUpgrade = Upgrade;
            if (resolvedUpgrade == null)
            {
                return base.Description;
            }

            return resolvedUpgrade.Description;
        }
    }

    public void Bind(UnitUpgradeOfferChoice choice)
    {
        multiUpgrade = choice.MultiUpgrade;
        evolution = choice.Evolution;
        upgrade = choice.ResolvedUpgrade;
        currentLevel = choice.CurrentLevel;
        nextLevel = choice.NextLevel;
        maxLevel = choice.MaxLevel;
        RefreshTooltipIfHovered();
    }

    public void Bind(UpgradeSO upgrade)
    {
        this.upgrade = upgrade;
        multiUpgrade = null;
        evolution = null;
        currentLevel = 0;
        nextLevel = upgrade != null ? 1 : 0;
        maxLevel = upgrade != null ? 1 : 0;
        RefreshTooltipIfHovered();
    }

    public void Bind(MultiUpgradeSO multiUpgrade, int targetLevel, int currentLevel = 0)
    {
        this.multiUpgrade = multiUpgrade;
        evolution = null;
        this.currentLevel = Mathf.Max(0, currentLevel);
        nextLevel = Mathf.Max(0, targetLevel);
        maxLevel = multiUpgrade != null ? multiUpgrade.MaxLevel : 0;
        upgrade = multiUpgrade != null && multiUpgrade.TryGetLevelUpgrade(nextLevel, out UpgradeSO resolvedUpgrade)
            ? resolvedUpgrade
            : null;
        RefreshTooltipIfHovered();
    }

    public void Bind(EvolutionSO evolution)
    {
        this.evolution = evolution;
        multiUpgrade = null;
        upgrade = evolution != null ? evolution.ResolvedUpgrade : null;
        currentLevel = 0;
        nextLevel = upgrade != null ? 1 : 0;
        maxLevel = upgrade != null ? 1 : 0;
        RefreshTooltipIfHovered();
    }

    public void Clear()
    {
        upgrade = null;
        multiUpgrade = null;
        evolution = null;
        currentLevel = 0;
        nextLevel = 0;
        maxLevel = 0;
        Bind(string.Empty, string.Empty);
        RefreshTooltipIfHovered();
    }

    protected override void OnValidate()
    {
        base.OnValidate();
        currentLevel = Mathf.Max(0, currentLevel);
        nextLevel = Mathf.Max(0, nextLevel);
        maxLevel = Mathf.Max(0, maxLevel);
    }

    private UpgradeSO ResolveUpgrade()
    {
        if (upgrade != null)
        {
            return upgrade;
        }

        if (evolution != null)
        {
            return evolution.ResolvedUpgrade;
        }

        if (multiUpgrade != null && nextLevel > 0 && multiUpgrade.TryGetLevelUpgrade(nextLevel, out UpgradeSO resolvedUpgrade))
        {
            return resolvedUpgrade;
        }

        return null;
    }
}
