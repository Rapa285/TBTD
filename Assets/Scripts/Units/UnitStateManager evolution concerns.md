# UnitStateManager Evolution Concerns

## Intended Role
`UnitStateManager` should represent the player's full owned-unit roster. It is a player roster state manager, not a replacement for tower runtime behavior.

Each owned unit should live inside a nested serializable `OwnedUnitState` record keyed by a stable `unitId`. The record may store identity, progression, XP, applied upgrades, pending upgrade choices, and a transient reference to that unit's current deployed runtime tower.

The manager should apply persistent per-unit state to `TowerEntity` through public methods such as `AddUpgrade`, keeping stat compilation and combat execution inside the existing tower system.

## Design Smells To Watch
- God object / manager bloat: avoid letting this class become responsible for every unit-related feature.
- Feature envy toward `TowerEntity`: do not inspect upgrade stat effects and manually calculate final stats here.
- Roster/per-unit boundary blur: keep roster-wide lookup and per-unit state changes clear through `OwnedUnitState` and `unitId`.
- Persistent/transient state mixing: keep long-lived unit progression separate from runtime-only tower facts.
- Deployment coupling: do not move raycasts, placement validity, preview materials, or mouse input into this class.
- Upgrade-offer bloat: simple per-unit offer generation is fine, but complex rarity/prerequisite/synergy rules should be extracted later.
- Index-only unit lookup: use stable `unitId` values instead of list indexes for UI and deployment calls.
- Index-only upgrade selection: support UI-friendly choice indexes, but also support direct `UpgradeSO` selection.
- Hidden event chains: events should notify listeners; they should not hide major state transitions behind implicit cascades.

## Hard Boundaries
- Do not duplicate `TowerEntity.CompileFinalStats()`.
- Do not directly manipulate `UnitVision`, attack cooldowns, targeting, or weapon behavior from `UnitStateManager`.
- Do not put raycasts, mouse input, placement validation, preview materials, or UI button logic in `UnitStateManager`.
- Treat each `OwnedUnitState.currentRuntimeInstance` as transient. It is a live scene reference, not saved progression state.
- Do not allow one unit's XP, upgrades, pending choices, or runtime instance to leak into another owned unit's state.

## Recommended Direction
- Think of the class as "player roster state" instead of a full gameplay hub.
- Keep `OwnedUnitState` as the per-unit state boundary.
- Keep `ApplyStateTo(string unitId, TowerEntity tower)` as the main bridge into combat.
- Prefer both `SelectUpgrade(string unitId, int choiceIndex)` and `SelectUpgrade(string unitId, UpgradeSO upgrade)` when implementation happens.
- Let `TowerEntity` remain the runtime authority for base stats, compiled final stats, deployed state, targeting, and attacks.
- Extract upgrade-offer generation later only if the rules become complex enough to justify it.
