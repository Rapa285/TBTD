Current report is from the files on disk. Assumption for evolved-unit totals: base tower + the two required Lv2 prerequisite upgrades + the EVO upgrade, no Lv3 upgrades.

Runtime AttackSpeed is cooldown seconds. I list ASP as player-facing attack frequency, with cooldown in parentheses.

**Base Stats**
| Stat | Value |
|---|---:|
| DMG | 40 |
| ASP | 1.00/s, cooldown 1.00s |
| VIS | 5 |
| SET | 1.5 |
| AMO | 40 |
| BUL | 1 |

**Square Upgrades**
| Upgrade | Lv1 | Lv2 | Lv3 |
|---|---|---|---|
| High Caliber | DMG x1.5, ASP x0.95 | DMG x2.25, ASP x0.75 | DMG x3, ASP x0.5 |
| Relentless Assault | ASP x1.2, SET +0.5 | ASP x1.5, SET +1 | ASP x2, SET +2 |
| High Capacity | AMO x1.2, DMG x0.95 | AMO x1.5, DMG x0.85 | AMO x2, DMG x0.7 |
| Far-Reach | VIS x1.5 | VIS x2 | VIS x2.75 |
| Carnage | BUL x1.2, AMO x0.95 | BUL x1.5, AMO x0.85 | BUL x2, AMO x0.7 |

**EVO Effects**
| EVO | Prereqs | EVO Stat Changes |
|---|---|---|
| Shotgun | Carnage Lv2 + High Capacity Lv2 | SET x0.25, AMO x0.6, DMG x1.2, ASP x1.1, BUL x1.15, VIS x0.9 |
| Machine Gun | High Capacity Lv2 + Relentless Assault Lv2 | AMO x2, ASP x6.67, SET +0.5, DMG x0.9 |
| Aura | Relentless Assault Lv2 + Far-Reach Lv2 | SET +1, VIS x1.1, ASP x1.1, DMG x0.9 |
| Sniper | Far-Reach Lv2 + High Caliber Lv2 | SET +3.5, VIS x10, DMG x3, ASP x0.65, AMO x0.9 |
| Grenade Launcher | High Caliber Lv2 + Carnage Lv2 | ASP x0.8, SET +0.5, DMG x1.5, BUL x1.2, AMO x0.75, VIS +2.5 |

**Expected Evolved Stats**
| EVO | DMG | ASP / Cooldown | VIS | SET | AMO | BUL |
|---|---:|---:|---:|---:|---:|---:|
| Shotgun | 40.8 | 1.10/s, 0.91s | 4.5 | 0.375 | 30.6 | 1.725 |
| Machine Gun | 30.6 | 10.00/s, 0.10s | 5 | 3.0 | 120 | 1 |
| Aura | 36 | 1.65/s, 0.61s | 11 | 3.5 | 40 | 1 |
| Sniper | 270 | 0.49/s, 2.05s | 100 | 5.0 | 36 | 1 |
| Grenade Launcher | 135 | 0.60/s, 1.67s | 7.5 | 2.0 | 25.5 | 1.8 |

Note: runtime ammo capacity is floored by `TowerEntity`, so 30.6 becomes 30 and 25.5 becomes 25 for actual max ammo.V