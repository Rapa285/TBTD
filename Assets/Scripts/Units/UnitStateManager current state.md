# UnitStateManager Current State

## Role
`UnitStateManager` is the player roster state manager. It stores shared XP thresholds, long-lived per-unit identity, progression, selected multi-upgrade levels, one selected evolution, cached deployment cost, and the transient scene reference for the unit's currently deployed tower.

It does not own combat execution. Runtime stats, weapon override/augment composition, targeting, attack timing, vision range, and projectile modifiers remain owned by `TowerEntity`, `UnitVision`, `AttackBehaviour`, and projectile scripts.

## Owned Unit State
Each roster entry is a nested serializable `OwnedUnitState` keyed by a stable `unitId`.

Stored inspector/runtime state:
- Shared progression config: `UnitStateManager.xpThresholds`, indexed by level; index 0 is the threshold from level 1 to 2.
- Unit identity: `unitId`, `displayName`, `icon`, and `TowerEntity unitPrefab`.
- Persistent progression state: `level`, `experience`, `upgradePending`, `appliedMultiUpgrades`, and `selectedEvolution`.
- Transient deployment state: `currentRuntimeInstance` and `currentRuntimeRoot`.
- Transient economy/UI cache: `hasCompiledDeploymentCost` and `deploymentCost`.

Important rules:
- `unitId` is the public lookup key for UI, deployment, progression, and upgrade selection.
- Duplicate `unitId` values log an error; empty IDs log a warning.
- `currentRuntimeInstance` and `currentRuntimeRoot` are scene references only. They are not save data.
- `appliedMultiUpgrades` stores selected `MultiUpgradeSO` line state plus the active selected level for each line. It does not cache offer pools, runtime weapons, or projectile modifier instances.
- `selectedEvolution` stores the one selected `EvolutionSO` for the unit when it has evolved.
- `AppliedUpgrades` is a resolved read-only view of the active `UpgradeSO` leaves from `appliedMultiUpgrades` plus the selected evolution's resolved `UpgradeSO` leaf.
- Cached deployment cost is a runtime/pre-UI value compiled from `TowerEntity.CalculateFinalStat(DeploymentCost, AppliedUpgrades)`. It is not a second runtime stat pipeline.

## Runtime Bridge
`UnitStateManager` applies roster state to runtime towers through public bridge methods.

- `ApplyStateTo(unitId, runtimeRoot, tower, evaluateThreshold)` applies every resolved active `UpgradeSO` leaf to the tower with `TowerEntity.AddUpgrade`, ensures a `UnitProgression` component exists, and initializes progression state.
- `Precompile()` refreshes cached deployment costs for every roster unit without instantiating previews.
- `TryGetDeploymentCost(unitId, out cost)` exposes cached cost for deployment and UI.
- `BindRuntimeInstance(unitId, tower, runtimeRoot)` records the currently deployed tower/root and refreshes `UnitProgression`.
- `ClearRuntimeInstance(unitId, tower)` clears the transient runtime reference only if it matches the recorded tower.
- `RecallUnit(unitId)` records current runtime XP, clears the runtime reference, destroys the runtime root, and raises `UnitRecalled`.

Deployment integration:
- `UIUnitItem` can deploy either a direct prefab or a managed roster unit.
- `UnitDeploymentController.BeginDeployment(UnitStateManager, string)` blocks unknown units, already-deployed units, cooling-down units, units with no prefab, invalid state manager input, and unaffordable cached deployment costs when a `CurrencyManager` exists.
- Managed deployment applies roster state before preview, calls `PrepareForDeploymentPreview()`, raises preview lifecycle events, spends currency only on final placement, and only binds the runtime instance after valid placement and before `Deploy()`.
- Direct prefab deployment remains outside roster currency enforcement.

## Progression And Upgrade Selection
`UnitProgression` lives on the deployed runtime unit. It tracks current XP, level, next threshold, and whether an upgrade is pending.

Current flow:
- Runtime systems add XP by calling `UnitProgression.AddExperience(amount)` on the deployed unit.
- `UnitProgression` raises `UnitExperienceChanged`; `UnitStateManager` records the latest XP for the matching `unitId`.
- When XP reaches the next threshold, `UnitProgression` raises `UnitUpgradeThresholdReached`.
- `UpgradesManager` listens, asks `UnitStateManager.TryBeginUpgradeSelection(unitId)` to mark the unit pending, builds an offer from its shared `MultiUpgradeSO` pool, and records the final selected multi-upgrade line.
- `UpgradesManager` builds offers from its shared `MultiUpgradeSO` pool plus eligible `EvolutionSO` entries from its evolution pool.
- `UnitStateManager.RecordSelectedUpgrade(unitId, multiUpgrade, evolution)` clears pending state, advances the unit level, and records either a selected multi-upgrade level or a selected evolution.
- Multi-upgrade selections apply the resolved next-level `UpgradeSO` to the deployed tower through `TowerEntity.ReplaceUpgrade`.
- Evolution selections are accepted only if the unit has no selected evolution and all prerequisites are met, then apply the resolved `UpgradeSO` to the deployed tower through `TowerEntity.AddUpgrade`.
- After either selection, cached deployment cost is refreshed and runtime `UnitProgression` is refreshed.

## Currency And UI Events
Roster deployment cost is exposed through `UnitEventBus`.

- `UnitDeploymentCostCompiled` is raised when cached deployment cost is compiled or cleared.
- `CurrencyChanged` is raised by `CurrencyManager` whenever balance changes.
- `UnitDeploymentPreviewStarted` and `UnitDeploymentPreviewEnded` allow UI to distinguish `CanDeploy` from `InDeployPreview`.
- `UnitUICost` shows cached cost only while the roster unit is undeployed.
- `UnitUIDeployment.CurrentState` is display state; input gating still blocks deployment while any preview is active.

Current limitation:
- Multi-threshold catch-up is not implemented. After an upgrade is selected, refreshed progression may evaluate the next threshold, but the system still resolves one pending upgrade selection at a time.

## Boundaries
- Do not move runtime stat math, weapon composition, or projectile modifier composition into `UnitStateManager`; extend `TowerEntity.CompileFinalStats()` or side-effect-free `TowerEntity` stat helpers instead.
- Do not inspect `UpgradeSO` effect contents in `UnitStateManager`; resolve active level references from `MultiUpgradeSO` and pass those `UpgradeSO` leaves to `TowerEntity` helpers when a cached single-stat preview is needed.
- Do not store upgrade offer pools or offer counts on `OwnedUnitState`; shared offer generation currently belongs to `UpgradesManager`.
- Do not put targeting, cooldowns, attack behaviour selection, vision, projectile logic, raycasts, mouse input, or placement validation in `UnitStateManager`.
- Keep roster state per-unit. XP, upgrades, pending status, and runtime instance references must not leak between `OwnedUnitState` entries.
- Keep scripts in the global namespace unless the whole project is intentionally migrated.

## Verification Checklist
- `dotnet build Assembly-CSharp.csproj` after script changes.
- Managed and direct `UIUnitItem` deployment paths both still work.
- A managed unit cannot deploy twice at the same time.
- Applied multi-upgrade levels persist after recall and redeploy.
- Cached deployment cost updates on startup and after selected upgrades.
- `UnitDeploymentCostCompiled` is raised when cost cache changes.
- Selecting a multi-upgrade while deployed updates only that unit's active runtime tower.
- Selecting an evolution while deployed updates only that unit's active runtime tower and prevents later evolution selections for that unit.
- Duplicate or empty `unitId` entries report configuration errors.
