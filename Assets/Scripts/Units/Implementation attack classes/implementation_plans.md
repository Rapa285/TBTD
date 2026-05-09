This documment explains the AttackBehaviour implementations that are going to be implemented :

0. BaseGunBehaviour
- Basically identical (if not, the same) as TestGunAttackBehaviour, this script is just to formalize the fact that this behaviour will be used in the end product instead of using TestGunAttackBehaviour on default towers

1. GrenadeLauncherBehaviour
- Has a new BulletBehaviour : Arching bullet
- Actual logic of arching will be defined in arching bullet, GrenadeLauncher behaviour only tells ArchingBullet where it needs to end up
- ArchingBullet will, given a point of where it is and where the enemy is, travel in an arching motion with (max) height of h
- There will be a ratio of distance to height, with closer enemies causing the bullet to arc higher and further enemies having less height
- the grenade launcher's bullet upon landing (do not check physically, just see from the trajectory animation) will "explode", doing so by doing a sphere cast to check targets inside

2. ShotgunBehaviour
- Very similar to GunBehaviour but shoots multiple bullets at once
- the bullets have a randomized dispersion, with max deviation of about 30 degrees

3. LaserBehaviour
- Instead of shooting a "traditional" projectile, shoots a straight line akin to debug attack behaviour (by akin meaning using line renderer to show its attack)
- The way it works is that the laser wont "stop" at where the enemy is like debygattackbehaviour, but rather go straight up until the unit's range
- The laser will pierce enemies, damaging (and applying its effects) to all units hit within this laser/raycast
- in other words it raycasts towards and through a primary target (the target currently aimed at) with a certain range
- If deemed necesary, a bullet behaviour may be defined to support this feature

4. HitscanBehaviour
- Immediately damages the target
- uses line renderer to draw a line that rapidly decreases in size to show the bullet action

5. AuraBehaviour
- An attackbehaviour that doesnt rely on SplineLeading like the others
- instead whenever an enemy enters the range, starts a countdown before attacking all enemies in range
- this countdown to attack will repeat until no enemies are inside
- Aura hit feedback is visual-only and lives in `AuraAttackFXComponent`, assigned through `AttackBehaviour`'s `attackFX` reference
- Aura should call `AttackFX.PlayAttackFX(AttackFXContext)` only after a target is successfully damaged

There are also changes to the leading attack behaviour :
- The current attack system does not prioritize first/forward enemies
To fix this, implement this behaviour :
1. Instead of polling enemies flatly every second or every time an enemy enters or exits the vision range, the tower will "check/update its target every given time period"
2. When an enemy enters the trigger/collider, it starts the enemy polling so that the tower can reliably target the most front enemy (or possibly other targeting behaviours)
3. Everytime an enemy dies or exits the trigger/collider, it refreshes the poll duration and immediately scans for the next valid target
4. this way targeting can be more consistent whilst avoiding unecesary heavy computation
