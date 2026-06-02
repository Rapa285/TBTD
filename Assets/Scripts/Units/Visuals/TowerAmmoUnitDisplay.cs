using TMPro;
using UnityEngine;

/// <summary>
/// Tower-local ammo text display that initializes on deployment and updates from unit events.
/// </summary>
public class TowerAmmoUnitDisplay : MonoBehaviour
{
    [SerializeField, Tooltip("Optional tower override. Defaults to the parent tower.")]
    private TowerEntity tower;

    [SerializeField, Tooltip("TMP text used to display current and max ammo units.")]
    private TMP_Text textDisplay;

    [SerializeField, Range(0f, 1f), Tooltip("Remaining ammo ratio at or below this value shows the low-ammo warning sprite while ammo remains.")]
    private float lowAmmoWarningThreshold = 0.25f;

    [SerializeField, Tooltip("Optional sprite root shown when this tower is about to run out of ammo.")]
    private GameObject lowAmmoWarningSprite;

    [SerializeField, Tooltip("Optional sprite root shown when this tower has no ammo remaining.")]
    private GameObject outOfAmmoSprite;

    private UnitEventBus eventBus;
    private string boundUnitId;
    private bool towerSubscribed;
    private bool eventBusSubscribed;

    private void Awake()
    {
        ResolveReferences();
        ClearDisplay();
    }

    private void Start()
    {
        ResolveReferences();
        SubscribeToTower();
        SubscribeToEventBus();
        TryInitializeFromTowerState();
    }

    private void OnEnable()
    {
        ResolveReferences();
        SubscribeToTower();
        SubscribeToEventBus();
        TryInitializeFromTowerState();
    }

    private void OnDisable()
    {
        UnsubscribeFromTower();
        UnsubscribeFromEventBus();
        ClearDisplay();
    }

    private void OnValidate()
    {
        lowAmmoWarningThreshold = Mathf.Clamp01(lowAmmoWarningThreshold);
        ResolveReferences();
    }

    private void HandleTowerDeployed(string deployedUnitId)
    {
        if (tower == null || string.IsNullOrWhiteSpace(deployedUnitId))
        {
            boundUnitId = string.Empty;
            ClearDisplay();
            return;
        }

        boundUnitId = deployedUnitId;
        RefreshDisplayFromTower();
    }

    private void HandleUnitAmmoConsumed(UnitAmmoConsumedEvent eventData)
    {
        if (!IsMatchingTower(eventData.UnitId, eventData.Tower))
        {
            return;
        }

        RefreshDisplayFromTower();
    }

    private void HandleTowerModified(TowerModifiedEvent eventData)
    {
        if (!IsMatchingTower(eventData.UnitId, eventData.Tower))
        {
            return;
        }

        RefreshDisplayFromTower();
    }

    private void ResolveReferences()
    {
        if (tower == null)
        {
            tower = GetComponentInParent<TowerEntity>(true);
        }

        if (textDisplay == null)
        {
            textDisplay = GetComponent<TMP_Text>();
        }

        if (eventBus == null)
        {
            ServiceLocator.TryResolve(out eventBus);
        }
    }

    private void SubscribeToTower()
    {
        if (towerSubscribed || tower == null)
        {
            return;
        }

        tower.OnDeploy.AddListener(HandleTowerDeployed);
        towerSubscribed = true;
    }

    private void UnsubscribeFromTower()
    {
        if (!towerSubscribed || tower == null)
        {
            return;
        }

        tower.OnDeploy.RemoveListener(HandleTowerDeployed);
        towerSubscribed = false;
    }

    private void SubscribeToEventBus()
    {
        if (eventBusSubscribed)
        {
            return;
        }

        if (eventBus == null)
        {
            ResolveReferences();
        }

        if (eventBus == null)
        {
            return;
        }

        eventBus.UnitAmmoConsumed += HandleUnitAmmoConsumed;
        eventBus.TowerModified += HandleTowerModified;
        eventBusSubscribed = true;
    }

    private void UnsubscribeFromEventBus()
    {
        if (!eventBusSubscribed || eventBus == null)
        {
            return;
        }

        eventBus.UnitAmmoConsumed -= HandleUnitAmmoConsumed;
        eventBus.TowerModified -= HandleTowerModified;
        eventBusSubscribed = false;
    }

    private void TryInitializeFromTowerState()
    {
        if (tower == null || !tower.Deployed || !tower.HasResolvedUnitId)
        {
            ClearDisplay();
            return;
        }

        HandleTowerDeployed(tower.UnitId);
    }

    private void RefreshDisplayFromTower()
    {
        if (tower == null || string.IsNullOrWhiteSpace(boundUnitId))
        {
            ClearDisplay();
            return;
        }

        int currentAmmo = tower.CurrentAmmoUnits;
        int maxAmmo = tower.MaxAmmoUnits;
        float ammoRatio = maxAmmo > 0 ? Mathf.Clamp01((float)currentAmmo / maxAmmo) : 0f;
        bool isOutOfAmmo = currentAmmo <= 0;
        bool isLowAmmo = !isOutOfAmmo && ammoRatio <= lowAmmoWarningThreshold;

        SetDisplayText($"{currentAmmo} / {maxAmmo}");
        SetAmmoWarningSpritesVisible(isLowAmmo, isOutOfAmmo);
    }

    private bool IsMatchingTower(string eventUnitId, TowerEntity eventTower)
    {
        if (tower == null)
        {
            return false;
        }

        if (eventTower == tower)
        {
            return true;
        }

        return !string.IsNullOrWhiteSpace(boundUnitId) && eventUnitId == boundUnitId;
    }

    private void SetDisplayText(string displayText)
    {
        if (textDisplay == null)
        {
            return;
        }

        textDisplay.text = displayText ?? string.Empty;
        textDisplay.enabled = !string.IsNullOrWhiteSpace(textDisplay.text);
    }

    private void ClearDisplay()
    {
        SetAmmoWarningSpritesVisible(false, false);

        if (textDisplay == null)
        {
            return;
        }

        textDisplay.text = string.Empty;
        textDisplay.enabled = false;
    }

    private void SetAmmoWarningSpritesVisible(bool showLowAmmoWarning, bool showOutOfAmmo)
    {
        SetGameObjectVisible(lowAmmoWarningSprite, showLowAmmoWarning);
        SetGameObjectVisible(outOfAmmoSprite, showOutOfAmmo);
    }

    private static void SetGameObjectVisible(GameObject target, bool isVisible)
    {
        if (target != null && target.activeSelf != isVisible)
        {
            target.SetActive(isVisible);
        }
    }
}
