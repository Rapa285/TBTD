Current upgrade system state.

Runtime ownership:
- `UnitStateManager` stores persistent selected multi-upgrade levels and one selected evolution per unit.
- `EvolutionSO` stores prerequisites and resolves to one tower-facing `UpgradeSO` leaf.
- `TowerEntity.CompileFinalStats()` remains the only runtime stat, weapon override, augment, and projectile modifier composition path.

Active upgrade lines:
- High Caliber.
- Relentless Assault.
- High Capacity.
- Far-Reach.
- Carnage.

Active evolutions:
- Shotgun requires Carnage Lv2 and High Capacity Lv2.
- Machine Gun requires High Capacity Lv2 and Relentless Assault Lv2.
- Aura requires Relentless Assault Lv2 and Far-Reach Lv2.
- Sniper requires Far-Reach Lv2 and High Caliber Lv2.
- Grenade Launcher requires High Caliber Lv2 and Carnage Lv2.

Offer rules:
- Upgrade offers are generated from `UpgradesManager.upgradePool` plus eligible entries from `UpgradesManager.evolutionPool`.
- Active manager pools should not include Burst or Laser.
- Evolutions are offered only when all prerequisites are met and the unit has not already evolved.
- Lv3 upgrade leaves are valid continued line upgrades but are not required for evolution.

Stat display:
- `UpgradeStatInfoUI` displays `GlobalDamage` as DMG, `AttackSpeed` as ASP, `VisualRange` as VIS, `AmmoUnits` as AMO, `SetupTime` as SET, and `BulletSize` as BUL.
- Runtime `AttackSpeed` is still cooldown seconds. Multiplicative ASP effects are displayed as the inverse of the stored cooldown multiplier.

Legacy content:
- Laser scripts, prefabs, projectiles, VFX definitions, and old assets can remain in the repository for now.
- Those legacy assets must not be referenced by active upgrade or evolution pools unless Laser is deliberately brought back.
