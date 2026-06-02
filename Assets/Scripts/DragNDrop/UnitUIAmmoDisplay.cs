using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Displays deployed runtime ammo for one managed unit item.
/// </summary>
[RequireComponent(typeof(UIUnitItem))]
public class UnitUIAmmoDisplay : UnitUIBehaviour
{
    [SerializeField, Tooltip("TMP text used to display current and maximum ammo.")]
    private TMP_Text ammoText;

    [SerializeField, Tooltip("Root GameObject for the ammo bar. The fill should be a child of this object.")]
    private GameObject ammoBarRoot;

    [SerializeField, Tooltip("Child RectTransform whose width displays current ammo ratio.")]
    private RectTransform ammoFillRect;

    [SerializeField, Tooltip("Optional fill graphic whose color communicates remaining ammo state. Defaults to the fill RectTransform's Graphic.")]
    private Graphic ammoFillGraphic;

    private Image ammoFillImage;
    [SerializeField, Tooltip("Optional root shown only when runtime ammo is available. Defaults to the bar root, then the text object.")]
    private GameObject ammoRoot;

    [SerializeField, Range(0f, 1f), Tooltip("Remaining ammo ratio above this value uses the high ammo color.")]
    private float highThreshold = 0.5f;

    [SerializeField, Range(0f, 1f), Tooltip("Remaining ammo ratio above this value uses the mid ammo color; lower positive values use the low ammo color.")]
    private float midThreshold = 0.25f;

    [SerializeField, Tooltip("Fill color used above the high threshold.")]
    private Color highAmmoColor = Color.green;

    [SerializeField, Tooltip("Fill color used between the mid and high thresholds.")]
    private Color midAmmoColor = Color.yellow;

    [SerializeField, Tooltip("Fill color used below the mid threshold while ammo remains.")]
    private Color lowAmmoColor = new Color(1f, 0.5f, 0f, 1f);

    [SerializeField, Tooltip("Fill color used when the unit has no remaining ammo.")]
    private Color emptyAmmoColor = Color.red;

    [SerializeField, Min(0.01f), Tooltip("Seconds for one full empty-ammo flash cycle.")]
    private float emptyFlashPeriod = 0.6f;

    [SerializeField, Range(0f, 1f), Tooltip("Minimum alpha used while flashing at zero ammo.")]
    private float emptyFlashMinAlpha = 0.35f;

    private bool isEmptyFlashing;
    private bool isExternalStatusVisible;

    protected override void Awake()
    {
        base.Awake();
        EnsureSetupDisplayCompanion();
        ClearDisplay();
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

    private void Update()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        if (!IsSubscribedToEventBus)
        {
            SubscribeToEventBusIfNeeded();
        }

        if (isEmptyFlashing)
        {
            UpdateEmptyFlash();
        }
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
        highThreshold = Mathf.Clamp01(highThreshold);
        midThreshold = Mathf.Clamp(midThreshold, 0f, highThreshold);
        emptyFlashPeriod = Mathf.Max(0.01f, emptyFlashPeriod);
        ResolveDisplayReferences();
    }

    protected override void ResolveReferences()
    {
        base.ResolveReferences();
        ResolveDisplayReferences();
    }

    private void EnsureSetupDisplayCompanion()
    {
        if (!Application.isPlaying || GetComponent<UnitUISetupDisplay>() != null)
        {
            return;
        }

        gameObject.AddComponent<UnitUISetupDisplay>();
    }

    protected override void SubscribeToEvents(UnitEventBus eventBus)
    {
        eventBus.UnitDeployed += HandleUnitDeployed;
        eventBus.UnitRecalled += HandleUnitRecalled;
        eventBus.UnitAmmoConsumed += HandleUnitAmmoConsumed;
        eventBus.TowerModified += HandleTowerModified;
    }

    protected override void UnsubscribeFromEvents(UnitEventBus eventBus)
    {
        eventBus.UnitDeployed -= HandleUnitDeployed;
        eventBus.UnitRecalled -= HandleUnitRecalled;
        eventBus.UnitAmmoConsumed -= HandleUnitAmmoConsumed;
        eventBus.TowerModified -= HandleTowerModified;
    }

    private void HandleUnitDeployed(UnitDeployedEvent eventData)
    {
        if (IsMatchingUnit(eventData.UnitId))
        {
            RefreshDisplay();
        }
    }

    private void HandleUnitRecalled(UnitRecalledEvent eventData)
    {
        if (IsMatchingUnit(eventData.UnitId))
        {
            ClearDisplay();
        }
    }

    private void HandleUnitAmmoConsumed(UnitAmmoConsumedEvent eventData)
    {
        if (IsMatchingUnit(eventData.UnitId))
        {
            RefreshDisplay();
        }
    }

    private void HandleTowerModified(TowerModifiedEvent eventData)
    {
        if (IsMatchingUnit(eventData.UnitId))
        {
            RefreshDisplay();
        }
    }

    public void RefreshDisplay()
    {
        ResolveReferences();

        if (isExternalStatusVisible)
        {
            return;
        }

        if (!TryGetRuntimeTower(out TowerEntity tower))
        {
            ClearDisplay();
            return;
        }

        if (tower.IsInSetupTime)
        {
            ClearDisplay();
            return;
        }

        if (!UsesFiniteAmmo(tower))
        {
            ClearDisplay();
            return;
        }

        int currentAmmo = tower.CurrentAmmoUnits;
        int maxAmmo = tower.MaxAmmoUnits;
        bool isEmpty = currentAmmo <= 0;
        float ratio = maxAmmo > 0 ? Mathf.Clamp01((float)currentAmmo / maxAmmo) : 0f;

        SetAmmoVisible(true);

        if (ammoText != null)
        {
            ammoText.text = $"{currentAmmo} / {maxAmmo}";
            ammoText.enabled = true;
        }

        SetFillAmount(isEmpty ? 1f : ratio);

        isEmptyFlashing = isEmpty;
        SetFillColor(isEmpty ? emptyAmmoColor : GetBandColor(ratio));
    }

    public void ShowExternalStatus(string displayText, float normalizedFill, Color fillColor)
    {
        ResolveReferences();

        isExternalStatusVisible = true;
        isEmptyFlashing = false;
        SetAmmoVisible(true);

        if (ammoText != null)
        {
            ammoText.text = displayText ?? string.Empty;
            ammoText.enabled = true;
        }

        SetFillAmount(normalizedFill);
        SetFillColor(fillColor);
    }

    public void ClearExternalStatus(bool refreshAmmoDisplay)
    {
        if (!isExternalStatusVisible)
        {
            if (refreshAmmoDisplay)
            {
                RefreshDisplay();
            }

            return;
        }

        isExternalStatusVisible = false;

        if (refreshAmmoDisplay)
        {
            RefreshDisplay();
            return;
        }

        ClearDisplay();
    }

    private bool TryGetRuntimeTower(out TowerEntity tower)
    {
        tower = null;

        if (!TryGetManagedUnit(out UnitStateManager.OwnedUnitState unit))
        {
            return false;
        }

        tower = unit.CurrentRuntimeInstance;
        if (tower == null || !tower.Deployed)
        {
            return false;
        }

        return true;
    }

    private bool UsesFiniteAmmo(TowerEntity tower)
    {
        return tower != null
            && tower.ActiveAttackBehaviour != null
            && tower.ActiveAttackBehaviour.UsesFiniteAmmo;
    }

    private Color GetBandColor(float ratio)
    {
        if (ratio > highThreshold)
        {
            return highAmmoColor;
        }

        if (ratio >= midThreshold)
        {
            return midAmmoColor;
        }

        return lowAmmoColor;
    }

    private void UpdateEmptyFlash()
    {
        if (ammoFillGraphic == null)
        {
            return;
        }

        float wave = Mathf.PingPong(Time.unscaledTime / emptyFlashPeriod, 1f);
        Color color = emptyAmmoColor;
        color.a = Mathf.Lerp(emptyFlashMinAlpha, emptyAmmoColor.a, wave);
        ammoFillGraphic.color = color;
    }

    private void ClearDisplay()
    {
        isExternalStatusVisible = false;
        isEmptyFlashing = false;
        SetAmmoVisible(false);

        if (ammoText != null)
        {
            ammoText.text = string.Empty;
            ammoText.enabled = false;
        }

        SetFillAmount(0f);
        SetFillColor(highAmmoColor);
    }

    private void ResolveDisplayReferences()
    {
        if (ammoText == null)
        {
            ammoText = GetComponentInChildren<TMP_Text>(true);
        }

        if (ammoBarRoot == null && ammoFillRect != null)
        {
            ammoBarRoot = ammoFillRect.parent != null
                ? ammoFillRect.parent.gameObject
                : ammoFillRect.gameObject;
        }

        if (ammoFillRect == null && ammoBarRoot != null)
        {
            RectTransform rootRect = ammoBarRoot.GetComponent<RectTransform>();
            RectTransform[] childRects = ammoBarRoot.GetComponentsInChildren<RectTransform>(true);
            for (int i = 0; i < childRects.Length; i++)
            {
                if (childRects[i] != null && childRects[i] != rootRect)
                {
                    ammoFillRect = childRects[i];
                    break;
                }
            }
        }

        if (ammoFillGraphic == null && ammoFillRect != null)
        {
            ammoFillGraphic = ammoFillRect.GetComponent<Graphic>();
        }

        if (ammoFillImage == null && ammoFillGraphic != null)
        {
            ammoFillImage = ammoFillGraphic as Image;
        }

        if (ammoFillImage == null && ammoFillRect != null)
        {
            ammoFillImage = ammoFillRect.GetComponent<Image>();
        }

        if (ammoRoot == null)
        {
            if (ammoBarRoot != null)
            {
                ammoRoot = ammoBarRoot;
            }
            else if (ammoText != null)
            {
                ammoRoot = ammoText.gameObject;
            }
        }
    }

    private void SetAmmoVisible(bool isVisible)
    {
        GameObject target = ammoRoot != null
            ? ammoRoot
            : ammoBarRoot != null ? ammoBarRoot : ammoText != null ? ammoText.gameObject : null;

        if (target != null && target != gameObject && target.activeSelf != isVisible)
        {
            target.SetActive(isVisible);
        }
    }

    private void SetFillAmount(float normalized)
    {
        float fill = Mathf.Clamp01(normalized);
        if (ammoFillImage != null)
        {
            ConfigureFillImage();
            ammoFillImage.fillAmount = fill;
            return;
        }

        if (ammoFillRect == null)
        {
            return;
        }

        Vector2 anchorMin = ammoFillRect.anchorMin;
        Vector2 anchorMax = ammoFillRect.anchorMax;
        Vector2 offsetMin = ammoFillRect.offsetMin;
        Vector2 offsetMax = ammoFillRect.offsetMax;

        anchorMin.x = 0f;
        anchorMax.x = fill;
        offsetMin.x = 0f;
        offsetMax.x = 0f;

        ammoFillRect.anchorMin = anchorMin;
        ammoFillRect.anchorMax = anchorMax;
        ammoFillRect.offsetMin = offsetMin;
        ammoFillRect.offsetMax = offsetMax;
    }

    private void ConfigureFillImage()
    {
        if (ammoFillImage == null)
        {
            return;
        }

        ammoFillImage.type = Image.Type.Filled;
        ammoFillImage.fillMethod = Image.FillMethod.Horizontal;
        ammoFillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
    }

    private void SetFillColor(Color color)
    {
        if (ammoFillGraphic != null)
        {
            ammoFillGraphic.color = color;
        }
    }
}
