# UnitStateManager Current State

## Role
`UnitStateManager` is the player roster state manager. It stores long-lived per-unit identity, progression, selected upgrades, and the transient scene reference for the unit's currently deployed tower.

It does not own combat execution. Runtime stats, weapon override/augment composition, targeting, attack timing, vision range, and projectile modifiers remain owned by `TowerEntity`, `UnitVision`, `AttackBehaviour`, and projectile scripts.

## Owned Unit State
Each roster entry is a nested serializable `OwnedUnitState` keyed by a stable `unitId`.

Stored inspector/runtime state:
- Unit identity: `unitId`, `displayName`, `icon`, and `TowerEntity unitPrefab`.
- Progression config: `xpThresholds`.
- Persistent progression state: `level`, `experience`, `upgradePending`, and append-only `appliedUpgrades`.
- Transient deployment state: `currentRuntimeInstance` and `currentRuntimeRoot`.

Important rules:
- `unitId` is the public lookup key for UI, deployment, progression, and upgrade selection.
- Duplicate `unitId` values log an error; empty IDs log a warning.
- `currentRuntimeInstance` and `currentRuntimeRoot` are scene references only. They are not save data.
- `appliedUpgrades` stores selected `UpgradeSO` assets. It does not cache offer pools, compiled stats, runtime weapons, or projectile modifier instances.

## Runtime Bridge
`UnitStateManager` applies roster state to runtime towers through public bridge methods.

- `ApplyStateTo(unitId, runtimeRoot, tower, evaluateThreshold)` applies every stored upgrade to the tower with `TowerEntity.AddUpgrade`, ensures a `UnitProgression` component exists, and initializes progression state.
- `BindRuntimeInstance(unitId, tower, runtimeRoot)` records the currently deployed tower/root and refreshes `UnitProgression`.
- `ClearRuntimeInstance(unitId, tower)` clears the transient runtime reference only if it matches the recorded tower.
- `RecallUnit(unitId)` records current runtime XP, clears the runtime reference, destroys the runtime root, and raises `UnitRecalled`.

Deployment integration:
- `UIUnitItem` can deploy either a direct prefab or a managed roster unit.
- `UnitDeploymentController.BeginDeployment(UnitStateManager, string)` blocks unknown units, already-deployed units, units with no prefab, or invalid state manager input.
- Managed deployment applies roster state before preview, calls `PrepareForDeploymentPreview()`, and only binds the runtime instance after valid placement and `Deploy()`.

## Progression And Upgrade Selection
`UnitProgression` lives on the deployed runtime unit. It tracks current XP, level, next threshold, and whether an upgrade is pending.

Current flow:
- Runtime systems add XP by calling `UnitProgression.AddExperience(amount)` on the deployed unit.
- `UnitProgression` raises `UnitExperienceChanged`; `UnitStateManager` records the latest XP for the matching `unitId`.
- When XP reaches the next threshold, `UnitProgression` raises `UnitUpgradeThresholdReached`.
- `UpgradesManager` listens, asks `UnitStateManager.TryBeginUpgradeSelection(unitId)` to mark the unit pending, builds an offer from its shared pool, and records the final selected upgrade.
- `UnitStateManager.RecordSelectedUpgrade(unitId, upgrade)` clears pending state, advances the level, appends the upgrade if it is new, applies it immediately to the deployed tower through `TowerEntity.AddUpgrade`, and refreshes `UnitProgression`.

Current limitation:
- Multi-threshold catch-up is not implemented. After an upgrade is selected, refreshed progression may evaluate the next threshold, but the system still resolves one pending upgrade selection at a time.

## Boundaries
- Do not move stat math, weapon composition, or projectile modifier composition into `UnitStateManager`; extend `TowerEntity.CompileFinalStats()` instead.
- Do not inspect `UpgradeSO` contents in `UnitStateManager` except for storing references and checking whether a selected asset is already applied.
- Do not store upgrade offer pools or offer counts on `OwnedUnitState`; shared offer generation currently belongs to `UpgradesManager`.
- Do not put targeting, cooldowns, attack behaviour selection, vision, projectile logic, raycasts, mouse input, or placement validation in `UnitStateManager`.
- Keep roster state per-unit. XP, upgrades, pending status, and runtime instance references must not leak between `OwnedUnitState` entries.
- Keep scripts in the global namespace unless the whole project is intentionally migrated.

## Verification Checklist
- `dotnet build Assembly-CSharp.csproj` after script changes.
- Managed and direct `UIUnitItem` deployment paths both still work.
- A managed unit cannot deploy twice at the same time.
- Applied upgrades persist after recall and redeploy.
- Selecting an upgrade while deployed updates only that unit's active runtime tower.
- Duplicate or empty `unitId` entries report configuration errors.
