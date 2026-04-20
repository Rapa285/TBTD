# UnitStateManager Boundaries

## Current Role
`UnitStateManager` represents the player's full owned-unit roster. It is a roster state manager, not a replacement for tower runtime behavior.

Each owned unit lives inside a nested serializable `OwnedUnitState` record keyed by a stable `unitId`. The record stores identity, progression config, XP, applied upgrades, pending upgrade state, and transient references to that unit's deployed runtime tower/root.

The manager applies persistent per-unit state to `TowerEntity` through `ApplyStateTo` and `TowerEntity.AddUpgrade`. Stat compilation, weapon override/augment composition, projectile modifier composition, targeting, and attack execution stay inside the tower/combat system.

## Design Smells To Watch
- God object / manager bloat: avoid letting this class become responsible for every unit-related feature.
- Feature envy toward `TowerEntity`: do not inspect upgrade stat effects and manually calculate final stats here.
- Roster/per-unit boundary blur: keep roster-wide lookup and per-unit state changes clear through `OwnedUnitState` and `unitId`.
- Persistent/transient state mixing: keep long-lived unit progression separate from runtime-only tower facts.
- Deployment coupling: do not move raycasts, placement validity, preview materials, or mouse input into this class.
- Upgrade-offer bloat: `UpgradesManager` currently owns simple random offers. Complex rarity/prerequisite/synergy rules should be extracted later.
- Index-only unit lookup: use stable `unitId` values instead of list indexes for UI and deployment calls.
- Index-only upgrade selection: `UpgradesManager` supports both UI-friendly choice indexes and direct `UpgradeSO` selection.
- Hidden event chains: events should notify listeners; they should not hide major state transitions behind implicit cascades.

## Hard Boundaries
- Do not duplicate `TowerEntity.CompileFinalStats()`.
- Do not directly manipulate `UnitVision`, attack cooldowns, targeting, or weapon behavior from `UnitStateManager`.
- Do not put raycasts, mouse input, placement validation, preview materials, or UI button logic in `UnitStateManager`.
- Treat each `OwnedUnitState.currentRuntimeInstance` as transient. It is a live scene reference, not saved progression state.
- Do not allow one unit's XP, upgrades, pending choices, or runtime instance to leak into another owned unit's state.

## Extension Direction
- Think of the class as player roster state instead of a full gameplay hub.
- Keep `OwnedUnitState` as the per-unit state boundary.
- Keep `ApplyStateTo(string unitId, TowerEntity tower)` as the main bridge into combat.
- Keep upgrade offer generation and selection in `UpgradesManager`.
- Let `TowerEntity` remain the runtime authority for base stats, compiled final stats, deployed state, targeting, active weapons, projectile modifiers, and attacks.
- Extract upgrade-offer generation later only if the rules become complex enough to justify it.
