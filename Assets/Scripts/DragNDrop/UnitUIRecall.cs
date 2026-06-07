using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Legacy roster-card recall entry point. Kept so existing scene references compile while recall moves to details and tower UI.
/// </summary>
[RequireComponent(typeof(UIUnitItem))]
public class UnitUIRecall : MonoBehaviour
{
    [SerializeField, HideInInspector]
    private UIUnitItem uiUnitItem;

    [SerializeField, HideInInspector]
    private UnitUIDeployment unitUiDeployment;

    [SerializeField, Tooltip("Legacy recall button root to hide now that roster-card recall has moved.")]
    private Button recallButton;

    [SerializeField, Tooltip("Legacy recall root to hide now that roster-card recall has moved. Defaults to the recall button object.")]
    private GameObject recallButtonRoot;

    private void Awake()
    {
        ResolveReferences();
        HideLegacyRecallButton();
    }

    private void Start()
    {
        HideLegacyRecallButton();
    }

    private void OnEnable()
    {
        ResolveReferences();
        HideLegacyRecallButton();
    }

    private void OnValidate()
    {
        ResolveReferences();
    }

    private void ResolveReferences()
    {
        if (uiUnitItem == null)
        {
            uiUnitItem = GetComponent<UIUnitItem>();
        }

        if (unitUiDeployment == null)
        {
            unitUiDeployment = GetComponent<UnitUIDeployment>();
        }

        if (recallButtonRoot == null && recallButton != null)
        {
            recallButtonRoot = recallButton.gameObject;
        }
    }

    private void HideLegacyRecallButton()
    {
        GameObject target = recallButtonRoot != null
            ? recallButtonRoot
            : recallButton != null ? recallButton.gameObject : null;

        if (target != null && target.activeSelf)
        {
            target.SetActive(false);
        }
    }
}
