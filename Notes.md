# Project Notes

- `UnitEventBus`, `UnitDeploymentController`, `UnitStateManager`, and `CurrencyManager` are resolved through `ServiceLocator`.
- Deployment currently instantiates a preview object and destroys it on cancel/invalid placement. Pooling or an undeployed disabled roster-preview workflow is still a future optimization.
- Roster deployment cost is precompiled by `UnitStateManager` from `TowerEntity.CalculateFinalStat(DeploymentCost, AppliedUpgrades)`, so UI and pre-preview currency checks do not need to instantiate a tower just to read cost.
- `CurrencyManager` tracks current player currency for deployment only. Enemy gold rewards, recall refunds, and currency persistence are not implemented yet.
- `UnitUIDeployment` separates input gating from display state with `DeploymentUIState.CannotDeploy`, `CanDeploy`, and `InDeployPreview`.
- `UnitUICost` displays cached cost for undeployed roster units; `UICurrencyDisplayer` displays current currency.
- Future visual work: deployment preview pooling, unit vision circle/range synchronization, tube-like vision volume for map height differences, and showing unit vision only while hovered.
