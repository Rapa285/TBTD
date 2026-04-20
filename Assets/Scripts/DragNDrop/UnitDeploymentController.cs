using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Owns tower deployment previews, placement validation, cancellation, and final runtime binding.
/// </summary>
public class UnitDeploymentController : MonoBehaviour
{
    [SerializeField, Tooltip("Placement service used to convert mouse position into valid world placement results.")]
    private UnitDeploymentChecker deploymentChecker;

    [SerializeField, Tooltip("Optional parent assigned to preview and deployed tower instances.")]
    private Transform deployedTowerParent;

    private GameObject currentDraggedRoot;
    private TowerEntity currentDraggedTower;
    private MaterialOverrider currentMaterialOverrider;
    private UnitDeploymentChecker.PlacementResult currentPlacementResult;
    private UnitStateManager currentStateManager;
    private string currentUnitId;
    private bool hasCurrentPlacement;

    public bool IsDragging => currentDraggedRoot != null;
    public TowerEntity CurrentDraggedTower => currentDraggedTower;

    private void Awake()
    {
        if (deploymentChecker == null)
        {
            deploymentChecker = GetComponent<UnitDeploymentChecker>();
        }
    }

    private void Update()
    {
        if (!IsDragging)
        {
            return;
        }

        Mouse mouse = Mouse.current;
        if (mouse == null)
        {
            CancelDeployment();
            return;
        }

        UpdateCurrentPlacement(mouse.position.ReadValue());

        if (mouse.rightButton.wasPressedThisFrame)
        {
            CancelDeployment();
            return;
        }

        if (mouse.leftButton.wasReleasedThisFrame)
        {
            if (hasCurrentPlacement && currentPlacementResult.isValid)
            {
                CompleteDeployment();
            }
            else
            {
                CancelDeployment();
            }
        }
    }

    /// <summary>
    /// Begins deployment for a direct tower prefab reference.
    /// </summary>
    public bool BeginDeployment(TowerEntity towerPrefab)
    {
        return towerPrefab != null && BeginDeployment(towerPrefab.gameObject);
    }

    /// <summary>
    /// Begins deployment for a direct prefab that contains a TowerEntity.
    /// </summary>
    public bool BeginDeployment(GameObject unitPrefab)
    {
        return BeginDeployment(unitPrefab, null, null);
    }

    /// <summary>
    /// Begins deployment for a persistent roster unit, applying saved state before preview.
    /// </summary>
    public bool BeginDeployment(UnitStateManager stateManager, string unitId)
    {
        if (stateManager == null
            || string.IsNullOrWhiteSpace(unitId)
            || !stateManager.CanDeploy(unitId)
            || !stateManager.TryGetUnit(unitId, out UnitStateManager.OwnedUnitState unit)
            || unit.UnitPrefab == null)
        {
            return false;
        }

        return BeginDeployment(unit.UnitPrefab.gameObject, stateManager, unitId);
    }

    private bool BeginDeployment(GameObject unitPrefab, UnitStateManager stateManager, string unitId)
    {
        if (unitPrefab == null || IsDragging || Mouse.current == null)
        {
            return false;
        }

        if (deploymentChecker == null)
        {
            deploymentChecker = GetComponent<UnitDeploymentChecker>();
        }

        if (deploymentChecker == null)
        {
            return false;
        }

        currentDraggedRoot = Instantiate(unitPrefab, deployedTowerParent);
        currentDraggedTower = currentDraggedRoot.GetComponent<TowerEntity>();
        if (currentDraggedTower == null)
        {
            currentDraggedTower = currentDraggedRoot.GetComponentInChildren<TowerEntity>();
        }

        if (currentDraggedTower == null)
        {
            Destroy(currentDraggedRoot);
            ClearCurrentDeployment();
            return false;
        }

        if (stateManager != null && !stateManager.ApplyStateTo(unitId, currentDraggedRoot, currentDraggedTower, false))
        {
            Destroy(currentDraggedRoot);
            ClearCurrentDeployment();
            return false;
        }

        // Roster-managed previews receive saved upgrades before preview mode so placement stats match runtime stats.
        currentStateManager = stateManager;
        currentUnitId = unitId;
        currentDraggedTower.PrepareForDeploymentPreview();

        currentMaterialOverrider = currentDraggedRoot.GetComponentInChildren<MaterialOverrider>();
        if (currentMaterialOverrider != null)
        {
            currentMaterialOverrider.ShowNeutralPreview();
        }

        hasCurrentPlacement = false;
        UpdateCurrentPlacement(Mouse.current.position.ReadValue());
        return true;
    }

    /// <summary>
    /// Cancels the active deployment preview without mutating roster state.
    /// </summary>
    public void CancelDeployment()
    {
        if (!IsDragging)
        {
            return;
        }

        if (currentMaterialOverrider != null)
        {
            currentMaterialOverrider.RestoreOriginalMaterials();
        }

        Destroy(currentDraggedRoot);
        ClearCurrentDeployment();
    }

    private void CompleteDeployment()
    {
        if (!IsDragging)
        {
            return;
        }

        if (currentMaterialOverrider != null)
        {
            currentMaterialOverrider.RestoreOriginalMaterials();
        }

        currentDraggedRoot.transform.position = currentPlacementResult.position;
        currentDraggedTower.Deploy();

        if (currentStateManager != null)
        {
            // Bind only after a valid placement so canceled previews never become roster runtime instances.
            currentStateManager.BindRuntimeInstance(currentUnitId, currentDraggedTower, currentDraggedRoot);
        }

        ClearCurrentDeployment();
    }

    private void UpdateCurrentPlacement(Vector2 screenPosition)
    {
        if (deploymentChecker == null || currentDraggedRoot == null)
        {
            hasCurrentPlacement = false;
            return;
        }

        deploymentChecker.TryGetPlacement(screenPosition, out currentPlacementResult);
        hasCurrentPlacement = currentPlacementResult.hasGround;

        if (currentPlacementResult.hasGround)
        {
            currentDraggedRoot.transform.position = currentPlacementResult.position;
        }

        if (currentMaterialOverrider == null)
        {
            return;
        }

        if (currentPlacementResult.isValid)
        {
            currentMaterialOverrider.ShowValidPlacement();
        }
        else
        {
            currentMaterialOverrider.ShowInvalidPlacement();
        }
    }

    private void ClearCurrentDeployment()
    {
        currentDraggedRoot = null;
        currentDraggedTower = null;
        currentMaterialOverrider = null;
        currentPlacementResult = default;
        currentStateManager = null;
        currentUnitId = null;
        hasCurrentPlacement = false;
    }
}
