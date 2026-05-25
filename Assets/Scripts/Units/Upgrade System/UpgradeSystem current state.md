# Upgrade System Current State

## Role
The upgrade system is split between persistent roster selection and runtime tower composition.

- `UpgradeSO` is the tower-facing upgrade leaf asset.
- `MultiUpgradeSO` is the roster/offer-facing upgrade-line asset that resolves one active level into a normal `UpgradeSO`.
- `EvolutionSO` is the roster/offer-facing weapon-evolution asset that resolves to one normal `UpgradeSO` and declares prerequisite multi-upgrade levels.
- `UpgradesManager` owns the shared multi-upgrade pool, shared evolution pool, offer count, pending upgrade offers, and selection.
- `UnitStateManager` stores selected multi-upgrade line levels plus at most one selected evolution per owned unit.
- `TowerEntity` compiles resolved `UpgradeSO` leaves into runtime stats, active weapon behaviours, and projectile modifier prefabs.
- `UnitStateManager` uses `TowerEntity.CalculateFinalStat(...)` to cache deployment cost for UI and deployment preflight without building runtime weapon/modifier composition.
- Upgrade UI scripts present pending offers, focused-choice details, stat comparisons, and evolution hints only. They do not validate choices, mutate roster state directly, or compose runtime upgrades.

The normal roster flow records selected `MultiUpgradeSO` levels and optionally one selected `EvolutionSO`. Different multi-upgrade lines remain additive, but leveling the same multi-upgrade line replaces the previous resolved `UpgradeSO` leaf with the next one so only one level from that line is active at a time. A selected evolution contributes its resolved `UpgradeSO` leaf alongside the active multi-upgrade leaves.

## UpgradeSO
`UpgradeSO` assets are created through `Create > TBTD > Upgrade`.

An upgrade can combine:
- Display data: `upgradeName` and `description`.
- Stat effects: a list of `StatEffect` entries using `ENTITY_STATS` plus `STAT_TYPE.Add` or `STAT_TYPE.Mult`.
- Weapon fields: `weaponUpgradeType` plus `weaponBehaviourPrefab`.
- Projectile modifiers: a list of `ProjectileModifierBehaviour` prefab/component references used for direct-hit effects and projectile lifecycle behavior.

Current serialized stat modifier values:
- `STAT_TYPE.Mult = 0`
- `STAT_TYPE.Add = 1`
- Existing upgrade assets with `type: 0` are multiplicative modifiers.
- Final stat formula is `finalStat = (baseStat + totalAdd) * totalMult`.

Current weapon behavior:
- `WEAPON_UPGRADE_TYPE.None` does not affect the active weapon.
- `WEAPON_UPGRADE_TYPE.Override` is implemented. The latest applied override upgrade wins.
- `WEAPON_UPGRADE_TYPE.Augment` instantiates extra runtime weapons that fire alongside the active primary weapon.

Current projectile modifier behavior:
- Every non-null projectile modifier prefab from every applied upgrade is additive.
- `TowerEntity` instantiates modifier prefabs as runtime children for direct and hitscan-style hit hooks.
- `TowerEntity` also passes compiled modifier prefabs into active weapons.
- Direct and hitscan-style attacks dispatch only the hit hook with no projectile instance in the context.
- Projectile weapons pass modifier prefabs into `BaseProjectile.Initialize`.
- `BaseProjectile` instantiates modifier instances as projectile children so in-flight projectiles keep their fired-time behavior and full projectile lifecycle hooks.

## MultiUpgradeSO
`MultiUpgradeSO` assets are created through `Create > TBTD > Multi Upgrade`.

A multi-upgrade contains:
- `levelUpgrades`: an ordered list of `UpgradeSO` assets.
- 1-based level resolution by list order.
- Helper APIs for max level, valid level lookup, and next-level lookup.

Current multi-upgrade behavior:
- A fresh unit offered a multi-upgrade resolves to level 1.
- A unit that already owns a non-max multi-upgrade can be offered that same line again and resolves to the next level.
- A unit at max level for a multi-upgrade line will not be offered that line again.
- `UpgradeChoiceItem` displays the resolved next-level `UpgradeSO` name, description, and icon.
- Focused multi-upgrade details can compare the current level leaf to the offered next level leaf in `UpgradeStatInfoUI`.
- `TowerEntity` never receives or stores `MultiUpgradeSO`; it only receives resolved `UpgradeSO` leaves.

Current square-node assets live under `Assets/SOs/Square Upgrades/`:
- Widespread, Stopping Power, Relentless Assault, Far-Reach, Burst, and AoE each have Lv1-Lv3 `UpgradeSO` leaves.
- The square-node `MultiUpgradeSO` assets reference those leaves in Lv1, Lv2, Lv3 order.
- These assets are normal multi-upgrades; prerequisite/evolution meaning is expressed by `EvolutionSO`, not by a separate square-node runtime system.

## EvolutionSO
`EvolutionSO` assets are created through `Create > TBTD > Evolution`.

An evolution contains:
- `resolvedUpgrade`: one tower-facing `UpgradeSO` leaf applied if the evolution is selected.
- `prerequisites`: a list of required `MultiUpgradeSO` lines plus minimum selected levels.

Current evolution behavior:
- `UnitStateManager.OwnedUnitState` stores at most one selected `EvolutionSO`.
- `OwnedUnitState.CanSelectEvolution(...)` requires no existing selected evolution, a valid resolved upgrade, and all prerequisite multi-upgrade levels to be met.
- `AppliedUpgrades` resolves active multi-upgrade leaves plus the selected evolution leaf.
- Selecting an evolution advances the roster unit level and immediately applies the resolved upgrade to the deployed tower through `TowerEntity.AddUpgrade`.
- Evolution assets live under `Assets/SOs/Evolutions/`.

Current authored evolution prerequisites:
- Shotgun: Widespread Lv2 + Stopping Power Lv2.
- Machine Gun: Stopping Power Lv2 + Relentless Assault Lv2.
- Laser: Relentless Assault Lv2 + Far-Reach Lv2.
- Sniper: Far-Reach Lv2 + Burst Lv2.
- Grenade Launcher: Burst Lv2 + AoE Lv2.
- Aura: AoE Lv2 + Widespread Lv2.

Visual attack feedback is separate from upgrade-authored projectile modifiers:
- `AttackBehaviour` exposes an optional `AttackFX` hook backed by a serialized `MonoBehaviour` reference.
- Assigned FX behaviours must implement `AttackFXComponent`.
- Concrete attacks decide when to call `AttackFX.PlayAttackFX(AttackFXContext)`.
- Attack FX components are presentation-only and should not be used for damage, upgrade effects, ammo, or projectile lifecycle behavior.
- `LineAttackFXComponent` is the shared line-rendered visual component used by Laser and Sniper. It draws the full line immediately, then eases the line width from thick to thin before hiding.
- Projectile on-hit VFX use `BaseProjectile.OnBulletHit`, `VFXRequester`, `VFXDefSO`, and the scene-level `VFXService`.
- `VFXService` resolves requests by `VFXType`, pools one inactive-child list per VFX definition, dynamically grows exhausted pools, and relies on `VFXSelfDisable` to return instances to the inactive pool.
- `VFXRequester` is prefab-side presentation wiring only; it must not own upgrade state, damage, projectile modifiers, ammo, targeting, or tower stat behavior.

## Offer And Selection Flow
`UpgradesManager` listens for `UnitUpgradeThresholdReached` events from `UnitEventBus`.

Current offer configuration lives on `UpgradesManager`:
- `upgradePool`: one shared `MultiUpgradeSO` pool used by all units.
- `evolutionPool`: one shared `EvolutionSO` pool used by all units.
- `upgradeChoiceCount`: one shared number of choices to offer when enough valid candidates exist.
- `baseRerollCost` and `rerollCostIncrement`: shared currency cost controls for rerolling pending offers.
- `EvolutionPool`: read-only public view used by UI hint panels to inspect evolution relationships without duplicating the pool.
- `CurrentRerollCost`: the current shared reroll price, computed from successful rerolls.

When a threshold is reached:
- Ignore missing unit IDs, missing managers, units that already have a pending offer, unknown units, or units that cannot begin upgrade selection.
- Build candidates from the manager's shared `upgradePool` and `evolutionPool`.
- Filter out null multi-upgrades, duplicate multi-upgrade references, invalid next-level assets, and multi-upgrades where the unit is already at max level.
- Filter out null evolutions, duplicate evolution references, evolutions with invalid resolved upgrades, evolutions whose prerequisites are not met, and all evolutions once the unit already has a selected evolution.
- Randomly offer up to `upgradeChoiceCount` unique choices from the combined multi-upgrade/evolution candidate list.
- Store the generated offer by stable `unitId` as a pending offer.
- Raise `UnitUpgradeChoicesOffered` when the offer is opened or rerolled.
- If no choices exist, record a null selection so the unit can advance and clear pending state.

Selection API:
- `SelectUpgrade(unitId, int choiceIndex)` selects by UI index.
- `SelectUpgrade(unitId, MultiUpgradeSO upgrade)` selects by asset reference.
- `SelectUpgrade(unitId, EvolutionSO evolution)` selects by asset reference.
- `UnitUpgradeChoiceRequestedEvent` is the decoupled UI request path; `UpgradesManager` handles it by calling the index-based selection API.
- `UnitUpgradeOfferRequestedEvent` asks `UpgradesManager` to raise a stored pending offer for a unit, creating one if the roster unit is pending and no offer is stored yet.
- `UnitUpgradeRerollRequestedEvent` asks `UpgradesManager` to replace an active pending offer with a different generated offer when possible.
- `UnitUpgradeMenuClosedEvent` clears `UpgradesManager`'s active menu unit tracking without selecting.
- Selection is only valid while a pending offer exists and the selected multi-upgrade or evolution belongs to that offer.

Reroll behavior:
- Rerolls require an existing pending offer and a candidate pool that can produce an alternate offer.
- If a `CurrencyManager` exists, `TrySpend(CurrentRerollCost)` must succeed before replacing the offer.
- Successful rerolls increment the shared reroll count, replace the stored offer, and raise `UnitUpgradeChoicesOffered`.
- Failed rerolls leave the current pending offer unchanged.

On successful selection, `UpgradesManager` removes the pending offer, calls `UnitStateManager.RecordSelectedUpgrade`, then raises `UnitUpgradeSelected`. If recording fails, the removed pending offer is restored.

## Upgrade Selection UI
Upgrade selection UI is event-bus driven and presentation-only.

Main components:
- `UpgradeSelectionUI` listens for `UnitUpgradeChoicesOffered`, binds pooled `UpgradeChoiceItem` entries under `choicesRoot`, and sends `UnitUpgradeChoiceRequestedEvent` for accepted clicks.
- `UpgradeSelectionUI` owns optional close and reroll buttons. Close raises `UnitUpgradeMenuClosed`; reroll raises `UnitUpgradeRerollRequested`.
- `UpgradeSelectionUI` refreshes reroll visibility, affordability, and cost text from `UpgradesManager` plus `CurrencyManager`.
- `UpgradeSelectionUI` initializes focused details from the first valid choice in a new offer and updates details when a child choice is hovered or UI-focused.
- `UpgradeSelectionUI` coordinates chained reveal playback for active valid choices in display order using `choiceRevealDelay`.
- `UpgradeChoiceItem` displays the resolved `UpgradeSO` name, description, and icon, then notifies its owner on pointer enter or UI selection. Clicks are ignored while the attached `UpgradeItemFX` reveal is unfinished.
- `UpgradeItemFX` handles one choice item's hover scale and reveal visuals. Its reveal grows the mask `RectTransform` from left to right by animating the right offset, and updates the sheen color material's `_AnimProgress`.
- `UnitUIUpgrade` is the roster-item upgrade button that requests a stored pending offer for its unit through `UnitUpgradeOfferRequestedEvent`.

Focused details:
- `UpgradeInfoDetailsUI` is the details coordinator. It owns an optional root, title TMP text, `UpgradeStatInfoUI`, and `EvoHintUI`.
- The title uses `UpgradeSO.UpgradeName` with the asset name as fallback.
- `UpgradeStatInfoUI` lists the focused upgrade's stat effects. For multi-upgrade offers above level 1, it compares the currently active leaf to the offered next leaf with `current >>> next` for matching stats.
- `GenericIconDisplay` binds either an `UpgradeSO` or raw `Sprite`, updates its image, and toggles its configured root when no icon exists.
- `UpgradeIconLevelUI` binds a `MultiUpgradeSO` against a roster unit. Normal display shows `LVL X`; requirement display shows `LVL X/Y`. Icons resolve from the required level for requirement display, otherwise from the current level with level 1 fallback.
- `UnitDetailsUI` shows the selected deployed unit's roster identity, XP, ammo, active multi-upgrades, and selected evolution. It owns the dedicated evolution slot separately from the multi-upgrade list.
- Optional `UnitDetailUIFX` animates the selected-unit detail panel open and closed by moving the panel `RectTransform` on X. It is presentation-only; selection identity, roster reads, XP/ammo refreshes, and evolution binding stay in `UnitDetailsUI`.
- `UnitUpgradeListUI` displays active multi-upgrade lines only; it must not bind or clear the selected evolution slot.
- `ConvertibleUpgradeHoverable` extends upgrade hover behavior with default generic tooltip content for empty slots, such as the no-evolution placeholder.

Evolution hint behavior:
- For a focused `MultiUpgradeSO`, `EvoHintUI` shows the focused upgrade in the middle, hides the target evolution icon, and searches `UpgradesManager.EvolutionPool` for evolutions whose prerequisites include the focused line.
- Related evolutions are ranked by lowest total missing prerequisite levels, then highest met-prerequisite count, then shared pool order.
- The two side slots show up to two related evolutions. Each side slot shows the evolution's resolved upgrade icon plus the other prerequisite line as `LVL current/required`.
- If a focused multi-upgrade has no related evolutions, the middle focused-upgrade slot stays visible and both side slots clear.
- For a focused `EvolutionSO`, `EvoHintUI` clears the middle focused-upgrade slot, shows `targetEvo` with the evolution's resolved upgrade icon, and uses the two side related-upgrade slots for the evolution's prerequisites.
- If the active unit already has a selected evolution, multi-upgrade hinting clears all hint slots.
- `EvoHintUI` does not decide evolution eligibility; eligibility is still authored through `EvolutionSO` and validated by `OwnedUnitState.CanSelectEvolution(...)`.

UI boundaries:
- UI scripts may read `UnitUpgradeOfferChoice`, `UpgradesManager.EvolutionPool`, and `OwnedUnitState` levels for display.
- UI scripts must not change roster upgrade state directly, generate offers independently, validate selections as authoritative, inspect runtime tower composition, or instantiate runtime weapons/modifiers.
- Prefab wiring is expected for `UpgradeSelectionUI`, `UpgradeChoiceItem`, optional `UpgradeItemFX`, `UpgradeInfoDetailsUI`, `UpgradeStatInfoUI`, `EvoHintUI`, `GenericIconDisplay`, `UpgradeIconLevelUI`, `UnitDetailsUI`, and `UnitUpgradeListUI`.
- `UpgradeItemFX` should reference the choice target transform, the mask `RectTransform`, and the sheen color `Graphic`; the mask material is not animated directly because masking materials do not reliably support this UI reveal.
- `UnitDetailUIFX` should reference the detail panel `RectTransform` and root object when used. Shown X is `0`; hidden X is computed from the panel width.

## Runtime Composition
`TowerEntity.CompileFinalStats()` is the single runtime compilation point for tower upgrades.

Compilation outputs:
- Final stats using `finalStat = (baseStat + totalAdd) * totalMult`.
- The latest `Override` weapon prefab from the applied upgrade list.
- The ordered additive list of `Augment` weapon prefabs from all applied upgrades.
- The ordered additive list of projectile modifier prefabs from all applied upgrades.

Runtime application:
- The serialized `attackBehaviour` is preserved as the default weapon.
- If at least one override upgrade exists, the latest override prefab is instantiated as the runtime active weapon.
- If no override exists, the default attack behaviour remains active.
- Augment weapon prefabs are instantiated as runtime children and fire on the same target/tick as the active primary weapon.
- Projectile modifier prefabs are instantiated as runtime children for direct/hitscan hit hooks and exposed to every active weapon for projectile initialization.
- Appending a new upgrade only adds new modifier instances when the current modifier list is a prefix of the compiled list.
- Leveling an existing multi-upgrade line calls `TowerEntity.ReplaceUpgrade(previousLeaf, nextLeaf)` so the previous level leaf stops applying before the next level leaf is compiled.

Deployment price changes should use `ENTITY_STATS.DeploymentCost` stat effects. Cached roster deployment cost is refreshed on startup and after selected upgrades.

Important constraints:
- `TowerEntity.AddUpgrade` ignores null or duplicate upgrade assets.
- `RemoveUpgrade` exists for runtime/editor utility; normal roster leveling uses `ReplaceUpgrade` when a multi-upgrade line advances.
- `UnitStateManager` stores multi-upgrade line levels, the selected evolution reference, and cached deployment cost only; it does not inspect stat effects or construct runtime weapon/modifier instances.

## Current Weapon Implementations
Current tower-facing weapon implementations include:
- Base gun: projectile weapon using `BaseStraightProjectile`.
- Shotgun: projectile spread volley.
- Machine Gun: projectile weapon whose evolved `AttackSpeed` is the top fire rate; wind-up is implemented internally by skipping attack ticks before firing.
- Laser: piercing hitscan beam that damages each valid target along a ray and uses `LineAttackFXComponent` for visual feedback.
- Sniper: direct single-target damage against TowerEntity's current target; no projectile is spawned, and `LineAttackFXComponent` draws the shot line.
- Grenade Launcher: arcing projectile weapon using a grenade projectile.
- Aura: area pulse weapon over currently tracked vision targets.

## Projectile Modifier Pipeline
`ProjectileModifierBehaviour` is the base class for C# authored hit modifiers and projectile modifiers.

Modifier scripts can override initialization, tick, hit, and expiry hooks. Direct, sniper, laser, and other hitscan-style attacks only invoke the hit hook with `ProjectileModifierContext.Projectile == null`; projectile attacks instantiate a copied modifier set on each projectile and can invoke the full lifecycle. `ProjectilePropertiesModifierBehaviour` covers common lifetime, destroy-on-hit, straight projectile speed, and collider-size changes.

Projectile initialization still supports the old `Initialize(float damage, Transform owner)` overload, but upgraded projectile weapons should call the overload with tower, attack behaviour, and projectile modifier list.

Damage scaling should normally be authored as `ENTITY_STATS.GlobalDamage` stat effects. Projectile damage comes from the active weapon's `AttackBehaviour.BaseDamage` after tower damage multipliers are applied, so projectile modifiers should be reserved for bullet behavior rather than base damage balance.

## Extension Rules
- Add new stats in `EntityConstants.cs` and update `TowerEntity.GetDefaultStat()`.
- Add new attack behaviours by deriving from `AttackBehaviour`; do not put weapon-specific logic in `TowerEntity`.
- Add visual-only attack feedback by implementing `AttackFXComponent` and wiring it through the attack's `attackFX` reference.
- Add projectile hit VFX by creating `VFXDefSO` assets, registering them on scene `VFXService`, and assigning `VFXType` on projectile `VFXRequester` components.
- Add new hit modifiers or projectile modifiers by deriving from `ProjectileModifierBehaviour`; do not hard-code modifier-specific behavior into `TowerEntity`, `AttackBehaviour`, or `BaseProjectile`.
- Keep offer pools and complex offer rules out of `UnitStateManager`. If rarity, prerequisites, tags, or synergies grow, extract offer generation behind `UpgradesManager`.
- Evolution prerequisite metadata lives on `EvolutionSO`; tower-facing effects still live on the resolved `UpgradeSO` leaf.
- Keep scripts in the global namespace unless the project is intentionally migrated.

## Verification Checklist
- `dotnet build Assembly-CSharp.csproj` after script changes.
- `UnitStateManager.OwnedUnitState` does not expose per-unit offer pool or offer count.
- `UpgradesManager` exposes one shared multi-upgrade pool and one shared offer count.
- `UpgradesManager` exposes one shared evolution pool and combines eligible evolutions with normal multi-upgrade choices.
- Upgrade UI uses `UpgradesManager.EvolutionPool` for hinting instead of maintaining a parallel evolution list.
- Fresh multi-upgrades offer level 1, non-max owned multi-upgrades offer the next level, and maxed multi-upgrades are omitted.
- Evolutions are offered only when all prerequisites are met and the unit has no selected evolution.
- Reroll is disabled when no alternate offer can be produced or the player cannot afford the current reroll cost.
- Reroll replaces the displayed pending offer without recording a selection.
- Closing the upgrade menu does not clear the pending offer.
- Selected evolutions persist through recall and redeploy as resolved `UpgradeSO` leaves.
- Focused multi-upgrade details show title, stat effects, current-level display, and related evolution hints.
- Focused evolution details show title, stat effects, target evolution icon, and prerequisite progress.
- Stat comparisons display `current >>> next` only when there is a comparable previous multi-upgrade level effect.
- Stat-only upgrades still update final tower stats and vision range.
- `DeploymentCost` upgrades update cached roster cost and deployment UI.
- Leveling a multi-upgrade replaces the previous level leaf instead of stacking it.
- Multiple override upgrades keep all selections recorded, with the latest override firing.
- Augment upgrades fire alongside the active primary weapon.
- Augments still fire after a later override upgrade.
- Earlier stat and modifier upgrades remain active after a later weapon override.
- Direct, debug/hitscan, and projectile attacks all trigger hit modifiers.
- Projectile modifiers affect newly fired projectiles without changing existing in-flight projectiles.
- Applied upgrades persist through recall and redeploy.
