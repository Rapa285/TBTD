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
- Pushes compiled `VisualRange` into `UnitVision.Range`.
- Resolves a stable runtime `unitId` from `UnitProgression` when roster-managed, otherwise generates a unique runtime ID.
- Raises `OnDeploy` only after activation has completed and a resolved unit ID exists.
- Raises `TowerModified` after deployed runtime refreshes caused by stat or upgrade changes.

Do not create a second runtime stat/combat composition pipeline. Extend `TowerEntity.CompileFinalStats()` and related `TowerEntity` partials unless there is a deliberate refactor.

## Roster And Deployment

`UnitStateManager` is the persistent roster authority for player-owned units.

- Stores `OwnedUnitState` entries keyed by stable `unitId`.
- Owns display metadata, prefab reference, XP thresholds, level, stored XP, pending-upgrade state, and selected `UpgradeSO` list.
- Stores transient runtime bindings with `currentRuntimeInstance` and `currentRuntimeRoot`.
- Applies persistent upgrades and progression state into runtime towers through `ApplyStateTo(...)`.
- Finalizes managed deployments through `CompleteRuntimeDeployment(...)`.
- Applies selected upgrades immediately to the deployed runtime tower when present.
- Recalls deployed units while preserving persistent XP and upgrades.

`UnitDeploymentController` owns deployment preview and placement flow.

- Instantiates a preview root.
- Calls `TowerEntity.PrepareForDeploymentPreview()` before placement.
- Applies roster state during preview without running deployment-only activation work.
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

Current modifier types:

- `STAT_TYPE.Add`: additive modifier.
- `STAT_TYPE.Mult`: multiplicative modifier.
- Serialized values are `STAT_TYPE.Mult = 0` and `STAT_TYPE.Add = 1`.

Current final stat formula:

```text
finalStat = (baseStat + totalAdd) * totalMult
```

When adding stats, also update `TowerEntity.GetDefaultStat()` with a safe default.

## Combat Loop

`TowerEntity` combat timing is currently:

- Deployment sets `activeAfterTime = Time.time + SetupTime`.
- `AttackSpeed` is used as cooldown seconds between attack ticks.
- The current target is retained until it becomes null, inactive, or leaves `UnitVision`.
- The tower reacquires with `UnitVision.GetFirstValidTarget()`.
- One attack tick fires the primary attack behaviour first, then augment attack behaviours in order while the target remains valid.
- Only the primary weapon can consume tower ammo.
- A finite primary weapon cannot start a new attack tick when `CurrentAmmoUnits == 0`.

Debug-only target rescanning currently exists through `activelyPollEnemies` and `enemyPollPeriod` on `TowerEntity`.

## Upgrades

`UpgradeSO` is the single authored upgrade asset type.

- Create assets through `Create > TBTD > Upgrade`.
- Stores display name, description, optional icon, stat effects, weapon upgrade mode, weapon behaviour prefab, and projectile modifier prefabs.
- Selected upgrade references are stored persistently on `UnitStateManager.OwnedUnitState`.
- Runtime weapon override, augment, stat, and projectile-modifier composition is applied by `TowerEntity.CompileFinalStats()`.

Weapon upgrade fields:

- `WEAPON_UPGRADE_TYPE.None`: no weapon change.
- `WEAPON_UPGRADE_TYPE.Override`: latest valid override replaces the primary attack behaviour at runtime.
- `WEAPON_UPGRADE_TYPE.Augment`: each valid augment is instantiated as an extra runtime weapon that fires on the same attack tick.

Do not add a second upgrade asset model. Extend `UpgradeSO` and `TowerEntity.CompileFinalStats()` unless there is a deliberate refactor.

## Upgrade Flow

Upgrade selection is event-bus driven.

- `UnitProgression` raises `UnitUpgradeThresholdReached` when runtime XP reaches the current threshold.
- `UpgradesManager` listens, marks the roster unit pending through `UnitStateManager.TryBeginUpgradeSelection(unitId)`, and builds an offer from its shared `upgradePool`.
- `UpgradeSelectionUI` listens for `UnitUpgradeChoicesOffered`, instantiates `UpgradeChoiceItem` entries, and raises `UnitUpgradeChoiceRequested` when the player selects one.
- `UpgradesManager` validates the pending offer, calls `UnitStateManager.RecordSelectedUpgrade`, and raises `UnitUpgradeSelected`.
- `UnitStateManager.RecordSelectedUpgrade` clears pending state, advances level, records the selected upgrade when non-null, applies it immediately to the deployed tower if present, and refreshes runtime progression.
- The UI hides only after `UnitUpgradeSelected` confirms the active unit's choice.

Current offer rules:

- `UpgradesManager.upgradePool` is shared across all units.
- Null upgrades, duplicate asset references, and already-applied upgrades are filtered out.
- Offers contain up to `upgradeChoiceCount` unique random choices.
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

Existing implementations include direct damage, debug beam/hitscan, and projectile weapons such as `TestGunAttackBehaviour`.

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
- Upgrade offers are still built from one shared pool with simple duplicate/already-owned filtering.
- Runtime-generated unit IDs exist for unmanaged towers, but broader persistence/save-load infrastructure is not implemented here.
