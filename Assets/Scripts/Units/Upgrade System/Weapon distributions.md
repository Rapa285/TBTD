Implement the first draft of the tower weapon evolution upgrade system.

Implementation status:
- The six square-node `MultiUpgradeSO` lines now exist under `Assets/SOs/Square Upgrades/`, each with Lv1-Lv3 `UpgradeSO` leaves.
- Weapon evolutions are represented by `EvolutionSO` assets under `Assets/SOs/Evolutions/`.
- Current evolution offer rules require both adjacent square-node prerequisites at Lv2 and allow only one selected evolution per roster unit.
- Machine Gun and Sniper now have concrete attack prefabs; Machine Gun uses internal tick-skip wind-up, and Sniper uses direct damage plus shared line FX.
- Laser and Sniper share `LineAttackFXComponent`, which draws the full line immediately and thins it out.

Game context:
- Tower defense game.
- Player starts with 8 default towers.
- All towers begin as a base Pistol.
- Pistol fires a bullet every few seconds.
- Towers have limited ammo.
- Towers can be recalled and redeployed.
- DeploymentCooldown controls how long a recalled tower is locked before it can be deployed again.
- SetupTime controls how long a tower takes after deployment before it starts attacking.
- On level-up, the player chooses 1 upgrade from 3 offered choices.
- Weapon evolutions are mutually exclusive for now.
- Square upgrades affect the tower immediately.
- Square upgrades also act as prerequisites for weapon evolutions.
- Each square upgrade contributes to two adjacent weapon evolutions.
- Evolution requirement: a tower must have Lv2 in both adjacent square upgrades.
- Square upgrades can reach Lv2 before evolution.
- Optional: Lv3 can exist later as post-evolution specialization, but do not require Lv3 for evolution.

Important stat notation:
- GlobalDamage + means more damage.
- AttackSpeed + means faster attacks.
- VisualRange + means longer range.
- AmmoAmount + means more ammo.
- SetupTime reduced means the tower starts attacking sooner.
- SetupTime increased means the tower takes longer to start attacking.
- DeploymentCooldown reduced means faster redeployment after recall.
- DeploymentCooldown increased means slower redeployment after recall.

Available stats:
- GlobalDamage
- AttackSpeed
- VisualRange
- SetupTime
- AmmoAmount
- DeploymentCooldown

Base weapon:
Pistol
- Default weapon for all towers.
- Simple single-target bullet attack.
- No special behavior.
- Used until the tower evolves.

Square upgrade nodes:
1. Widespread
2. Stopping Power
3. Relentless Assault
4. Far-Reach
5. Burst
6. AoE

Weapon evolution nodes:
1. Shotgun
2. Machine Gun
3. Laser
4. Sniper
5. Grenade Launcher
6. Aura

Evolution unlock requirements:
- Shotgun requires Widespread Lv2 + Stopping Power Lv2.
- Machine Gun requires Stopping Power Lv2 + Relentless Assault Lv2.
- Laser requires Relentless Assault Lv2 + Far-Reach Lv2.
- Sniper requires Far-Reach Lv2 + Burst Lv2.
- Grenade Launcher requires Burst Lv2 + AoE Lv2.
- Aura requires AoE Lv2 + Widespread Lv2.

Upgrade identity map:
- Shotgun = coverage + power.
- Machine Gun = power + speed.
- Laser = speed + reach.
- Sniper = reach + burst.
- Grenade Launcher = burst + AoE.
- Aura = AoE + coverage.

Implement square upgrades with these immediate effects:

Widespread:
Identity: more coverage, less precision.
Supports: Aura + Shotgun.

Lv1:
- VisualRange increased.
- AmmoAmount increased.
- GlobalDamage decreased.

Lv2:
- VisualRange increased more than Lv1.
- AmmoAmount increased.
- GlobalDamage decreased.
- AttackSpeed decreased.

Optional Lv3, not required for evolution:
- VisualRange increased greatly.
- AmmoAmount increased more.
- GlobalDamage decreased heavily.

Stopping Power:
Identity: stronger bullets, heavier recoil.
Supports: Shotgun + Machine Gun.

Lv1:
- GlobalDamage increased.
- AttackSpeed decreased.

Lv2:
- GlobalDamage increased more than Lv1.
- AttackSpeed decreased.
- AmmoAmount decreased.

Optional Lv3, not required for evolution:
- GlobalDamage increased greatly.
- AttackSpeed decreased heavily.
- AmmoAmount decreased.

Relentless Assault:
Identity: sustained fire and pressure.
Supports: Machine Gun + Laser.

Lv1:
- AttackSpeed increased.
- AmmoAmount decreased.

Lv2:
- AttackSpeed increased more than Lv1.
- AmmoAmount decreased.
- SetupTime increased.

Optional Lv3, not required for evolution:
- AttackSpeed increased greatly.
- AmmoAmount decreased heavily.
- SetupTime increased.

Far-Reach:
Identity: precision at distance.
Supports: Laser + Sniper.

Lv1:
- VisualRange increased.
- GlobalDamage increased.

Lv2:
- VisualRange increased more than Lv1.
- GlobalDamage increased.
- SetupTime increased.

Optional Lv3, not required for evolution:
- VisualRange increased greatly.
- GlobalDamage increased more.
- SetupTime increased heavily.

Burst:
Identity: large damage windows, poor cadence.
Supports: Sniper + Grenade Launcher.

Lv1:
- GlobalDamage increased.
- AttackSpeed decreased.

Lv2:
- GlobalDamage increased more than Lv1.
- AttackSpeed decreased heavily.
- AmmoAmount increased.

Optional Lv3, not required for evolution:
- GlobalDamage increased greatly.
- AttackSpeed decreased heavily.
- SetupTime increased.

AoE:
Identity: multi-target coverage, weaker single-target pressure.
Supports: Grenade Launcher + Aura.

Lv1:
- VisualRange increased.
- GlobalDamage decreased.

Lv2:
- VisualRange increased.
- AttackSpeed increased.
- GlobalDamage decreased.

Optional Lv3, not required for evolution:
- VisualRange increased more.
- AttackSpeed increased.
- GlobalDamage decreased heavily.

Weapon evolutions and their stat packages:

Shotgun:
Role: panic clear / close-range wave control.
Fantasy: short-to-mid range burst cone, deletes clustered enemies, inconsistent at distance.
Special behavior:
- Replaces Pistol with a shotgun attack.
- Fires multiple pellets or a cone spread.
- Strong against nearby clustered enemies.
- Weak at long distance.
Stat package:
- GlobalDamage increased greatly.
- AttackSpeed increased.
- VisualRange decreased heavily.
- AmmoAmount decreased.
- SetupTime reduced.
- DeploymentCooldown increased.

Machine Gun:
Role: lane stabilizer / sustained anti-horde DPS.
Fantasy: sustained bullet hose, great against streams, poor burst per shot.
Special behavior:
- Replaces Pistol with a rapid-fire bullet stream.
- Great at sustained damage.
- Burns ammo quickly through frequent attacks.
- Weaker per-shot damage.
Stat package:
- AttackSpeed increased greatly.
- AmmoAmount increased.
- GlobalDamage decreased.
- VisualRange unchanged.
- SetupTime increased.
- DeploymentCooldown unchanged.

Laser:
Role: anti-elite / lane-piercing sustained damage.
Fantasy: focused beam, pierces or ramps damage, excellent against lines or durable enemies.
Special behavior:
- Replaces Pistol with a laser beam.
- Beam may pierce enemies or continuously damage a target.
- Optional behavior: damage ramps up while staying on the same target, and resets when changing target.
- Strong when pre-positioned.
- Bad as an emergency deployment.
Stat package:
- GlobalDamage increased.
- AttackSpeed unchanged.
- VisualRange increased.
- AmmoAmount decreased.
- SetupTime increased heavily.
- DeploymentCooldown unchanged.

Sniper:
Role: elite killer / backline support / long-range control.
Fantasy: long-range, high-damage shots, deletes priority targets.
Special behavior:
- Replaces Pistol with a slow, long-range precision shot.
- Prioritizes high-value targets if target priority logic exists.
- Weak against swarms.
- Ammo should not be too low because slow firing already limits output.
Stat package:
- GlobalDamage increased greatly.
- AttackSpeed decreased heavily.
- VisualRange increased greatly.
- AmmoAmount unchanged.
- SetupTime increased.
- DeploymentCooldown unchanged.

Grenade Launcher:
Role: clustered enemy punish / ranged splash damage / wave breaker.
Fantasy: explosive AoE shells, strong multi-hit potential, slow and clunky.
Special behavior:
- Replaces Pistol with a slow projectile that explodes on impact.
- Deals splash damage in an area.
- Better at controlling groups before they reach danger range.
- Less immediate than Shotgun.
Stat package:
- GlobalDamage increased greatly.
- AttackSpeed decreased heavily.
- VisualRange increased.
- AmmoAmount decreased.
- SetupTime increased.
- DeploymentCooldown unchanged.

Aura:
Role: passive zone control / anti-swarm / placement puzzle.
Fantasy: damages all enemies in a radius with constant local area denial.
Special behavior:
- Replaces Pistol with an aura damage field.
- Damages all enemies within radius at intervals or continuously.
- Low per-target damage.
- Very reliable against swarms.
- Weak against bulky enemies.
Stat package:
- GlobalDamage decreased.
- AttackSpeed increased.
- VisualRange decreased heavily.
- AmmoAmount increased.
- SetupTime reduced.
- DeploymentCooldown increased.

Upgrade offer rules:
- On level-up, offer 3 choices by default.
- Include available square upgrades that are not yet at their current max level.
- If a tower has Lv2 in both prerequisite square upgrades for an evolution, add that weapon evolution to the possible offer pool.
- Do not offer evolutions for weapons if the tower has already evolved.
- Once a tower evolves, lock out all other weapon evolutions.
- Evolution should be presented as a special/rare upgrade choice, but it should be reasonably likely once requirements are met.
- Do not require Lv3 upgrades for evolution.
- If Lv3 is implemented, make it optional and preferably available after evolution as further specialization.

Data structure suggestion:
Represent square upgrades with:
- id
- displayName
- maxLevel
- effectsByLevel
- connectedEvolutions

Represent weapon evolutions with:
- id
- displayName
- prerequisiteUpgradeIds
- statPackage
- weaponBehaviorId
- roleDescription

Use these ids:

Square upgrade ids:
- widespread
- stopping_power
- relentless_assault
- far_reach
- burst
- aoe

Weapon evolution ids:
- shotgun
- machine_gun
- laser
- sniper
- grenade_launcher
- aura

Implement helper logic:
- getUpgradeLevel(tower, upgradeId)
- canUpgradeSquareNode(tower, upgradeId)
- canEvolveToWeapon(tower, weaponEvolutionId)
- getAvailableEvolutionChoices(tower)
- applySquareUpgrade(tower, upgradeId)
- applyWeaponEvolution(tower, weaponEvolutionId)
- getLevelUpChoices(tower, choiceCount = 3)

Acceptance criteria:
- A tower starts as Pistol.
- A tower can take square upgrades and receive their stat effects immediately.
- A square upgrade can level to Lv2.
- A tower with Widespread Lv2 and Stopping Power Lv2 can be offered Shotgun evolution.
- A tower with Stopping Power Lv2 and Relentless Assault Lv2 can be offered Machine Gun evolution.
- A tower with Relentless Assault Lv2 and Far-Reach Lv2 can be offered Laser evolution.
- A tower with Far-Reach Lv2 and Burst Lv2 can be offered Sniper evolution.
- A tower with Burst Lv2 and AoE Lv2 can be offered Grenade Launcher evolution.
- A tower with AoE Lv2 and Widespread Lv2 can be offered Aura evolution.
- Once a tower evolves, no other weapon evolution can be offered to that tower.
- Weapon evolution replaces the Pistol behavior with the evolved weapon behavior.
- SetupTime and DeploymentCooldown effects use explicit increased/reduced semantics rather than ambiguous plus/minus signs.
