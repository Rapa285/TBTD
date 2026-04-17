using UnityEngine;
using UnityEngine.InputSystem;

public class UnitDeploymentController : MonoBehaviour
{
    [SerializeField] private UnitDeploymentChecker deploymentChecker;
    [SerializeField] private Transform deployedTowerParent;

    private GameObject currentDraggedRoot;
    private TowerEntity currentDraggedTower;
    private MaterialOverrider currentMaterialOverrider;
    private UnitDeploymentChecker.PlacementResult currentPlacementResult;
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

    public bool BeginDeployment(TowerEntity towerPrefab)
    {
        return towerPrefab != null && BeginDeployment(towerPrefab.gameObject);
    }

    public bool BeginDeployment(GameObject unitPrefab)
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
        hasCurrentPlacement = false;
    }
}
