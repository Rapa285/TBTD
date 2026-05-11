using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

/// <summary>
/// Displays one offered upgrade and reports choice clicks to the owning selection UI.
/// </summary>
public class UpgradeChoiceItem : MonoBehaviour, IPointerEnterHandler, ISelectHandler
{
    [SerializeField, Tooltip("Clickable control used to select this upgrade.")]
    private Button button;

    [SerializeField, Tooltip("Optional image used to display the upgrade icon.")]
    private Image iconImage;

    [SerializeField, Tooltip("TMP text element used to display the upgrade name.")]
    private TMP_Text upgradeNameText;

    [SerializeField, Tooltip("TMP text element used to display the upgrade description.")]
    private TMP_Text descriptionText;

    [SerializeField, Tooltip("Optional hover data source populated from the bound upgrade choice.")]
    private UpgradeHoverableItem hoverableItem;

    private UpgradeSelectionUI owner;
    private UnitUpgradeOfferChoice choice;
    private int choiceIndex = -1;

    public UnitUpgradeOfferChoice Choice => choice;
    public MultiUpgradeSO MultiUpgrade => choice.MultiUpgrade;
    public EvolutionSO Evolution => choice.Evolution;
    public UpgradeSO Upgrade => choice.ResolvedUpgrade;
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
    public void Bind(UnitUpgradeOfferChoice choice, int choiceIndex, UpgradeSelectionUI owner)
    {
        this.choice = choice;
        this.choiceIndex = choiceIndex;
        this.owner = owner;

        RefreshDisplay();

        if (hoverableItem != null)
        {
            hoverableItem.Bind(choice);
        }

        if (button != null)
        {
            button.interactable = choice.IsValid && owner != null && choiceIndex >= 0;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        NotifyFocused();
    }

    public void OnSelect(BaseEventData eventData)
    {
        NotifyFocused();
    }

    private void HandleButtonClicked()
    {
        if (owner == null || choiceIndex < 0)
        {
            return;
        }

        owner.HandleChoiceSelected(choiceIndex);
    }

    private void NotifyFocused()
    {
        if (owner == null || choiceIndex < 0)
        {
            return;
        }

        owner.HandleChoiceFocused(choiceIndex);
    }

    private void RefreshDisplay()
    {
        UpgradeSO upgrade = Upgrade;
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
        UpgradeSO upgrade = Upgrade;
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

        if (hoverableItem == null)
        {
            hoverableItem = GetComponent<UpgradeHoverableItem>();
        }
    }
}
