# Project Agent Guide

This Unity project is a roster-managed tower combat prototype. Keep future agentic work aligned with the current roster-to-runtime bridge instead of introducing parallel state, upgrade, or combat pipelines.

## Project Shape

- Unity version: `6000.4.2f1`.
- Runtime scripts live under `Assets/Scripts`.
- Unit/tower scripts live under `Assets/Scripts/Units`.
- Shared stat enums live under `Assets/Scripts/Constants`.
- Scripts are currently in the global namespace. Do not add namespaces unless the project is intentionally migrated.
- Preserve Unity `.meta` files when creating or moving assets.

## Runtime Authority

`TowerEntity` is the center of deployed tower runtime behavior.

- Owns authored base stats plus applied `UpgradeSO` references.
- Compiles final runtime stats in `TowerEntity.CompileFinalStats()`.
- Owns the active primary `AttackBehaviour`, runtime override weapon instance, augment weapon instances, and active tower-level projectile modifier instances.
- Owns attack timing, setup delay, target retention, and primary attack ammo state.
- Owns selected state for deployed towers and is the only runtime owner that changes `UnitVision` range visualization visibility.
- Keeps range visualization visible when selected or while in deployment preview; deployment preview state and selected state are separate reasons.
- Pushes compiled `VisualRange` into `UnitVision.Range`.
- Resolves a stable runtime `unitId` from `UnitProgression` when roster-managed, otherwise generates a unique runtime ID.
- Raises `OnDeploy` only after activation has completed and a resolved unit ID exists.
- Raises `TowerModified` after deployed runtime refreshes caused by stat or upgrade changes.
- Provides `CalculateFinalStat(...)` for side-effect-free single-stat previews, such as roster deployment cost caching, without instantiating runtime weapon or modifier composition.

Do not create a second runtime stat/combat composition pipeline. Extend `TowerEntity.CompileFinalStats()` and related `TowerEntity` partials unless there is a deliberate refactor.

## Roster And Deployment

`UnitStateManager` is the persistent roster authority for player-owned units.

- Stores `OwnedUnitState` entries keyed by stable `unitId`.
- Owns shared XP thresholds plus per-unit display metadata, prefab reference, level, stored XP, pending-upgrade state, selected `MultiUpgradeSO` level state, and one selected `EvolutionSO`.
- Stores transient runtime bindings with `currentRuntimeInstance` and `currentRuntimeRoot`.
- Applies persistent upgrades and progression state into runtime towers through `ApplyStateTo(...)` after resolving each active multi-upgrade level and selected evolution to normal `UpgradeSO` leaves.
- Precompiles cached deployment costs for roster units through `TowerEntity.CalculateFinalStat(...)`.
- Finalizes managed deployments through `CompleteRuntimeDeployment(...)`.
- Applies selected multi-upgrade levels immediately to the deployed runtime tower when present by replacing the previous resolved `UpgradeSO` leaf with the new leaf.
- Applies the selected evolution immediately to the deployed runtime tower when present by adding the evolution's resolved `UpgradeSO` leaf.
- Refreshes cached deployment cost when selected upgrades change.
- Recalls deployed units while preserving persistent XP and upgrades.

`UnitDeploymentController` owns deployment preview and placement flow.

- Checks cached roster deployment cost before preview creation when available.
- Instantiates a preview root.
- Calls `TowerEntity.PrepareForDeploymentPreview()` before placement.
- Preview preparation enables the tower range preview; `TowerEntity.Deploy()` disables preview range visibility before runtime activation.
- Applies roster state during preview without running deployment-only activation work.
- Raises deployment preview started/ended events for UI state.
- Spends roster deployment currency only on successful final placement.
- Calls `UnitStateManager.CompleteRuntimeDeployment(...)` before `TowerEntity.Deploy()` for managed units.

Do not move combat stat math, runtime weapon composition, or projectile modifier composition into `UnitStateManager`. That remains runtime `TowerEntity` work.

## Stats

Current stats are defined in `EntityConstants.cs`.

- `ENTITY_STATS.GlobalDamage`: multiplier applied to weapon base damage.
- `ENTITY_STATS.AttackSpeed`: cooldown time between attack ticks, not attacks per second.
- `ENTITY_STATS.VisualRange`: vision sphere radius.
- `ENTITY_STATS.SetupTime`: delay from deployment until attacking is allowed.
- `ENTITY_STATS.AmmoEffectiveness`: multiplier applied to a primary weapon's `AttacksPerAmmo`.
- `ENTITY_STATS.AmmoUnits`: deployed tower ammo pool for finite primary weapons.
- `ENTITY_STATS.DeploymentCooldown`: time before a recalled unit can deploy again.
- `ENTITY_STATS.DeploymentCost`: currency cost paid when roster-managed units deploy.

Current modifier types:

- `STAT_TYPE.Add`: additive modifier.
- `STAT_TYPE.Mult`: multiplicative modifier.
- Serialized values are `STAT_TYPE.Mult = 0` and `STAT_TYPE.Add = 1`.

Current final stat formula:

```text
finalStat = (baseStat + totalAdd) * totalMult
```

When adding stats, also update `TowerEntity.GetDefaultStat()` with a safe default.

## Currency And Deployment UI

`CurrencyManager` is the scene-level player currency authority.

- Registers through `ServiceLocator`.
- Stores nonnegative integer currency initialized from `startingCurrency`.
- Exposes `CurrentCurrency`, `CanAfford(int)`, `TrySpend(int)`, and `AddCurrency(int)`.
- Debug/testing endpoints `DebugAddCurrency(int)` and `DebugRemoveCurrency(int)` reuse the same event-raising balance path.
- Raises `CurrencyChanged` through `UnitEventBus` whenever the balance changes.

Roster deployment economy currently applies only to managed units.

- `UnitStateManager.Precompile()` caches each managed unit's rounded-up `DeploymentCost`.
- `UnitDeploymentCostCompiled` is raised when cached cost is compiled or cleared.
- Missing cached cost falls back to instantiated preview cost lookup with a warning.
- Missing `CurrencyManager` skips currency enforcement with a warning.
- Direct prefab deployment remains free.

Deployment UI state is separate from deployment input eligibility.

- `UnitUIDeployment.CanBeginDeployment()` still blocks input while any preview is active.
- `UnitUIDeployment.CurrentState` can be `CannotDeploy`, `CanDeploy`, or `InDeployPreview`.
- The deployable indicator is visible for `CanDeploy` and `InDeployPreview`.
- `UnitUICost` displays cached cost for undeployed roster units and hides it while deployed or when no cached cost exists.
- `UICurrencyDisplayer` displays the current currency balance and refreshes from `CurrencyChanged`.

## Player Selection And Range Preview

`PlayerStateController` is the scene-level authority for player interaction state and selected tower identity.

- Uses `PlayerInput` click actions, resolved in order from `Click`, `Attack`, then a direct pointer press binding.
- Checks UI through `EventSystem.RaycastAll` only to block gameplay clicks when UI is under the pointer.
- Uses direct `Physics.RaycastAll` from the active selection camera for world tower selection.
- Sorts physics hits by distance and resolves exact `RaycastHit.collider` matches against `TowerSelectionTarget.SelectionCollider`.
- Uses `selectionLayers` for selectable tower body colliders, `selectionPassThroughLayers` for layers like `TowerVision`, and `selectionBlockingLayers` for geometry that should prevent click-through selection.
- Clears selection on empty world clicks or blocking/non-selectable physics hits while selection is allowed.

`TowerSelectionTarget` is a data/event bridge, not an input owner.

- Stores the selectable `TowerEntity` and directly assigned selection `Collider`.
- Does not implement pointer handlers and does not call `UnitVision.SetVisualizationVisible(...)`.
- Relays `TowerEntity.Selected` and `TowerEntity.Deselected` through optional UnityEvents for target-local visuals.

Do not add a second pointer or event-system physics selection path unless the selection architecture is deliberately refactored.

## Combat Loop

`TowerEntity` combat timing is currently:

- Deployment sets `activeAfterTime = Time.time + SetupTime`.
- `AttackSpeed` is used as cooldown seconds between attack ticks.
- The current target is retained until it becomes null, inactive, or leaves `UnitVision`.
- The tower reacquires with `UnitVision.GetFrontMostValidTarget()`.
- One attack tick fires the primary attack behaviour first, then augment attack behaviours in order while the target remains valid.
- Only the primary weapon can consume tower ammo.
- A finite primary weapon cannot start a new attack tick when `CurrentAmmoUnits == 0`.

Debug-only target rescanning currently exists through `activelyPollEnemies` and `enemyPollPeriod` on `TowerEntity`.

## Upgrades

`UpgradeSO` is the tower-facing upgrade leaf asset.

- Create assets through `Create > TBTD > Upgrade`.
- Stores display name, description, optional icon, stat effects, weapon upgrade mode, weapon behaviour prefab, and projectile modifier prefabs.
- Runtime weapon override, augment, stat, and projectile-modifier composition is applied by `TowerEntity.CompileFinalStats()`.
- `TowerEntity` only stores and compiles `UpgradeSO` references.

`MultiUpgradeSO` is the roster/offer-facing upgrade-line asset.

- Create assets through `Create > TBTD > Multi Upgrade`.
- Stores an ordered list of `UpgradeSO` level assets; levels are 1-based by list order and can have any positive count.
- `UnitStateManager.OwnedUnitState` stores selected `MultiUpgradeSO` line state plus the current selected level.
- Only one resolved `UpgradeSO` level from a `MultiUpgradeSO` can be active on a unit at a time.
- Upgrade UI displays the next resolved `UpgradeSO` level's name, description, and icon.

`EvolutionSO` is the roster/offer-facing weapon-evolution asset.

- Create assets through `Create > TBTD > Evolution`.
- Stores one resolved `UpgradeSO` leaf plus prerequisite `MultiUpgradeSO` level requirements.
- `UnitStateManager.OwnedUnitState` stores at most one selected `EvolutionSO`.
- Evolutions are eligible only when every prerequisite line has reached the required level and the unit has not already evolved.
- Runtime composition still receives only the resolved `UpgradeSO`; `TowerEntity` does not store `EvolutionSO`.

Weapon upgrade fields:

- `WEAPON_UPGRADE_TYPE.None`: no weapon change.
- `WEAPON_UPGRADE_TYPE.Override`: latest valid override replaces the primary attack behaviour at runtime.
- `WEAPON_UPGRADE_TYPE.Augment`: each valid augment is instantiated as an extra runtime weapon that fires on the same attack tick.

Do not add another runtime upgrade/effect pipeline. Extend `UpgradeSO` for tower-facing effects, `MultiUpgradeSO` for roster offer/level grouping, and `TowerEntity.CompileFinalStats()` for runtime composition unless there is a deliberate refactor.

## Upgrade Flow

Upgrade selection is event-bus driven.

- `UnitProgression` raises `UnitUpgradeThresholdReached` when runtime XP reaches the current threshold.
- `UpgradesManager` listens, marks the roster unit pending through `UnitStateManager.TryBeginUpgradeSelection(unitId)`, and builds an offer from its shared `MultiUpgradeSO` `upgradePool` plus eligible entries in its `EvolutionSO` `evolutionPool`.
- `UpgradeSelectionUI` listens for `UnitUpgradeChoicesOffered`, instantiates `UpgradeChoiceItem` entries, and raises `UnitUpgradeChoiceRequested` when the player selects one.
- `UpgradesManager` validates the pending offer, calls `UnitStateManager.RecordSelectedUpgrade`, and raises `UnitUpgradeSelected`.
- `UnitStateManager.RecordSelectedUpgrade` clears pending state, advances unit level, advances the selected multi-upgrade line or records the selected evolution, applies the resolved `UpgradeSO` leaf immediately to the deployed tower if present, and refreshes runtime progression.
- The UI hides only after `UnitUpgradeSelected` confirms the active unit's choice.

Current offer rules:

- `UpgradesManager.upgradePool` is a shared list of `MultiUpgradeSO` lines across all units.
- `UpgradesManager.evolutionPool` is a shared list of `EvolutionSO` assets across all units.
- Null multi-upgrades, duplicate asset references, invalid next-level assets, and maxed multi-upgrades are filtered out.
- Already-selected non-max multi-upgrades can be offered again and resolve to their next level.
- Null evolutions, duplicate evolution references, invalid resolved upgrades, unmet prerequisites, and all evolutions for already-evolved units are filtered out.
- Offers contain up to `upgradeChoiceCount` unique random choices from the combined multi-upgrade/evolution candidate list.
- If no valid choices remain, a null selection is recorded so level progression can continue.

## Progression

`UnitProgression` is the runtime XP component for one deployed unit instance.

- Mirrors `unitId`, level, stored XP, next threshold, and pending-upgrade state from the roster.
- Raises `UnitExperienceChanged` whenever runtime XP changes.
- Raises `UnitUpgradeThresholdReached` when XP reaches the next threshold and no upgrade is already pending.
- Re-evaluates thresholds when reinitialized after a selection.

Current behavior is one pending upgrade choice at a time. Do not assume multi-threshold catch-up or queued offers exist.

## Vision And Targeting

`UnitVision` owns target discovery.

- Requires a `SphereCollider`.
- Forces the collider to trigger mode.
- Syncs collider radius from `Range`.
- Tracks valid targets through `OnTriggerEnter` and `OnTriggerExit`.
- Supports one-shot overlap rescans through `ScanForTargetsOnce()`.
- Targetability is currently layer-based through `targetLayers`.
- Uses `ColliderTargetUtility.GetTargetTransform(...)` so rigidbody-rooted targets resolve consistently.
- Invalid targets are pruned when null or inactive.
- Range visualization is exposed through `SetVisualizationVisible(...)`, but runtime callers should go through `TowerEntity` selected/preview state instead of manipulating it directly.

There is still no faction/team system or targeting priority beyond first valid target. Do not assume targetability implies hostility beyond layer configuration.

## Attack Behaviours

`AttackBehaviour` is the abstract base class for weapons.

- It is a `MonoBehaviour`.
- It owns `baseDamage`, `attacksPerAmmo`, `infiniteAmmo`, and the shared final `aimModifierVector`.
- Public attack entrypoint: `Attack(Transform target, float damageMultiplier)`.
- Derived classes implement `ExecuteAttack(Transform target, float damage)` and return whether a real attack was dispatched.
- `ConfigureRuntime(...)` receives tower/root context, active tower hit modifiers, compiled projectile modifier prefabs, and whether this weapon consumes tower ammo.
- `UsesFiniteAmmo` applies only to the primary weapon path configured with `usesTowerAmmo = true`.
- Use `TryApplyDamage()` to apply damage consistently.
- Non-projectile attacks that still need upgrade-authored hit behavior should use `TryApplyDamage()` or `DispatchHitModifiers(...)`.

Damage application order currently lives in `CombatDamageUtility`:

- First tries `IAttackContextDamageable.TakeDamage(float, AttackHitContext)` on the target or its parents.
- Then tries `IDamageable.TakeDamage(float)` on the target or its parents.
- Falls back to `SendMessage("TakeDamage", damage, DontRequireReceiver)`.

Existing implementations include direct damage, debug beam/hitscan, line-FX direct Sniper, piercing Laser, tick-skip-ramping Machine Gun, Aura, Grenade Launcher, Shotgun, and projectile weapons such as `BaseGunBehaviour` and `TestGunAttackBehaviour`.

`LineAttackFXComponent` is the shared visual-only line effect for Laser and Sniper. It draws the full line immediately from attack origin to hit/end point, then eases line width from thick to thin before hiding. Keep damage and hit modifier dispatch in the attack behaviour, not in the FX component.

When adding a new weapon, derive from `AttackBehaviour` and keep attack-specific logic there. Do not put weapon-specific behavior directly into `TowerEntity`.

## Projectile Modifiers And Projectiles

`ProjectileModifierBehaviour` is the authored base for hit hooks and projectile lifecycle behavior.

- Tower-level modifier instances are runtime children of `TowerEntity` and service direct/hitscan hit hooks.
- Projectile attacks receive compiled modifier prefabs from `AttackBehaviour.ProjectileModifiers`.
- `BaseProjectile.Initialize(...)` instantiates per-projectile modifier copies so in-flight projectiles keep fired-time behavior.
- Modifier hooks cover projectile initialization, tick, hit, and expiry.
- Direct and hitscan attacks invoke only the hit hook with `ProjectileModifierContext.Projectile == null`.
- Beam, chained, area, or other non-projectile attacks should dispatch hit hooks once per resolved gameplay hit when upgrade-authored effects should apply.

`ProjectilePropertiesModifierBehaviour` currently covers common projectile tuning such as lifetime, destroy-on-hit, straight projectile speed, and collider-size changes.

Do not reintroduce parallel modifier systems such as a separate on-hit-effect base type.

## Damage And Health

`HealthComponent` is now the basic health implementation.

- Implements both `IAttackContextDamageable` and `IDamageable`.
- Supports health, optional shield, last-hit context capture, and configurable death behavior.
- Exposes `OnDeath` for death-driven behaviors.

`EnemyEntity` currently:

- Initializes `HealthComponent` and `EnemyMover`.
- Awards XP to the attacker's `UnitProgression` on death when hit context is available.
- Can deal damage to a configured base target on reach-end.

Do not state that the project has no health implementation. The current gap is broader combat ownership/faction logic, not health itself.

## Unity Setup Expectations

A functional tower GameObject should have:

- `TowerEntity`.
- One concrete `AttackBehaviour`.
- `UnitVision` on the same object or a child.
- A `SphereCollider` on the `UnitVision` object.
- Target layers configured on `UnitVision`.
- A `TowerSelectionTarget` with its `TowerEntity` and exact selection collider assigned when the tower should be player-selectable.
- A selectable tower-body layer such as `TowerUnit` on the assigned selection collider, and a pass-through layer such as `TowerVision` on vision trigger colliders when using `PlayerStateController`.

Roster-managed deployed units also expect:

- A stable roster `unitId` managed by `UnitStateManager`.
- A `UnitProgression` component on the runtime root or tower after deployment.

Projectile weapons additionally expect:

- A projectile prefab with `BaseProjectile` or a derived projectile component.
- Correct collider setup for trigger hits.

Targets that should take damage should have:

- A collider that enters the vision trigger or projectile trigger.
- A layer included by the relevant vision/projectile layer masks.
- `IAttackContextDamageable`, `IDamageable`, or a `TakeDamage(float)` method.

## Coding Guidelines For Future Agents

- Prefer extending current components over adding duplicate systems.
- Keep Unity serialized fields private with public read-only accessors where needed.
- Keep implementations compile-ready after each change.
- Run `dotnet build Assembly-CSharp.csproj` after script changes when possible.
- If `--no-restore` fails because `Temp/obj/.../project.assets.json` is missing, run build without `--no-restore`.
- Avoid changing generated `.csproj` files unless necessary for immediate local compilation; Unity may regenerate them.
- Preserve existing `.meta` files, but do not create or edit `.meta` files manually in this project. Let Unity generate new `.meta` files itself when the editor regains focus.
- Do not use destructive git commands.

## Known Intentional Gaps

- No faction/team/allegiance model yet.
- No targeting priority beyond first valid target.
- Upgrade offers are built from shared multi-upgrade and evolution pools with simple duplicate/max-level/prerequisite filtering.
- Runtime-generated unit IDs exist for unmanaged towers, but broader persistence/save-load infrastructure is not implemented here.
- Currency rewards, recall refunds, and save/load persistence for currency are not implemented yet.
