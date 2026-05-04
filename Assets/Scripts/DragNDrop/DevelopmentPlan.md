# Drag N Drop Development And Integration Plan

## Summary
Build the deployment flow around the existing tower/combat system: `UnitDeploymentController` manages the drag lifecycle, `UnitDeploymentChecker` owns placement validation, `MaterialOverrider` owns preview feedback, currency is enforced for roster-managed units, and `TowerEntity` gets a deployment gate without creating a parallel tower runtime.

Existing scene-placed towers stay active by default. Drag previews are explicitly prepared as undeployed instances, then converted into real deployed towers when placement succeeds. Managed units use cached deployment cost for pre-preview affordability checks and spend currency only on final placement.

## Public API / Interfaces
- `TowerEntity`
  - Serialized `deployed = true` with public `bool Deployed`.
  - `PrepareForDeploymentPreview()` prepares controller-created previews.
  - `Deploy()` activates combat, clears stale targeting, reapplies final vision range, and starts `SetupTime` from the deployment moment.
  - `Update()` exits early while undeployed.
- `UnitVision`
  - `ClearTargets()` clears stale vision targets.
- `UnitDeploymentController`
  - Public entrypoint: `bool BeginDeployment(TowerEntity towerPrefab)`.
  - Public entrypoint: `bool BeginDeployment(UnitStateManager stateManager, string unitId)`.
  - Public state: `bool IsDragging`, `TowerEntity CurrentDraggedTower`.
  - Future UI should call `BeginDeployment` from pointer-down/begin-drag, not normal button click, because primary mouse release attempts placement.
- `UnitDeploymentChecker`
  - Public method: `bool TryGetPlacement(Vector2 screenPosition, out PlacementResult result)`.
  - `PlacementResult` contains `hasGround`, `isValid`, `position`, `normal`, and `groundCollider`.
  - Exposes `UnityEvent` callbacks for entering valid and invalid placement states.
- `MaterialOverrider`
  - Public methods: `ShowNeutralPreview()`, `ShowValidPlacement()`, `ShowInvalidPlacement()`, `RestoreOriginalMaterials()`.
- `UnitUIDeployment`
  - Public state: `DeploymentUIState CurrentState`.
  - Display states are `CannotDeploy`, `CanDeploy`, and `InDeployPreview`.
- `UnitEventBus`
  - Raises deployment preview started/ended events, cost compiled events, and currency changed events used by deployment UI.
- `CurrencyManager`
  - Public methods: `CanAfford(int)`, `TrySpend(int)`, `AddCurrency(int)`, `DebugAddCurrency(int)`, and `DebugRemoveCurrency(int)`.

## Implementation Changes
- `TowerEntity`
  - `Awake()` remains responsible for reference discovery and stat compilation.
  - `Start()` initializes attack timers only when `deployed == true`.
  - `PrepareForDeploymentPreview()` sets `deployed = false`, clears `currentTarget`, clears `UnitVision`, and prevents attacks while dragging.
  - `Deploy()` sets `deployed = true`, clears targeting, applies `VisualRange`, then sets `activeAfterTime = Time.time + SetupTime` and `nextAttackTime = activeAfterTime`.
  - Upgrade/stat formulas, attack behavior APIs, weapon override/augment behavior, and projectile modifiers remain owned by the tower/combat system.

- `UnitDeploymentChecker`
  - Uses inspector-assigned masks for `Ground` raycast surfaces and blocking layers such as `Units`.
  - Uses `Camera.main` as fallback when no camera is assigned.
  - Raycasts from cursor screen position to `groundLayers`; no ground hit is invalid placement.
  - Validates overlap with a serialized capsule footprint: `placementRadius`, `placementHeight`, and `verticalOffset`.
  - Uses `Physics.CheckCapsule(..., blockingLayers, QueryTriggerInteraction.Ignore)` so tower vision triggers do not block placement.
  - Tracks validity transitions and invokes valid/invalid events only when the state changes.

- `UnitDeploymentController`
  - Holds one active preview at a time and rejects new deployment attempts while dragging.
  - Managed deployment checks `UnitStateManager.TryGetDeploymentCost(...)` before preview instantiation when cached cost exists.
  - Missing cached cost logs a warning and falls back to instantiated preview cost lookup.
  - Missing `CurrencyManager` logs a warning and skips currency enforcement.
  - Instantiates the `TowerEntity` prefab once at drag start; the preview object becomes the deployed tower on success.
  - Every update while dragging reads `Mouse.current.position`, asks `UnitDeploymentChecker` for placement, moves the preview to the returned ground point, and updates `MaterialOverrider`.
  - Primary mouse release attempts deployment: valid placement spends roster currency when applicable, binds/deploys the unit, and invalid placement cancels and destroys the preview.
  - Right mouse press cancels and destroys the preview.
  - Raises `UnitDeploymentPreviewStarted` after preview setup succeeds and `UnitDeploymentPreviewEnded` before preview identity is cleared.
  - Missing mouse or checker references fail gracefully without throwing.
  - Managed-unit deployment applies saved upgrades after preview preparation and before placement so range, override weapons, augment weapons, and projectile modifiers match the stored unit state after placement.

- `UnitUIDeployment`
  - Uses `CanBeginDeployment()` as input gating and keeps `IsDragging` there so a second deployment cannot start.
  - Evaluates display state separately so the deployable indicator remains visible for the same unit while in deployment preview.
  - Refreshes from currency, cost compiled, deployment, recall, cooldown, and preview lifecycle events.

- `UnitUICost`
  - Displays cached roster deployment cost in TMP text.
  - Hides for direct prefab items, missing cost, or deployed roster units.
  - Colors text by affordability when a `CurrencyManager` exists.

- `UICurrencyDisplayer`
  - Displays current player currency and refreshes on `CurrencyChanged`.

- `MaterialOverrider`
  - Auto-caches child `MeshRenderer` and `SpriteRenderer` components by caching all renderers except `LineRenderer`.
  - Caches original shared material arrays on initialization.
  - Applies neutral/valid/invalid preview materials to all cached renderers.
  - Restores original materials before successful deployment and before cancellation destruction.
  - If preview materials are unassigned, that visual state is skipped without error.

## Test Plan
- Run `dotnet build Assembly-CSharp.csproj` after script changes.
- Manual Unity checks:
  - Existing `UnitTest` scene tower still attacks without needing new Inspector edits.
  - Starting deployment creates one preview that follows the cursor over `Ground`.
  - Valid placement shows valid material and deploys on primary release.
  - Invalid placement shows invalid material and cancels on primary release.
  - Right click cancels deployment and destroys the preview.
  - Preview does not attack while dragged.
  - `SetupTime` begins after successful deployment, not when the preview is instantiated.
  - Blocking layers reject placement, while trigger colliders such as `UnitVision` do not.
  - Roster unit cost blocks preview before instantiation when cached cost is available and unaffordable.
  - Roster currency is deducted only after valid placement succeeds.
  - Deployable indicator remains visible while that unit is in deployment preview and hides after successful deployment.
  - Cost display hides while a roster unit is deployed.

## Assumptions
- Uses Unity's Input System via `UnityEngine.InputSystem.Mouse.current`; `InputSystem_Actions.inputactions` is unchanged.
- Placement is freeform on ground surfaces, not grid-snapped.
- Footprint is inspector-configured serialized radius/height, not inferred from prefab colliders.
- Upgrade selection UI is handled by the separate event-bus upgrade UI flow; drag/drop UI only starts deployment.
- Deployment economy is limited to roster-managed unit cost. Direct prefab deployment remains free.
