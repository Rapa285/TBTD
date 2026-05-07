# Project Notes

- `UnitEventBus`, `UnitDeploymentController`, `UnitStateManager`, and `CurrencyManager` are resolved through `ServiceLocator`.
- Deployment currently instantiates a preview object and destroys it on cancel/invalid placement. Pooling or an undeployed disabled roster-preview workflow is still a future optimization.
- Roster deployment cost is precompiled by `UnitStateManager` from `TowerEntity.CalculateFinalStat(DeploymentCost, AppliedUpgrades)`, so UI and pre-preview currency checks do not need to instantiate a tower just to read cost.
- `CurrencyManager` tracks current player currency for deployment only. Enemy gold rewards, recall refunds, and currency persistence are not implemented yet.
- `UnitUIDeployment` separates input gating from display state with `DeploymentUIState.CannotDeploy`, `CanDeploy`, and `InDeployPreview`.
- `UnitUICost` displays cached cost for undeployed roster units; `UICurrencyDisplayer` displays current currency.
- Tower selection is centralized in `PlayerStateController` through direct physics raycasts. Configure selectable tower colliders on `TowerUnit`, vision triggers on `TowerVision`, and assign each `TowerSelectionTarget.SelectionCollider` exactly.
- `TowerEntity` owns selected state and range visualization. `UnitVision` range is visible while a tower is selected or in deployment preview.
- Future visual work: deployment preview pooling, richer selected-tower visuals through `TowerSelectionTarget` UnityEvents, and tube-like vision volume support for map height differences.
