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
- Calls the assigned `AttackBehaviour`.
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
- Can hold a future weapon behavior reference.
- Weapon override and augment are prepared but intentionally not applied yet.

Weapon upgrade fields:

- `WEAPON_UPGRADE_TYPE.None`: no weapon change.
- `WEAPON_UPGRADE_TYPE.Override`: intended future replacement of current attack behavior.
- `WEAPON_UPGRADE_TYPE.Augment`: intended future composition/addition to current attack behavior.
- `weaponBehaviourPrefab`: intended to reference an `AttackBehaviour` implementation such as `DirectAttackBehaviour` or `DebugAttackBehaviour`.

Do not implement weapon override/augment behavior unless explicitly requested. If implementing it later, use the existing `UpgradeSO` fields instead of adding a new upgrade asset model.

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

Damage application order:

- First tries `IDamageable.TakeDamage(float)` on the target or its parents.
- Falls back to `SendMessage("TakeDamage", damage, DontRequireReceiver)`.

Existing implementations:

- `DirectAttackBehaviour`: immediately applies damage.
- `DebugAttackBehaviour`: applies damage and draws a temporary laser beam using `LineRenderer`.

When adding a new weapon, derive from `AttackBehaviour` and keep attack-specific presentation/logic there. Do not put weapon-specific behavior directly into `TowerEntity`.

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
- No projectile system yet.
- No weapon override/augment application yet.
- No targeting priority beyond first valid target.
- No upgrade UI or upgrade selection flow yet.
