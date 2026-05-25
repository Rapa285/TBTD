Class implementations in this folder are template/base behaviours for future attack implementations that need shared weapon logic.

## Current Attack Base Shape

- `AttackBehaviour` is the runtime weapon entrypoint called by `TowerEntity`.
- Concrete weapons should implement `ExecuteAttack(Transform target, float damage)`.
- Keep damage multiplier handling in `AttackBehaviour.Attack`; concrete attacks receive the final damage value.
- `TowerEntity` configures active attack behaviours with owner/root context, runtime hit modifiers, and projectile modifier prefabs.
- `AttackBehaviour` has an optional serialized `attackFX` reference exposed through `AttackFX`. Store it as a `MonoBehaviour` in the inspector and assign a component that implements `AttackFXComponent`.
- `AttackBehaviour.Attack` does not automatically play attack FX. Concrete attack scripts decide when a gameplay action should trigger `AttackFX.PlayAttackFX(AttackFXContext)`.
- `CombatDamageUtility` owns the shared context-aware damage, legacy `IDamageable`, and `SendMessage` fallback order used by attacks and projectiles.
- New non-projectile weapons such as direct damage, laser, beam, or hitscan attacks should explicitly decide how `ProjectileModifierBehaviour` hooks participate. Use `TryApplyDamage()` for normal single-hit weapons, or call `DispatchHitModifiers(...)` when a custom damage path still needs upgrade-authored hit behavior.
- Do not put projectile, hitscan, beam, or leading-specific logic directly into `TowerEntity`.

## Attack FX Components

- `AttackFXComponent` is the shared interface for attack visualization components layered on top of attack logic and projectiles.
- `AttackFXContext` carries source attack, owner tower, owner root, target, damage, optional origin, optional hit/FX position, and optional hit collider data.
- Attack FX components are visual-only. They should not apply damage, dispatch projectile modifiers, spend ammo, retarget, or mutate tower runtime stats.
- Missing, null, or non-`AttackFXComponent` `attackFX` assignments should be treated as no visual output.
- `AuraAttackFXComponent` is the current production example: it owns an aura `ParticleSystem` and emits one particle at the context hit position for each successfully damaged target.

## Projectile Hit VFX

- Projectile hit VFX are visual-only and should use `BaseProjectile.OnBulletHit` plus `VFXRequester`, not projectile modifiers.
- `OnBulletHit` supplies the transform used for VFX placement: straight bullets use the projectile transform, grenades use the projectile transform at completed arc, and beams use each hit target transform once per beam lifetime.
- `VFXRequester` belongs on projectile prefabs and requests a `VFXType` from the scene `VFXService`.
- `VFXService` resolves the requested `VFXType` through its `availableVFXs` list of `VFXDefSO` assets.
- VFX instances are pooled per definition. The service uses the first inactive child in that definition's pool and dynamically creates another instance if the pool is exhausted.
- Pooled VFX return by disabling themselves. Add `VFXSelfDisable` to VFX prefabs for custom lifetime; otherwise the service warns and adds the default scaled-time one-second disabler to spawned instances.
- Do not use hit VFX to apply damage, dispatch modifiers, spend ammo, choose targets, or mutate tower runtime stats.

## Hit And Projectile Modifiers

- Upgrade-authored hit and projectile modifiers derive from `ProjectileModifierBehaviour`.
- `ProjectileModifierContext` carries the projectile, attacker tower, source attack behaviour, owner root, optional hit target/collider, damage, hit position data, and tick delta time.
- Direct and hitscan-style attacks should use `TryApplyDamage()` so damage and hit modifiers stay in the same dispatch path.
- Direct and hitscan-style attacks dispatch only the hit hook with `ProjectileModifierContext.Projectile == null`.
- Beam, laser, chained, area, or multi-hit non-projectile attacks may need custom integration. Each resolved target hit should dispatch hit modifiers when upgrade behavior should apply, but visual-only ticks should not dispatch hit hooks unless they represent an actual gameplay hit.
- `BaseProjectile` instantiates modifier prefabs as runtime children during `Initialize`.
- In-flight projectiles snapshot modifier instances when fired, so later tower upgrades do not change projectiles already in the scene.
- Modifier hooks run on projectile initialization, per-frame tick, hit resolution after damage, and expiry.
- Projectile attacks should pass `OwnerTower`, `this`, and `ProjectileModifiers` into projectile initialization.
- `ProjectilePropertiesModifierBehaviour` provides common authored changes for lifetime, destroy-on-hit, straight projectile speed, and supported collider dimensions.
- Damage upgrades should normally use `ENTITY_STATS.GlobalDamage` on `UpgradeSO` stat effects; projectile damage is passed in from the weapon's `AttackBehaviour.BaseDamage` after tower damage multipliers are applied.
- Do not hard-code effect-specific behavior into `AttackBehaviour`, `BaseProjectile`, or `TowerEntity`.
- Do not use projectile modifiers for visual-only attack feedback. Use `AttackFXComponent` when the behavior is presentation-only.

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
- `ProjectilePoolService` owns scene-level projectile pooling through `ProjectileDefSO` mappings keyed by `ProjectileType`.
- `ColliderTargetUtility` owns the shared collider-to-target transform rule: use `attachedRigidbody.transform` when present, otherwise use the collider transform.
- Movement belongs in subclasses such as `BaseStraightProjectile`, not in attack behaviours.
- Attack behaviours should request projectiles from `ProjectilePoolService`, initialize damage and owner, set direction/targeting data, then call `Fire()`.
- If setup fails after a pooled projectile is requested, call `CancelProjectile()` instead of destroying the GameObject.
- Upgraded projectile attacks should use the `BaseProjectile.Initialize(float, Transform, TowerEntity, AttackBehaviour, IReadOnlyList<ProjectileModifierBehaviour>)` overload.
- A straight projectile prefab must have a trigger collider and `BaseStraightProjectile`.
- Optional on-hit VFX should be wired by adding `VFXRequester` to the projectile prefab and selecting the desired `VFXType`; the scene must include a configured `VFXService`.

## Test Implementations

- `TestGunAttackBehaviour` is a spline-leading projectile weapon example.
- `TestDumbGunAttackBehaviour` is the equivalent simple-leading projectile weapon example if present in the branch.
- `DebugAttackBehaviour` and `DebugEnemySpawner` are development utilities, not required production combat components.
- Treat test guns as wiring examples; move shared production behaviour into reusable base classes before adding more concrete weapons.
