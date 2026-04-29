# Upgrade System Current State

## Role
The upgrade system is split between persistent roster selection and runtime tower composition.

- `UpgradeSO` is the authored upgrade asset.
- `UpgradesManager` owns the shared upgrade pool, offer count, pending upgrade offers, and selection.
- `UnitStateManager` stores selected upgrade assets per owned unit.
- `TowerEntity` compiles selected upgrades into runtime stats, active weapon behaviours, and projectile modifier prefabs.
- `UnitStateManager` uses `TowerEntity.CalculateFinalStat(...)` to cache deployment cost for UI and deployment preflight without building runtime weapon/modifier composition.

The normal upgrade flow is append-only. Selected upgrades are recorded on `OwnedUnitState.AppliedUpgrades`; later upgrades do not delete earlier selections.

## UpgradeSO
`UpgradeSO` assets are created through `Create > TBTD > Upgrade`.

An upgrade can combine:
- Display data: `upgradeName` and `description`.
- Stat effects: a list of `StatEffect` entries using `ENTITY_STATS` plus `STAT_TYPE.Add` or `STAT_TYPE.Mult`.
- Weapon fields: `weaponUpgradeType` plus `weaponBehaviourPrefab`.
- Projectile modifiers: a list of `ProjectileModifierBehaviour` prefab/component references used for direct-hit effects and projectile lifecycle behavior.

Current serialized stat modifier values:
- `STAT_TYPE.Mult = 0`
- `STAT_TYPE.Add = 1`
- Existing upgrade assets with `type: 0` are multiplicative modifiers.
- Final stat formula is `finalStat = (baseStat + totalAdd) * totalMult`.

Current weapon behavior:
- `WEAPON_UPGRADE_TYPE.None` does not affect the active weapon.
- `WEAPON_UPGRADE_TYPE.Override` is implemented. The latest applied override upgrade wins.
- `WEAPON_UPGRADE_TYPE.Augment` instantiates extra runtime weapons that fire alongside the active primary weapon.

Current projectile modifier behavior:
- Every non-null projectile modifier prefab from every applied upgrade is additive.
- `TowerEntity` instantiates modifier prefabs as runtime children for direct and hitscan-style hit hooks.
- `TowerEntity` also passes compiled modifier prefabs into active weapons.
- Direct and hitscan-style attacks dispatch only the hit hook with no projectile instance in the context.
- Projectile weapons pass modifier prefabs into `BaseProjectile.Initialize`.
- `BaseProjectile` instantiates modifier instances as projectile children so in-flight projectiles keep their fired-time behavior and full projectile lifecycle hooks.

## Offer And Selection Flow
`UpgradesManager` listens for `UnitUpgradeThresholdReached` events from `UnitEventBus`.

Current offer configuration lives on `UpgradesManager`:
- `upgradePool`: one shared pool used by all units.
- `upgradeChoiceCount`: one shared number of choices to offer when enough valid candidates exist.

When a threshold is reached:
- Ignore missing unit IDs, missing managers, units that already have a pending offer, unknown units, or units that cannot begin upgrade selection.
- Build candidates from the manager's shared `upgradePool`.
- Filter out null upgrades, duplicates in the candidate list, and upgrades already present in `OwnedUnitState.AppliedUpgrades`.
- Randomly offer up to `upgradeChoiceCount` unique choices.
- Raise `UnitUpgradeChoicesOffered` when choices exist.
- If no choices exist, record a null selection so the unit can advance and clear pending state.

Selection API:
- `SelectUpgrade(unitId, int choiceIndex)` selects by UI index.
- `SelectUpgrade(unitId, UpgradeSO upgrade)` selects by asset reference.
- `UnitUpgradeChoiceRequestedEvent` is the decoupled UI request path; `UpgradesManager` handles it by calling the index-based selection API.
- Selection is only valid while a pending offer exists and the selected upgrade belongs to that offer.

On successful selection, `UpgradesManager` calls `UnitStateManager.RecordSelectedUpgrade`, then raises `UnitUpgradeSelected`.

## Runtime Composition
`TowerEntity.CompileFinalStats()` is the single runtime compilation point for tower upgrades.

Compilation outputs:
- Final stats using `finalStat = (baseStat + totalAdd) * totalMult`.
- The latest `Override` weapon prefab from the applied upgrade list.
- The ordered additive list of `Augment` weapon prefabs from all applied upgrades.
- The ordered additive list of projectile modifier prefabs from all applied upgrades.

Runtime application:
- The serialized `attackBehaviour` is preserved as the default weapon.
- If at least one override upgrade exists, the latest override prefab is instantiated as the runtime active weapon.
- If no override exists, the default attack behaviour remains active.
- Augment weapon prefabs are instantiated as runtime children and fire on the same target/tick as the active primary weapon.
- Projectile modifier prefabs are instantiated as runtime children for direct/hitscan hit hooks and exposed to every active weapon for projectile initialization.
- Appending a new upgrade only adds new modifier instances when the current modifier list is a prefix of the compiled list.

Deployment price changes should use `ENTITY_STATS.DeploymentCost` stat effects. Cached roster deployment cost is refreshed on startup and after selected upgrades.

Important constraints:
- `TowerEntity.AddUpgrade` ignores null or duplicate upgrade assets.
- `RemoveUpgrade` exists for runtime/editor utility, but the normal roster flow only adds upgrades.
- `UnitStateManager` stores upgrade references and cached deployment cost only; it does not inspect stat effects or construct runtime weapon/modifier instances.

## Projectile Modifier Pipeline
`ProjectileModifierBehaviour` is the base class for C# authored hit modifiers and projectile modifiers.

Modifier scripts can override initialization, tick, hit, and expiry hooks. Direct and hitscan-style attacks only invoke the hit hook with `ProjectileModifierContext.Projectile == null`; projectile attacks instantiate a copied modifier set on each projectile and can invoke the full lifecycle. `ProjectilePropertiesModifierBehaviour` covers common lifetime, destroy-on-hit, straight projectile speed, and collider-size changes.

Projectile initialization still supports the old `Initialize(float damage, Transform owner)` overload, but upgraded projectile weapons should call the overload with tower, attack behaviour, and projectile modifier list.

Damage scaling should normally be authored as `ENTITY_STATS.GlobalDamage` stat effects. Projectile damage comes from the active weapon's `AttackBehaviour.BaseDamage` after tower damage multipliers are applied, so projectile modifiers should be reserved for bullet behavior rather than base damage balance.

## Extension Rules
- Add new stats in `EntityConstants.cs` and update `TowerEntity.GetDefaultStat()`.
- Add new attack behaviours by deriving from `AttackBehaviour`; do not put weapon-specific logic in `TowerEntity`.
- Add new hit modifiers or projectile modifiers by deriving from `ProjectileModifierBehaviour`; do not hard-code modifier-specific behavior into `TowerEntity`, `AttackBehaviour`, or `BaseProjectile`.
- Keep offer pools and complex offer rules out of `UnitStateManager`. If rarity, prerequisites, tags, or synergies grow, extract offer generation behind `UpgradesManager`.
- Future upgrade gating metadata should live on `UpgradeSO`, such as prerequisite upgrades, class/tag gates, or level-specific offer hints. Do not implement those rules until the design is ready.
- Keep scripts in the global namespace unless the project is intentionally migrated.

## Verification Checklist
- `dotnet build Assembly-CSharp.csproj` after script changes.
- `UnitStateManager.OwnedUnitState` does not expose per-unit offer pool or offer count.
- `UpgradesManager` exposes one shared upgrade pool and one shared offer count.
- Stat-only upgrades still update final tower stats and vision range.
- `DeploymentCost` upgrades update cached roster cost and deployment UI.
- Multiple override upgrades keep all selections recorded, with the latest override firing.
- Augment upgrades fire alongside the active primary weapon.
- Augments still fire after a later override upgrade.
- Earlier stat and modifier upgrades remain active after a later weapon override.
- Direct, debug/hitscan, and projectile attacks all trigger hit modifiers.
- Projectile modifiers affect newly fired projectiles without changing existing in-flight projectiles.
- Applied upgrades persist through recall and redeploy.
