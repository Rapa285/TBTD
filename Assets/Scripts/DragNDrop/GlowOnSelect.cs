using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Shows and animates one roster-card UI root while this managed unit is selected.
/// </summary>
[RequireComponent(typeof(UIUnitItem))]
public class GlowOnSelect : UnitUIBehaviour
{
    [SerializeField, Tooltip("Player selection source. Uses the scene service locator when empty.")]
    private PlayerStateController playerStateController;

    [SerializeField, Tooltip("Root object shown while this roster unit is selected.")]
    private GameObject selectedRoot;

    [SerializeField, Tooltip("Image whose color cycles while this roster unit is selected.")]
    private Image glowImage;

    [SerializeField, Tooltip("First color in the selected glow cycle.")]
    private Color colorA = Color.white;

    [SerializeField, Tooltip("Second color in the selected glow cycle.")]
    private Color colorB = Color.cyan;

    [SerializeField, Min(0.01f), Tooltip("Seconds for one half-cycle between the two glow colors.")]
    private float cycleDuration = 0.45f;

    [SerializeField, Tooltip("Use unscaled time so selection glow continues while gameplay time is paused.")]
    private bool useUnscaledTime = true;

    private Tween glowTween;
    private bool playerStateSubscribed;
    private bool isVisible;
    private bool warnedRootIsSelf;

    protected override void Start()
    {
        base.Start();
        SubscribeToPlayerStateIfNeeded();
        RefreshSelectionState();
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        SubscribeToPlayerStateIfNeeded();
        RefreshSelectionState();
    }

    private void Update()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        if (!playerStateSubscribed)
        {
            SubscribeToPlayerStateIfNeeded();
            RefreshSelectionState();
        }
    }

    protected override void OnDisable()
    {
        SetSelectedVisible(false);
        UnsubscribeFromPlayerStateIfNeeded();
        base.OnDisable();
    }

    protected override void OnDestroy()
    {
        KillGlowTween();
        UnsubscribeFromPlayerStateIfNeeded();
        base.OnDestroy();
    }

    protected override void OnValidate()
    {
        base.OnValidate();
        cycleDuration = Mathf.Max(0.01f, cycleDuration);
        ResolveGlowReferences();

        if (!Application.isPlaying)
        {
            SetRootActive(false);
        }
    }

    protected override void ResolveReferences()
    {
        base.ResolveReferences();

        if (playerStateController == null)
        {
            ServiceLocator.TryResolve(out playerStateController);
        }

        ResolveGlowReferences();
    }

    private void HandleSelectionChanged(PlayerSelectionChangedEvent eventData)
    {
        RefreshSelectionState();
    }

    private void RefreshSelectionState()
    {
        ResolveReferences();
        SetSelectedVisible(IsThisUnitSelected());
    }

    private bool IsThisUnitSelected()
    {
        return playerStateController != null
            && UnitItem != null
            && UnitItem.IsManagedUnit
            && !string.IsNullOrWhiteSpace(UnitItem.UnitId)
            && UnitItem.UnitId == playerStateController.SelectedUnitId;
    }

    private void SetSelectedVisible(bool visible)
    {
        if (isVisible == visible)
        {
            SetRootActive(visible);

            if (visible && glowTween == null)
            {
                StartGlowTween();
            }
            else if (!visible)
            {
                StopGlowTween();
            }

            return;
        }

        isVisible = visible;
        SetRootActive(visible);

        if (visible)
        {
            StartGlowTween();
        }
        else
        {
            StopGlowTween();
        }
    }

    private void StartGlowTween()
    {
        ResolveGlowReferences();
        KillGlowTween();

        if (glowImage == null)
        {
            return;
        }

        glowImage.color = colorA;
        glowTween = glowImage
            .DOColor(colorB, cycleDuration)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo)
            .SetUpdate(useUnscaledTime)
            .SetTarget(this);
    }

    private void StopGlowTween()
    {
        KillGlowTween();

        if (glowImage != null)
        {
            glowImage.color = colorA;
        }
    }

    private void KillGlowTween()
    {
        if (glowTween == null)
        {
            return;
        }

        glowTween.Kill();
        glowTween = null;
    }

    private void SubscribeToPlayerStateIfNeeded()
    {
        if (playerStateSubscribed)
        {
            return;
        }

        if (playerStateController == null)
        {
            ResolveReferences();
        }

        if (playerStateController == null)
        {
            return;
        }

        playerStateController.SelectionChanged += HandleSelectionChanged;
        playerStateSubscribed = true;
    }

    private void UnsubscribeFromPlayerStateIfNeeded()
    {
        if (!playerStateSubscribed || playerStateController == null)
        {
            playerStateSubscribed = false;
            return;
        }

        playerStateController.SelectionChanged -= HandleSelectionChanged;
        playerStateSubscribed = false;
    }

    private void ResolveGlowReferences()
    {
        if (selectedRoot == null && glowImage != null && glowImage.gameObject != gameObject)
        {
            selectedRoot = glowImage.gameObject;
        }

        if (glowImage == null && selectedRoot != null)
        {
            glowImage = selectedRoot.GetComponentInChildren<Image>(true);
        }
    }

    private void SetRootActive(bool active)
    {
        if (selectedRoot == null)
        {
            return;
        }

        if (selectedRoot == gameObject)
        {
            WarnRootIsSelf();
            return;
        }

        if (selectedRoot.activeSelf != active)
        {
            selectedRoot.SetActive(active);
        }
    }

    private void WarnRootIsSelf()
    {
        if (warnedRootIsSelf)
        {
            return;
        }

        warnedRootIsSelf = true;
        Debug.LogWarning(
            $"{nameof(GlowOnSelect)} on '{name}' cannot hide its own GameObject. Assign a child indicator root instead.",
            this);
    }
}
