# UnitStateManager Implementation Plan

## Summary
Implement `UnitStateManager` as the player roster state manager. One manager tracks all player-owned units, with the starting roster expected to be around 10 base units but not hard-coded.

Each owned unit is represented by a nested serializable `OwnedUnitState` record keyed by a stable `unitId`. Combat behavior, targeting, final stat compilation, and attack timing remain owned by `TowerEntity`, `UnitVision`, and `AttackBehaviour`.

## Public API / Interfaces
- `UnitStateManager`
  - Serialized roster: `List<OwnedUnitState> ownedUnits`.
  - Read-only roster access: `OwnedUnits`, `TryGetUnit(string unitId, out OwnedUnitState unit)`, and `HasUnit(string unitId)`.
  - Progression methods: `AddExperience(string unitId, float amount)`, `RequestLevelUp(string unitId)`, `SelectUpgrade(string unitId, int choiceIndex)`, `SelectUpgrade(string unitId, UpgradeSO upgrade)`, and `ApplyUpgrade(string unitId, UpgradeSO upgrade)`.
  - Runtime bridge methods: `CanDeploy(string unitId)`, `ApplyStateTo(string unitId, TowerEntity tower)`, `BindRuntimeInstance(string unitId, TowerEntity tower)`, `ClearRuntimeInstance(string unitId, TowerEntity tower)`, and `RetreatUnit(string unitId)`.
  - Inspector-friendly events should include the changed `unitId`, using a serializable Unity event wrapper if needed. UI should read the latest details from the manager after events fire.

- `OwnedUnitState`
  - Stable `unitId` string for UI and deployment calls.
  - Unit info: display name, optional icon, and plain `TowerEntity` prefab.
  - Progression config: XP thresholds, upgrade choice count defaulting to `3`, and a unit-specific `List<UpgradeSO>` upgrade pool.
  - Runtime progression state: level starting at `1`, current XP, applied upgrades, and pending upgrade choices.
  - Transient runtime state: `TowerEntity currentRuntimeInstance`.
  - Read-only UI properties should expose display name, icon, level, XP, next threshold, pending choices, applied upgrades, runtime instance, and deployment state.

- `UnitDeploymentController`
  - Add `BeginDeployment(UnitStateManager stateManager, string unitId)` without removing the existing prefab overloads.
  - Store pending `stateManager` and `unitId` while dragging.
  - For roster-managed deployment, instantiate the selected owned unit's prefab, apply stored upgrades before preview, and bind the runtime instance only after successful placement.

- `UIUnitItem`
  - Add optional `UnitStateManager` reference plus `unitId`.
  - If both are assigned, deploy through the roster manager.
  - Otherwise preserve existing `GameObject unitToDeploy` behavior.

## Implementation Changes
- Roster state
  - Store upgrades as `UpgradeSO` references only. Do not cache compiled stats or duplicate `TowerEntity.CompileFinalStats()`.
  - Use `unitId` for all public operations instead of list index.
  - Keep one deployed runtime instance per owned unit.
  - Treat empty or duplicate `unitId` entries as configuration errors reported through `Debug.LogWarning` or `Debug.LogError`.

- Progression
  - XP, level-up processing, pending choices, and applied upgrades are tracked independently per owned unit.
  - `AddExperience` ignores negative amounts, accumulates XP on the selected unit, and processes level-ups using that unit's `xpThresholds[level - 1]`.
  - If XP crosses multiple thresholds, generate one pending upgrade choice at a time for that unit. Extra XP remains stored; further level-ups resume after the current upgrade is selected.
  - Upgrade offers come from that unit's own upgrade pool, filtered to non-null upgrades not already applied, randomly choosing up to `choiceCount` unique options.
  - If the pool has fewer than three valid upgrades, offer fewer. If none are available, still advance the level and leave no pending choice.
  - `SelectUpgrade` validates the selected upgrade, applies it, clears pending choices, applies the upgrade immediately to the deployed runtime tower if present, then continues processing retained XP for that unit.

- Deployment integration
  - State-managed deployment is blocked when `unitId` is missing, unknown, has pending upgrade choices, already has a deployed runtime instance, or has no prefab.
  - Preview instances receive stored upgrades through `ApplyStateTo(unitId, tower)` before `PrepareForDeploymentPreview()` so preview/deployed stats match the roster state.
  - Successful placement calls `Deploy()` on the tower, then `BindRuntimeInstance(unitId, tower)`.
  - Canceled placement destroys only the preview and does not bind or mutate roster state.
  - `RetreatUnit(unitId)` destroys only that unit's current runtime tower GameObject and keeps XP/upgrades intact for future redeployment.

- Boundaries
  - Do not implement weapon override/augment behavior from `UpgradeSO`.
  - Do not add enemy kill tracking yet; future enemy/wave systems should call `AddExperience(unitId, amount)`.
  - Do not add save/load to disk yet; this first pass is serialized component/runtime state only.
  - Keep scripts in the global namespace and preserve/create Unity `.meta` files.

## Test Plan
- Run `dotnet build Assembly-CSharp.csproj` after script changes.
- Manual Unity checks:
  - Existing `UIUnitItem` using only `unitToDeploy` still deploys normally.
  - A `UIUnitItem` assigned a `UnitStateManager` and `unitId` deploys the matching owned unit prefab.
  - Two different owned units track separate XP, levels, upgrades, pending choices, and runtime instances.
  - The same owned unit cannot deploy twice at the same time.
  - Retreating one unit clears only that unit's runtime instance.
  - Applied upgrades persist after retreat and redeploy for the correct unit.
  - Selecting an upgrade while deployed immediately updates only that unit's runtime tower stats through `TowerEntity.AddUpgrade`.
  - Pending upgrade choices block deployment only for the affected unit.
  - Duplicate or empty `unitId` entries are reported as configuration errors.
  - Preview towers do not attack before placement; deployed towers still respect `SetupTime`.

## Assumptions
- Keep the class name `UnitStateManager`.
- Use nested `OwnedUnitState` for the first implementation.
- Use stable `unitId` strings instead of list indexes for UI/deployment calls.
- Each owned unit has at most one deployed runtime tower at a time.
- The initial roster is inspector-authored as about 10 entries, but the list size is not hard-coded.
- `xpThresholds[0]` means XP needed from level 1 to 2, `xpThresholds[1]` from level 2 to 3, and so on.
- Empty or exhausted XP thresholds mean the unit can keep accumulating XP but will not auto-level further.
- Upgrade choice randomness uses `UnityEngine.Random`.
- Unit prefabs used by `OwnedUnitState` are expected to be plain tower prefabs with no pre-applied runtime upgrades.
