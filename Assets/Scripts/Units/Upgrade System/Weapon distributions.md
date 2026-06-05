Current tower upgrade distribution draft.

Implementation status:
- Active square-node upgrade lines live under `Assets/SOs/Square Upgrades/`.
- Active weapon evolutions live under `Assets/SOs/Evolutions/`.
- Laser assets and code remain in the project as unused legacy content, but Laser is not part of active upgrade or evolution pools.
- Evolution offer rules require both prerequisite square-node lines at Lv2 and still allow only one selected evolution per roster unit.

Base stats:
- DMG: `GlobalDamage` = 40.
- ASP: player-facing attack frequency. Runtime `AttackSpeed` still stores cooldown seconds, so ASP multipliers are authored as inverse cooldown multipliers.
- VIS: `VisualRange` = 5.
- SET: `SetupTime` = 1.5.
- AMO: `AmmoUnits` = 40.
- BUL: `BulletSize` = 1.

Active square upgrade lines:
- High Caliber: DMG up, ASP down.
- Relentless Assault: ASP up, SET up.
- High Capacity: AMO up, DMG down.
- Far-Reach: VIS up.
- Carnage: BUL up, AMO down.

Square upgrade stat packages:
- High Caliber Lv1: DMG x1.2, ASP x0.95 (`AttackSpeed` x1.05263).
- High Caliber Lv2: DMG x1.5, ASP x0.75 (`AttackSpeed` x1.33333).
- High Caliber Lv3: DMG x2.0, ASP x0.50 (`AttackSpeed` x2.0).
- Relentless Assault Lv1: ASP x1.2 (`AttackSpeed` x0.83333), SET +0.5.
- Relentless Assault Lv2: ASP x1.5 (`AttackSpeed` x0.66667), SET +1.0.
- Relentless Assault Lv3: ASP x2.0 (`AttackSpeed` x0.5), SET +2.0.
- High Capacity Lv1: AMO x1.2, DMG x0.95.
- High Capacity Lv2: AMO x1.5, DMG x0.85.
- High Capacity Lv3: AMO x2.0, DMG x0.70.
- Far-Reach Lv1: VIS x1.1.
- Far-Reach Lv2: VIS x1.3.
- Far-Reach Lv3: VIS x1.5.
- Carnage Lv1: BUL x1.2, AMO x0.95.
- Carnage Lv2: BUL x1.5, AMO x0.85.
- Carnage Lv3: BUL x2.0, AMO x0.70.

Active evolution paths:
- Carnage Lv2 + High Capacity Lv2 -> Shotgun.
- High Capacity Lv2 + Relentless Assault Lv2 -> Machine Gun.
- Relentless Assault Lv2 + Far-Reach Lv2 -> Aura.
- Far-Reach Lv2 + High Caliber Lv2 -> Sniper.
- High Caliber Lv2 + Carnage Lv2 -> Grenade Launcher.

Evolution stat packages:
- Shotgun: SET x0.25, AMO x0.60, DMG x1.20, ASP x1.10 (`AttackSpeed` x0.90909), BUL x1.15, VIS x0.90.
- Machine Gun: AMO x1.50, ASP x1.25 (`AttackSpeed` x0.8), SET +0.5, DMG x0.90.
- Aura: SET +1.0, VIS x1.10, ASP x1.10 (`AttackSpeed` x0.90909), DMG x0.90.
- Sniper: SET +3.5, VIS x1.50, DMG x1.50, ASP x0.65 (`AttackSpeed` x1.53846), AMO x0.90.
- Grenade Launcher: ASP x0.80 (`AttackSpeed` x1.25), SET +0.5, DMG x1.10, BUL x1.20, AMO x0.90.

Acceptance criteria:
- Active upgrade offers contain only the five active square-node lines.
- Active evolution offers contain only Shotgun, Machine Gun, Aura, Sniper, and Grenade Launcher.
- Burst and Laser are not reachable from active upgrade offers.
- Lv3 square upgrades are available but not required for evolution.
- Weapon evolutions continue to resolve to normal `UpgradeSO` leaves and are applied by `TowerEntity.CompileFinalStats()`.
