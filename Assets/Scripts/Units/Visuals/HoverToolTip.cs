using TMPro;
using UnityEngine;

/// <summary>
/// Hand-placed canvas tooltip presenter controlled by <see cref="HoverController"/>.
/// </summary>
public class HoverToolTip : MonoBehaviour
{
    [SerializeField, Tooltip("Optional root toggled when the tooltip is visible. Defaults to this object.")]
    private GameObject root;

    [SerializeField, Tooltip("RectTransform moved by the hover controller. Defaults to this object's RectTransform.")]
    private RectTransform rectTransform;

    [SerializeField, Tooltip("TMP text used to display the hovered title.")]
    private TMP_Text titleText;

    [SerializeField, Tooltip("TMP text used to display the hovered description.")]
    private TMP_Text descriptionText;

    [SerializeField, Tooltip("Optional icon display shown only for upgrade hover items.")]
    private GenericIconDisplay iconDisplay;

    [SerializeField, Tooltip("Optional stat display shown for detail upgrade hover items.")]
    private UpgradeStatInfoUI statInfoUI;

    [SerializeField, Tooltip("Optional root for related evolution slots, shown only for detail upgrade hover items with related evolutions.")]
    private GameObject validEvolutionsRoot;

    [SerializeField, Tooltip("Evolution icon slots shown for detail upgrade hover items.")]
    private UpgradeIconLevelUI[] validEvolutionSlots;

    private HoverController controller;

    public RectTransform RectTransform => rectTransform;

    private void Awake()
    {
        ResolveReferences();
        ResolveController();
        Hide();
    }

    private void OnEnable()
    {
        ResolveReferences();
        ResolveController();
    }

    private void OnDestroy()
    {
        if (controller != null)
        {
            controller.UnregisterToolTip(this);
        }
    }

    private void OnValidate()
    {
        ResolveReferences();
    }

    public void SetController(HoverController controller)
    {
        this.controller = controller;
    }

    public void Bind(GenericHoverableItem item)
    {
        ResolveReferences();

        if (titleText != null)
        {
            titleText.text = item != null ? item.Title : string.Empty;
        }

        if (descriptionText != null)
        {
            descriptionText.text = item != null ? item.Description : string.Empty;
        }

        if (iconDisplay != null)
        {
            if (item is UpgradeHoverableItem upgradeHoverableItem)
            {
                iconDisplay.Bind(upgradeHoverableItem.IconSprite);
            }
            else
            {
                iconDisplay.Clear();
            }
        }

        if (item is DetailUpgradeHoverableItem detailUpgradeHoverableItem)
        {
            BindDetailSections(detailUpgradeHoverableItem);
        }
        else
        {
            ClearDetailSections();
        }
    }

    public void Show()
    {
        SetRootVisible(true);
    }

    public void Hide()
    {
        SetRootVisible(false);
    }

    private void ResolveReferences()
    {
        if (root == null)
        {
            root = gameObject;
        }

        if (rectTransform == null)
        {
            rectTransform = GetComponent<RectTransform>();
        }

        if (statInfoUI == null)
        {
            statInfoUI = GetComponentInChildren<UpgradeStatInfoUI>(true);
        }

        if (validEvolutionsRoot == null)
        {
            Transform validEvolutions = FindChildByName(transform, "Valid evolutions");
            if (validEvolutions == null)
            {
                validEvolutions = FindChildByName(transform, "Valid Evolutions");
            }

            if (validEvolutions == null)
            {
                validEvolutions = FindChildByName(transform, "RelatedEvolutions");
            }

            if (validEvolutions != null)
            {
                validEvolutionsRoot = validEvolutions.gameObject;
            }
        }

        if ((validEvolutionSlots == null || validEvolutionSlots.Length == 0) && validEvolutionsRoot != null)
        {
            validEvolutionSlots = validEvolutionsRoot.GetComponentsInChildren<UpgradeIconLevelUI>(true);
        }
    }

    private void BindDetailSections(DetailUpgradeHoverableItem detailUpgradeHoverableItem)
    {
        if (statInfoUI != null)
        {
            detailUpgradeHoverableItem.BindStats(statInfoUI);
        }
        else if (descriptionText != null)
        {
            string statDetails = detailUpgradeHoverableItem.BuildStatDetailsText();
            if (!string.IsNullOrWhiteSpace(statDetails))
            {
                descriptionText.text = string.IsNullOrWhiteSpace(descriptionText.text)
                    ? statDetails
                    : $"{descriptionText.text}\n{statDetails}";
            }
        }

        int visibleEvolutionCount = detailUpgradeHoverableItem.BindRelatedEvolutions(validEvolutionSlots);
        SetValidEvolutionsVisible(visibleEvolutionCount > 0);
    }

    private void ClearDetailSections()
    {
        if (statInfoUI != null)
        {
            statInfoUI.Clear();
        }

        ClearValidEvolutionSlots();
        SetValidEvolutionsVisible(false);
    }

    private void ClearValidEvolutionSlots()
    {
        if (validEvolutionSlots == null)
        {
            return;
        }

        for (int i = 0; i < validEvolutionSlots.Length; i++)
        {
            if (validEvolutionSlots[i] != null)
            {
                validEvolutionSlots[i].Clear();
            }
        }
    }

    private void SetValidEvolutionsVisible(bool isVisible)
    {
        if (validEvolutionsRoot != null && validEvolutionsRoot.activeSelf != isVisible)
        {
            validEvolutionsRoot.SetActive(isVisible);
        }
    }

    private static Transform FindChildByName(Transform parent, string childName)
    {
        if (parent == null || string.IsNullOrWhiteSpace(childName))
        {
            return null;
        }

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (child.name == childName)
            {
                return child;
            }

            Transform match = FindChildByName(child, childName);
            if (match != null)
            {
                return match;
            }
        }

        return null;
    }

    private void ResolveController()
    {
        if (controller == null)
        {
            ServiceLocator.TryResolve(out controller);
        }

        if (controller != null)
        {
            controller.RegisterToolTip(this);
        }
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
