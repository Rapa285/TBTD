using UnityEngine;

/// <summary>
/// Hover source for UI slots that alternate between a real upgrade/evolution tooltip and default placeholder text.
/// </summary>
public class ConvertibleUpgradeHoverable : UpgradeHoverableItem
{
    [SerializeField, Tooltip("Tooltip title shown while this slot has no bound upgrade or evolution.")]
    private string defaultTitle = "No Evolution Selected";

    [SerializeField, TextArea, Tooltip("Tooltip description shown while this slot has no bound upgrade or evolution.")]
    private string defaultDescription = "This unit has not selected an evolution.";

    /// <summary>
    /// Switches the active tooltip content back to the configured placeholder text.
    /// </summary>
    public void BindDefault()
    {
        Clear();
        Bind(defaultTitle, defaultDescription);
    }

    public new void Bind(EvolutionSO evolution)
    {
        if (evolution == null)
        {
            BindDefault();
            return;
        }

        base.Bind(evolution);
    }

    public new void Bind(UpgradeSO upgrade)
    {
        if (upgrade == null)
        {
            BindDefault();
            return;
        }

        base.Bind(upgrade);
    }

    public new void Bind(UnitUpgradeOfferChoice choice)
    {
        if (!choice.IsValid)
        {
            BindDefault();
            return;
        }

        base.Bind(choice);
    }

    public new void Bind(MultiUpgradeSO multiUpgrade, int targetLevel, int currentLevel = 0)
    {
        if (multiUpgrade == null)
        {
            BindDefault();
            return;
        }

        base.Bind(multiUpgrade, targetLevel, currentLevel);
    }
}
