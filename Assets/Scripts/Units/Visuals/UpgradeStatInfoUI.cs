using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

/// <summary>
/// Displays stat effects from one resolved upgrade.
/// </summary>
public class UpgradeStatInfoUI : MonoBehaviour
{
    [SerializeField, Tooltip("Text display that receives formatted stat effect lines.")]
    private TMP_Text textDisplay;

    private readonly StringBuilder builder = new StringBuilder();

    private void Awake()
    {
        ResolveReferences();
    }

    private void OnValidate()
    {
        ResolveReferences();
    }

    public void Bind(UnitUpgradeOfferChoice choice)
    {
        if (choice.IsMultiUpgrade
            && choice.MultiUpgrade != null
            && choice.CurrentLevel > 0
            && choice.MultiUpgrade.TryGetLevelUpgrade(choice.CurrentLevel, out UpgradeSO currentUpgrade))
        {
            BindComparison(currentUpgrade, choice.ResolvedUpgrade);
            return;
        }

        Bind(choice.ResolvedUpgrade);
    }

    public void Bind(MultiUpgradeSO multiUpgrade, int targetLevel)
    {
        if (multiUpgrade != null && multiUpgrade.TryGetLevelUpgrade(targetLevel, out UpgradeSO upgrade))
        {
            if (targetLevel > 1 && multiUpgrade.TryGetLevelUpgrade(targetLevel - 1, out UpgradeSO previousUpgrade))
            {
                BindComparison(previousUpgrade, upgrade);
                return;
            }

            Bind(upgrade);
            return;
        }

        Clear();
    }

    public void Bind(UpgradeSO upgrade)
    {
        ResolveReferences();

        if (textDisplay == null)
        {
            return;
        }

        builder.Clear();
        if (upgrade != null)
        {
            IReadOnlyList<UpgradeSO.StatEffect> statEffects = upgrade.StatEffects;
            for (int i = 0; i < statEffects.Count; i++)
            {
                UpgradeSO.StatEffect effect = statEffects[i];
                if (builder.Length > 0)
                {
                    builder.AppendLine();
                }

                builder.Append(effect.stat);
                builder.Append(' ');
                builder.Append(FormatModifier(effect));
            }
        }

        textDisplay.text = builder.ToString();
    }

    public void BindComparison(UpgradeSO currentUpgrade, UpgradeSO nextUpgrade)
    {
        ResolveReferences();

        if (textDisplay == null)
        {
            return;
        }

        builder.Clear();
        if (nextUpgrade != null)
        {
            IReadOnlyList<UpgradeSO.StatEffect> nextEffects = nextUpgrade.StatEffects;
            for (int i = 0; i < nextEffects.Count; i++)
            {
                UpgradeSO.StatEffect nextEffect = nextEffects[i];
                if (builder.Length > 0)
                {
                    builder.AppendLine();
                }

                builder.Append(nextEffect.stat);
                builder.Append(' ');

                if (TryFindComparableEffect(currentUpgrade, nextEffect, out UpgradeSO.StatEffect currentEffect))
                {
                    builder.Append(FormatModifier(currentEffect));
                    builder.Append(" >>> ");
                }

                builder.Append(FormatModifier(nextEffect));
            }
        }

        textDisplay.text = builder.ToString();
    }

    public void Clear()
    {
        if (textDisplay != null)
        {
            textDisplay.text = string.Empty;
        }
    }

    private static string FormatModifier(UpgradeSO.StatEffect effect)
    {
        switch (effect.type)
        {
            case STAT_TYPE.Add:
                return effect.value >= 0f ? $"+{effect.value:0.##}" : effect.value.ToString("0.##");
            case STAT_TYPE.Mult:
                return $"x{effect.value:0.##}";
            default:
                return effect.value.ToString("0.##");
        }
    }

    private static bool TryFindComparableEffect(
        UpgradeSO currentUpgrade,
        UpgradeSO.StatEffect nextEffect,
        out UpgradeSO.StatEffect currentEffect)
    {
        currentEffect = default;
        if (currentUpgrade == null)
        {
            return false;
        }

        IReadOnlyList<UpgradeSO.StatEffect> currentEffects = currentUpgrade.StatEffects;
        for (int i = 0; i < currentEffects.Count; i++)
        {
            UpgradeSO.StatEffect candidate = currentEffects[i];
            if (candidate.stat == nextEffect.stat && candidate.type == nextEffect.type)
            {
                currentEffect = candidate;
                return true;
            }
        }

        for (int i = 0; i < currentEffects.Count; i++)
        {
            UpgradeSO.StatEffect candidate = currentEffects[i];
            if (candidate.stat == nextEffect.stat)
            {
                currentEffect = candidate;
                return true;
            }
        }

        return false;
    }

    private void ResolveReferences()
    {
        if (textDisplay == null)
        {
            textDisplay = GetComponentInChildren<TMP_Text>(true);
        }
    }
}
