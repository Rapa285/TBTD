# Upgrade System Current State

## Role
The upgrade system is split between persistent roster selection and runtime tower composition.

- `UpgradeSO` is the authored upgrade asset.
- `UpgradesManager` owns the shared upgrade pool, offer count, pending upgrade offers, and selection.
- `UnitStateManager` stores selected upgrade assets per owned unit.
- `TowerEntity` compiles selected upgrades into runtime stats, active weapon behaviour, and active on-hit effects.

The normal upgrade flow is append-only. Selected upgrades are recorded on `OwnedUnitState.AppliedUpgrades`; later upgrades do not delete earlier selections.

## UpgradeSO
`UpgradeSO` assets are created through `Create > TBTD > Upgrade`.

An upgrade can combine:
- Display data: `upgradeName` and `description`.
- Stat effects: a list of `StatEffect` entries using `ENTITY_STATS` plus `STAT_TYPE.Add` or `STAT_TYPE.Mult`.
- Weapon fields: `weaponUpgradeType` plus `weaponBehaviourPrefab`.
- On-hit effects: a list of `OnHitEffectBehaviour` prefab/component references.

Current weapon behavior:
- `WEAPON_UPGRADE_TYPE.None` does not affect the active weapon.
- `WEAPON_UPGRADE_TYPE.Override` is implemented. The latest applied override upgrade wins.
- `WEAPON_UPGRADE_TYPE.Augment` exists in the enum but is not implemented as weapon composition.

Current on-hit behavior:
- Every non-null effect prefab from every applied upgrade is additive.
- Effects are instantiated as runtime children of the tower by `TowerEntity`.
- Effects receive `AttackHitContext` when a direct, hitscan-style, or projectile hit resolves.

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
- Selection is only valid while a pending offer exists and the selected upgrade belongs to that offer.

On successful selection, `UpgradesManager` calls `UnitStateManager.RecordSelectedUpgrade`, then raises `UnitUpgradeSelected`.

## Runtime Composition
`TowerEntity.CompileFinalStats()` is the single runtime compilation point for tower upgrades.

Compilation outputs:
- Final stats using `finalStat = (baseStat + totalAdd) * totalMult`.
- The latest `Override` weapon prefab from the applied upgrade list.
- The ordered additive list of on-hit effect prefabs from all applied upgrades.

Runtime application:
- The serialized `attackBehaviour` is preserved as the default weapon.
- If at least one override upgrade exists, the latest override prefab is instantiated as the runtime active weapon.
- If no override exists, the default attack behaviour remains active.
- On-hit effect prefabs are instantiated as runtime children and exposed to the active weapon.
- Appending a new upgrade only adds new effect instances when the current effect list is a prefix of the compiled list.

Important constraints:
- `TowerEntity.AddUpgrade` ignores null or duplicate upgrade assets.
- `RemoveUpgrade` exists for runtime/editor utility, but the normal roster flow only adds upgrades.
- `UnitStateManager` stores upgrade references only; it does not inspect stat effects or construct runtime weapon/effect instances.

## Hit Effect Pipeline
`OnHitEffectBehaviour` is the base class for C# authored on-hit effects.

Effect scripts implement:
```csharp
protected override void ExecuteHitEffect(AttackHitContext context)
```

`AttackHitContext` carries:
- The owning `TowerEntity`.
- The attacker/root transform.
- The source `AttackBehaviour`.
- The source `BaseProjectile`, when the hit came from a projectile.
- Target transform, hit collider, final damage, hit position, and whether a hit position is available.

Dispatch points:
- `AttackBehaviour.TryApplyDamage` dispatches effects for direct and hitscan-style attacks.
- `BaseProjectile.OnHit` applies damage, then dispatches effects using projectile hit context.
- Projectile initialization still supports the old `Initialize(float damage, Transform owner)` overload, but upgraded projectile weapons should call the overload with tower, attack behaviour, and effect list.

## Extension Rules
- Add new stats in `EntityConstants.cs` and update `TowerEntity.GetDefaultStat()`.
- Add new attack behaviours by deriving from `AttackBehaviour`; do not put weapon-specific logic in `TowerEntity`.
- Add new on-hit effects by deriving from `OnHitEffectBehaviour`; do not hard-code effect-specific behavior into `TowerEntity`, `AttackBehaviour`, or `BaseProjectile`.
- Keep offer pools and complex offer rules out of `UnitStateManager`. If rarity, prerequisites, tags, or synergies grow, extract offer generation behind `UpgradesManager`.
- Future upgrade gating metadata should live on `UpgradeSO`, such as prerequisite upgrades, class/tag gates, or level-specific offer hints. Do not implement those rules until the design is ready.
- Keep scripts in the global namespace unless the project is intentionally migrated.

## Verification Checklist
- `dotnet build Assembly-CSharp.csproj` after script changes.
- `UnitStateManager.OwnedUnitState` does not expose per-unit offer pool or offer count.
- `UpgradesManager` exposes one shared upgrade pool and one shared offer count.
- Stat-only upgrades still update final tower stats and vision range.
- Multiple override upgrades keep all selections recorded, with the latest override firing.
- Earlier stat and on-hit upgrades remain active after a later weapon override.
- Direct, debug/hitscan, and projectile attacks all trigger on-hit effects.
- Applied upgrades persist through recall and redeploy.
