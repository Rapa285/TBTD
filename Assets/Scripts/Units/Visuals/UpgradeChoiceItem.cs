using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Displays one offered upgrade and reports choice clicks to the owning selection UI.
/// </summary>
public class UpgradeChoiceItem : MonoBehaviour
{
    [SerializeField, Tooltip("Clickable control used to select this upgrade.")]
    private Button button;

    [SerializeField, Tooltip("Optional image used to display the upgrade icon.")]
    private Image iconImage;

    [SerializeField, Tooltip("TMP text element used to display the upgrade name.")]
    private TMP_Text upgradeNameText;

    [SerializeField, Tooltip("TMP text element used to display the upgrade description.")]
    private TMP_Text descriptionText;

    private UpgradeSelectionUI owner;
    private UpgradeSO upgrade;
    private int choiceIndex = -1;

    public UpgradeSO Upgrade => upgrade;
    public int ChoiceIndex => choiceIndex;

    private void Awake()
    {
        ResolveReferences();

        if (button != null)
        {
            button.onClick.AddListener(HandleButtonClicked);
        }
    }

    private void OnDestroy()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(HandleButtonClicked);
        }
    }

    private void OnValidate()
    {
        ResolveReferences();
    }

    /// <summary>
    /// Binds this item to one offered upgrade and its UI choice index.
    /// </summary>
    public void Bind(UpgradeSO upgrade, int choiceIndex, UpgradeSelectionUI owner)
    {
        this.upgrade = upgrade;
        this.choiceIndex = choiceIndex;
        this.owner = owner;

        RefreshDisplay();

        if (button != null)
        {
            button.interactable = upgrade != null && owner != null && choiceIndex >= 0;
        }
    }

    private void HandleButtonClicked()
    {
        if (owner == null || choiceIndex < 0)
        {
            return;
        }

        owner.HandleChoiceSelected(choiceIndex);
    }

    private void RefreshDisplay()
    {
        if (upgradeNameText != null)
        {
            upgradeNameText.text = GetDisplayName();
        }

        if (descriptionText != null)
        {
            descriptionText.text = upgrade != null ? upgrade.Description : string.Empty;
        }

        if (iconImage != null)
        {
            Sprite icon = upgrade != null ? upgrade.Icon : null;
            iconImage.sprite = icon;
            iconImage.enabled = icon != null;
        }
    }

    private string GetDisplayName()
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

    private void ResolveReferences()
    {
        if (button == null)
        {
            button = GetComponent<Button>();
        }
    }
}
