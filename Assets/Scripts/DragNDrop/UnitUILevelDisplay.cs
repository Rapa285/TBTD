using TMPro;
using UnityEngine;

/// <summary>
/// Displays the current roster level for one managed unit item.
/// </summary>
[RequireComponent(typeof(UIUnitItem))]
public class UnitUILevelDisplay : UnitUIBehaviour
{
    [SerializeField, Tooltip("TMP text used to display the unit level.")]
    private TMP_Text levelText;

    [SerializeField, Tooltip("Optional root shown only when a managed unit level is available. Defaults to the text object.")]
    private GameObject levelRoot;

    [SerializeField, Tooltip("Composite format used to display the level. {0} is the level value.")]
    private string levelFormat = "LVL {0}";

    protected override void Awake()
    {
        base.Awake();
        RefreshDisplay();
    }

    protected override void Start()
    {
        base.Start();
        RefreshDisplay();
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        RefreshDisplay();
    }

    protected override void OnDisable()
    {
        if (Application.isPlaying)
        {
            ClearDisplay();
        }

        base.OnDisable();
    }

    protected override void OnValidate()
    {
        base.OnValidate();
        ResolveDisplayReferences();
    }

    protected override void ResolveReferences()
    {
        base.ResolveReferences();
        ResolveDisplayReferences();
    }

    protected override void SubscribeToEvents(UnitEventBus eventBus)
    {
        eventBus.UnitLevelChanged += HandleUnitLevelChanged;
    }

    protected override void UnsubscribeFromEvents(UnitEventBus eventBus)
    {
        eventBus.UnitLevelChanged -= HandleUnitLevelChanged;
    }

    private void HandleUnitLevelChanged(UnitLevelChangedEvent eventData)
    {
        if (IsMatchingUnit(eventData.UnitId))
        {
            RefreshDisplay();
        }
    }

    private void RefreshDisplay()
    {
        ResolveReferences();

        if (!TryGetManagedUnit(out UnitStateManager.OwnedUnitState unit))
        {
            ClearDisplay();
            return;
        }

        SetLevelVisible(true);

        if (levelText != null)
        {
            levelText.text = string.Format(levelFormat, unit.Level);
            levelText.enabled = true;
        }
    }

    private void ClearDisplay()
    {
        SetLevelVisible(false);

        if (levelText != null)
        {
            levelText.text = string.Empty;
            levelText.enabled = false;
        }
    }

    private void ResolveDisplayReferences()
    {
        if (levelText == null)
        {
            levelText = GetComponentInChildren<TMP_Text>(true);
        }

        if (levelRoot == null && levelText != null)
        {
            levelRoot = levelText.gameObject;
        }
    }

    private void SetLevelVisible(bool isVisible)
    {
        GameObject target = levelRoot != null
            ? levelRoot
            : levelText != null ? levelText.gameObject : null;

        if (target != null && target != gameObject && target.activeSelf != isVisible)
        {
            target.SetActive(isVisible);
        }
    }
}
