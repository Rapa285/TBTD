using TMPro;
using UnityEngine;

/// <summary>
/// Coordinates focused upgrade details for the upgrade selection panel.
/// </summary>
public class UpgradeInfoDetailsUI : MonoBehaviour
{
    [SerializeField, Tooltip("Optional root toggled when details are bound. Defaults to this object.")]
    private GameObject root;

    [SerializeField, Tooltip("Evolution hint panel for focused multi-upgrade choices.")]
    private EvoHintUI evoHintUI;

    [SerializeField, Tooltip("Optional text display for the focused upgrade name.")]
    private TMP_Text upgradeNameText;

    [SerializeField, Tooltip("Optional stat details panel for the focused resolved upgrade.")]
    private UpgradeStatInfoUI statInfoUI;

    private void Awake()
    {
        ResolveReferences();
        Clear();
    }

    private void OnValidate()
    {
        ResolveReferences();
    }

    public void Bind(string unitId, UnitUpgradeOfferChoice choice)
    {
        ResolveReferences();

        if (!choice.IsValid)
        {
            Clear();
            return;
        }

        if (statInfoUI != null)
        {
            statInfoUI.Bind(choice);
        }

        if (upgradeNameText != null)
        {
            upgradeNameText.text = GetUpgradeDisplayName(choice.ResolvedUpgrade);
        }

        if (evoHintUI != null)
        {
            if (choice.IsMultiUpgrade)
            {
                evoHintUI.Bind(choice.MultiUpgrade, unitId);
            }
            else if (choice.IsEvolution)
            {
                evoHintUI.Bind(choice.Evolution, unitId);
            }
            else
            {
                evoHintUI.Clear();
            }
        }

        SetRootVisible(true);
    }

    public void Clear()
    {
        if (evoHintUI != null)
        {
            evoHintUI.Clear();
        }

        if (statInfoUI != null)
        {
            statInfoUI.Clear();
        }

        if (upgradeNameText != null)
        {
            upgradeNameText.text = string.Empty;
        }

        SetRootVisible(false);
    }

    private void ResolveReferences()
    {
        if (root == null)
        {
            root = gameObject;
        }

        if (evoHintUI == null)
        {
            evoHintUI = GetComponentInChildren<EvoHintUI>(true);
        }

        if (upgradeNameText == null)
        {
            upgradeNameText = GetComponentInChildren<TMP_Text>(true);
        }

        if (statInfoUI == null)
        {
            statInfoUI = GetComponentInChildren<UpgradeStatInfoUI>(true);
        }
    }

    private static string GetUpgradeDisplayName(UpgradeSO upgrade)
    {
        if (upgrade == null)
        {
            return string.Empty;
        }

        if (!string.IsNullOrWhiteSpace(upgrade.UpgradeName))
        {
            return upgrade.UpgradeName;
        }

        return upgrade.name;
    }

    private void SetRootVisible(bool isVisible)
    {
        GameObject target = root != null ? root : gameObject;
        if (target != null && target.activeSelf != isVisible)
        {
            target.SetActive(isVisible);
        }
    }
}
