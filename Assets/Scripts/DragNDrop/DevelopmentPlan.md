# Drag N Drop Development And Integration Plan

## Summary
Build the deployment flow around the existing tower/combat system: `UnitDeploymentController` manages the drag lifecycle, `UnitDeploymentChecker` owns placement validation, `MaterialOverrider` owns preview feedback, and `TowerEntity` gets a deployment gate without creating a parallel tower runtime.

Existing scene-placed towers stay active by default. Drag previews are explicitly prepared as undeployed instances, then converted into real deployed towers when placement succeeds.

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
  - Public state: `bool IsDragging`, `TowerEntity CurrentDraggedTower`.
  - Future UI should call `BeginDeployment` from pointer-down/begin-drag, not normal button click, because primary mouse release attempts placement.
- `UnitDeploymentChecker`
  - Public method: `bool TryGetPlacement(Vector2 screenPosition, out PlacementResult result)`.
  - `PlacementResult` contains `hasGround`, `isValid`, `position`, `normal`, and `groundCollider`.
  - Exposes `UnityEvent` callbacks for entering valid and invalid placement states.
- `MaterialOverrider`
  - Public methods: `ShowNeutralPreview()`, `ShowValidPlacement()`, `ShowInvalidPlacement()`, `RestoreOriginalMaterials()`.

## Implementation Changes
- `TowerEntity`
  - `Awake()` remains responsible for reference discovery and stat compilation.
  - `Start()` initializes attack timers only when `deployed == true`.
  - `PrepareForDeploymentPreview()` sets `deployed = false`, clears `currentTarget`, clears `UnitVision`, and prevents attacks while dragging.
  - `Deploy()` sets `deployed = true`, clears targeting, applies `VisualRange`, then sets `activeAfterTime = Time.time + SetupTime` and `nextAttackTime = activeAfterTime`.
  - Upgrade/stat formulas, attack behavior APIs, and weapon override/augment behavior remain unchanged.

- `UnitDeploymentChecker`
  - Uses inspector-assigned masks for `Ground` raycast surfaces and blocking layers such as `Units`.
  - Uses `Camera.main` as fallback when no camera is assigned.
  - Raycasts from cursor screen position to `groundLayers`; no ground hit is invalid placement.
  - Validates overlap with a serialized capsule footprint: `placementRadius`, `placementHeight`, and `verticalOffset`.
  - Uses `Physics.CheckCapsule(..., blockingLayers, QueryTriggerInteraction.Ignore)` so tower vision triggers do not block placement.
  - Tracks validity transitions and invokes valid/invalid events only when the state changes.

- `UnitDeploymentController`
  - Holds one active preview at a time and rejects new deployment attempts while dragging.
  - Instantiates the `TowerEntity` prefab once at drag start; the preview object becomes the deployed tower on success.
  - Every update while dragging reads `Mouse.current.position`, asks `UnitDeploymentChecker` for placement, moves the preview to the returned ground point, and updates `MaterialOverrider`.
  - Primary mouse release attempts deployment: valid placement calls `Deploy()`, invalid placement cancels and destroys the preview.
  - Right mouse press cancels and destroys the preview.
  - Missing mouse or checker references fail gracefully without throwing.

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

## Assumptions
- Uses Unity's Input System via `UnityEngine.InputSystem.Mouse.current`; `InputSystem_Actions.inputactions` is unchanged.
- Placement is freeform on ground surfaces, not grid-snapped.
- Footprint is inspector-configured serialized radius/height, not inferred from prefab colliders.
- No economy, cooldown, unit inventory, upgrade UI, or weapon behavior changes are included.
- UI implementation is out of scope except for the controller entrypoint future UI will call.
