Class implementations in this folder are template/base behaviours for future attack implementations that need shared weapon logic.

## Current Attack Base Shape

- `AttackBehaviour` is the runtime weapon entrypoint called by `TowerEntity`.
- Concrete weapons should implement `ExecuteAttack(Transform target, float damage)`.
- Keep damage multiplier handling in `AttackBehaviour.Attack`; concrete attacks receive the final damage value.
- `TowerEntity` configures the active attack behaviour with owner/root context and active on-hit effects.
- Do not put projectile, hitscan, beam, or leading-specific logic directly into `TowerEntity`.

## On-Hit Effects

- Upgrade-authored on-hit effects derive from `OnHitEffectBehaviour`.
- `AttackHitContext` carries the tower, root transform, source attack behaviour, optional projectile, target, hit collider, final damage, and hit position data.
- Direct and hitscan-style attacks should use `TryApplyDamage()` so damage and on-hit effects stay in the same dispatch path.
- Projectile attacks should pass `OwnerTower`, `this`, and `OnHitEffects` into projectile initialization.
- `BaseProjectile.OnHit()` applies damage first, then dispatches the projectile's copied effect list.
- Do not hard-code effect-specific behavior into `AttackBehaviour`, `BaseProjectile`, or `TowerEntity`.

## Leading Bases

- `LeadingAttackBehaviour` is for simple predictive aim.
- It calculates an aim point from `GetAttackOrigin()`, target position, target velocity, and `DistanceFactor`.
- It reads target velocity from `Rigidbody.linearVelocity` by default.
- Override `GetAttackOrigin()` for guns that fire from a muzzle/socket instead of the tower transform.
- Override `TryGetTargetVelocity()` when the target movement system is not Rigidbody-based.
- Lead gizmos are recorded when `GetLeadPosition()` is called and are drawn only while the object is selected.

## Spline Leading

- `SplineLeadingAttackBehaviour` intentionally derives from `LeadingAttackBehaviour`.
- It keeps the same distance-factor leading formula and only replaces target velocity lookup.
- It calculates target velocity from `SplineAnimate` by finding the nearest point on the spline path, reading the tangent, and multiplying by traversal speed.
- It falls back to `LeadingAttackBehaviour`'s Rigidbody velocity lookup when no usable `SplineAnimate` data exists.
- Do not reintroduce a separate projectile-speed intercept solver unless explicitly requested; tune `DistanceFactor` for this version.

## Projectile Attacks

- Projectile GameObjects should use `BaseProjectile` or a subclass.
- `BaseProjectile` owns lifetime, trigger-hit filtering, owner ignoring, and damage dispatch.
- Movement belongs in subclasses such as `BaseStraightProjectile`, not in attack behaviours.
- Attack behaviours should instantiate projectile prefabs, initialize damage and owner, set direction/targeting data, then call `Fire()`.
- Upgraded projectile attacks should use the `BaseProjectile.Initialize(float, Transform, TowerEntity, AttackBehaviour, IReadOnlyList<OnHitEffectBehaviour>)` overload.
- A straight projectile prefab must have a trigger collider and `BaseStraightProjectile`.

## Test Implementations

- `TestGunAttackBehaviour` is a spline-leading projectile weapon example.
- `TestDumbGunAttackBehaviour` is the equivalent simple-leading projectile weapon example if present in the branch.
- Treat test guns as wiring examples; move shared production behaviour into reusable base classes before adding more concrete weapons.
