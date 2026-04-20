# Project Agent Guide

This Unity project is currently a small tower-combat prototype. Keep future agentic work aligned with the systems below instead of introducing parallel architectures.

## Project Shape

- Unity version: `6000.4.2f1`.
- Runtime scripts live under `Assets/Scripts`.
- Unit/tower scripts live under `Assets/Scripts/Units`.
- Shared stat enums live under `Assets/Scripts/Constants`.
- Scripts are currently in the global namespace. Do not add namespaces unless the project is intentionally migrated.
- Preserve Unity `.meta` files when creating or moving assets.

## Core Combat Model

`TowerEntity` is the center of tower runtime behavior.

- Owns base tower stats.
- Owns assigned `UpgradeSO` assets.
- Compiles base stats plus upgrades into cached final stats.
- Selects a target from `UnitVision`.
- Calls the active primary `AttackBehaviour` and any runtime augment weapons.
- Keeps the current target until it leaves vision or becomes invalid.
- Uses `AttackSpeed` as cooldown time between attacks, not attacks per second.
- Uses `SetupTime` as initial delay before attacking.
- Uses `GlobalDamage` as the damage multiplier passed into `AttackBehaviour`.
- Pushes final `VisualRange` into `UnitVision.Range`.

Do not create a separate tower stat pipeline. Extend `TowerEntity.CompileFinalStats()` unless there is a deliberate refactor.

## Stats

Current stats are defined in `EntityConstants.cs`.

- `ENTITY_STATS.GlobalDamage`: multiplier applied to weapon base damage.
- `ENTITY_STATS.AttackSpeed`: cooldown time between attacks.
- `ENTITY_STATS.VisualRange`: vision sphere radius.
- `ENTITY_STATS.SetupTime`: delay before the tower can attack after spawn/deploy.

Current modifier types:

- `STAT_TYPE.Add`: additive modifier.
- `STAT_TYPE.Mult`: multiplicative modifier.
- Serialized values are `STAT_TYPE.Mult = 0` and `STAT_TYPE.Add = 1`.

Current final stat formula:

```text
finalStat = (baseStat + totalAdd) * totalMult
```

When adding stats, also update `TowerEntity.GetDefaultStat()` with a safe default.

## Upgrades

`UpgradeSO` is the only current upgrade asset type.

- Create assets through `Create > TBTD > Upgrade`.
- Stores display name and description.
- Stores a list of stat effects.
- Can hold a weapon behavior reference.
- Can hold projectile modifier prefabs.
- Weapon override and augment are applied by `TowerEntity.CompileFinalStats()`.

Weapon upgrade fields:

- `WEAPON_UPGRADE_TYPE.None`: no weapon change.
- `WEAPON_UPGRADE_TYPE.Override`: latest valid override replaces the primary attack behavior.
- `WEAPON_UPGRADE_TYPE.Augment`: each valid augment is instantiated as an extra weapon that fires with the primary attack tick.
- `weaponBehaviourPrefab`: references an `AttackBehaviour` implementation such as `DirectAttackBehaviour`, `DebugAttackBehaviour`, or a projectile weapon.

Do not add a second upgrade asset model. Extend `UpgradeSO` and `TowerEntity.CompileFinalStats()` unless there is a deliberate refactor.

## Vision And Targeting

`UnitVision` owns target discovery.

- Requires a `SphereCollider`.
- Forces the collider to trigger mode.
- Syncs collider radius from `Range`.
- Tracks valid targets through `OnTriggerEnter` and `OnTriggerExit`.
- Targetability is currently layer-based through `targetLayers`.
- If the collider has an attached `Rigidbody`, the target transform is the rigidbody transform.
- Otherwise, the target transform is the collider transform.
- Invalid targets are pruned when null or inactive.

There is no faction, enemy component, tag check, or health requirement yet. Do not assume targetability implies damageability.

## Attack Behaviours

`AttackBehaviour` is the abstract base class for weapons.

- It is a `MonoBehaviour`.
- It owns `baseDamage`.
- Public attack entrypoint: `Attack(Transform target, float damageMultiplier)`.
- Derived classes implement `ExecuteAttack(Transform target, float damage)`.
- Use `TryApplyDamage()` to apply damage consistently.
- `ConfigureRuntime(...)` receives tower/root context, runtime hit modifiers, and projectile modifier prefabs.
- New non-projectile attacks such as direct damage, laser, beam, or hitscan weapons must consider how `ProjectileModifierBehaviour` hooks participate. Use `TryApplyDamage()` for normal resolved hits, or call `DispatchHitModifiers(...)` when a custom damage path still needs upgrade-authored hit behavior.

Damage application order:

- First tries `IAttackContextDamageable.TakeDamage(float, AttackHitContext)` on the target or its parents.
- Then tries `IDamageable.TakeDamage(float)` on the target or its parents.
- Falls back to `SendMessage("TakeDamage", damage, DontRequireReceiver)`.
- Dispatches active `ProjectileModifierBehaviour.ApplyProjectileHit(...)` hooks after direct/hitscan damage resolves.

Existing implementations:

- `DirectAttackBehaviour`: immediately applies damage.
- `DebugAttackBehaviour`: applies damage and draws a temporary laser beam using `LineRenderer`.

When adding a new weapon, derive from `AttackBehaviour` and keep attack-specific presentation/logic there. Do not put weapon-specific behavior directly into `TowerEntity`.

## Projectile Modifiers

`ProjectileModifierBehaviour` is the single authored upgrade modifier base for hit hooks and projectile lifecycle behavior.

- Direct and hitscan attacks invoke only the hit hook with `ProjectileModifierContext.Projectile == null`.
- Beam, laser, chained, area, or multi-hit non-projectile attacks should dispatch hit hooks once per resolved gameplay hit when upgrade behavior should apply. Do not dispatch hit hooks for visual-only ticks.
- Projectile attacks pass compiled modifier prefabs into `BaseProjectile.Initialize(...)`.
- `BaseProjectile` instantiates modifier copies as projectile children so in-flight projectiles keep fired-time behavior.
- Modifier hooks cover projectile initialization, tick, hit, and expiry.
- `ProjectilePropertiesModifierBehaviour` covers common lifetime, destroy-on-hit, straight projectile speed, and collider-size changes.
- Keep base damage balance on `AttackBehaviour.BaseDamage` plus `ENTITY_STATS.GlobalDamage`; use projectile modifiers for bullet behavior changes.

Do not reintroduce `OnHitEffectBehaviour`; derive new hit effects and projectile behavior changes from `ProjectileModifierBehaviour`.

## Debug Laser Behaviour

`DebugAttackBehaviour`:

- Requires a `LineRenderer`.
- Uses optional `firePoint` as the beam start.
- Falls back to the tower transform position.
- Tracks the target position while the beam is active.
- Disables the line renderer after `beamDuration`.
- Restarts cleanly if another shot happens before the previous beam ends.

## Unity Setup Expectations

A functional tower GameObject should have:

- `TowerEntity`.
- One concrete `AttackBehaviour`, such as `DirectAttackBehaviour` or `DebugAttackBehaviour`.
- `UnitVision` on the same object or a child.
- A `SphereCollider` on the `UnitVision` object.
- Target layers configured on `UnitVision`.

Targets should have:

- A collider that enters the vision trigger.
- A layer included by `UnitVision.targetLayers`.
- Optionally `IDamageable` or a `TakeDamage(float)` method if damage should have an effect.

## Upgrade UI

Upgrade selection is event-bus driven.

- `UpgradesManager` generates pending offers when `UnitUpgradeThresholdReached` fires.
- `UpgradeSelectionUI` listens for offered choices and creates `UpgradeChoiceItem` entries.
- `UpgradeChoiceItem` uses TextMeshPro display fields and a `Button` for selection.
- UI selection raises `UnitUpgradeChoiceRequested`.
- `UpgradesManager` applies the pending selected upgrade and raises `UnitUpgradeSelected`.
- The UI hides only after the selected event confirms the active unit's choice.

## Coding Guidelines For Future Agents

- Prefer extending current components over adding duplicate systems.
- Keep Unity serialized fields private with public read-only accessors where needed.
- Keep implementations compile-ready after each change.
- Run `dotnet build Assembly-CSharp.csproj` after script changes when possible.
- If `--no-restore` fails because `Temp/obj/.../project.assets.json` is missing, run build without `--no-restore`.
- Avoid changing generated `.csproj` files unless necessary for immediate local compilation; Unity may regenerate them.
- Preserve existing `.meta` files and create `.meta` files for new Unity assets/scripts when working outside the editor.
- Do not use destructive git commands.

## Known Intentional Gaps

- No enemy/faction/team system yet.
- No health component implementation yet.
- No targeting priority beyond first valid target.
