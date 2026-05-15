using System;
using DG.Tweening;
using UnityEngine;

/// <summary>
/// Slide open/close effect for the selected unit detail panel.
/// </summary>
public class UnitDetailUIFX : MonoBehaviour
{
    [SerializeField, Tooltip("RectTransform animated by this effect. Defaults to this object's RectTransform.")]
    private RectTransform target;

    [SerializeField, Tooltip("Root object activated before opening and deactivated after closing. Defaults to the target object.")]
    private GameObject root;

    [SerializeField, Min(0f), Tooltip("Duration in seconds for the opening animation.")]
    private float openTime = 0.25f;

    [SerializeField, Min(0f), Tooltip("Duration in seconds for the closing animation.")]
    private float closeTime = 0.2f;

    [SerializeField, Tooltip("Curve used while opening from hidden X to shown X.")]
    private AnimationCurve openCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [SerializeField, Tooltip("Curve used while closing from shown X to hidden X.")]
    private AnimationCurve closeCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    private Tween positionTween;
    private bool isHidden;
    private bool isShowing;
    private Action pendingHiddenCallback;

    private float ShownX => 0f;
    public bool IsHidden => isHidden;

    private void Awake()
    {
        ResolveReferences();
        isHidden = root != null && !root.activeSelf;
    }

    private void OnEnable()
    {
        ResolveReferences();
    }

    private void OnDisable()
    {
        KillTween();
    }

    private void OnDestroy()
    {
        KillTween();
    }

    private void OnValidate()
    {
        ResolveReferences();
        openTime = Mathf.Max(0f, openTime);
        closeTime = Mathf.Max(0f, closeTime);

        if (openCurve == null)
        {
            openCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        }

        if (closeCurve == null)
        {
            closeCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        }
    }

    public void Show()
    {
        if (isShowing)
        {
            return;
        }

        isShowing = true;
        ResolveReferences();
        KillTween();
        bool wasHidden = isHidden || (root != null && !root.activeSelf);

        if (root != null && !root.activeSelf)
        {
            root.SetActive(true);
        }

        if (target == null)
        {
            isHidden = false;
            isShowing = false;
            return;
        }

        if (!isHidden && Mathf.Approximately(target.anchoredPosition.x, ShownX))
        {
            isShowing = false;
            return;
        }

        float startX = wasHidden ? GetHiddenX() : target.anchoredPosition.x;
        SetAnchoredX(startX);
        isHidden = false;

        if (openTime <= 0f)
        {
            SetAnchoredX(ShownX);
            isShowing = false;
            return;
        }

        positionTween = DOTween.To(
                () => 0f,
                progress => SetAnchoredX(Mathf.Lerp(startX, ShownX, Evaluate(openCurve, progress))),
                1f,
                openTime)
            .SetEase(Ease.Linear)
            .SetUpdate(true)
            .OnComplete(() => isShowing = false)
            .SetTarget(this);
    }

    public void Hide(Action onHidden = null)
    {
        ResolveReferences();
        KillTween();
        isShowing = false;
        pendingHiddenCallback = onHidden;

        if (target == null)
        {
            CompleteHide();
            return;
        }

        float startX = target.anchoredPosition.x;
        float hiddenX = GetHiddenX();

        if (closeTime <= 0f)
        {
            SetAnchoredX(hiddenX);
            CompleteHide();
            return;
        }

        positionTween = DOTween.To(
                () => 0f,
                progress => SetAnchoredX(Mathf.Lerp(startX, hiddenX, Evaluate(closeCurve, progress))),
                1f,
                closeTime)
            .SetEase(Ease.Linear)
            .SetUpdate(true)
            .OnComplete(CompleteHide)
            .SetTarget(this);
    }

    private void CompleteHide()
    {
        isHidden = true;
        Action callback = pendingHiddenCallback;
        pendingHiddenCallback = null;

        if (root != null && root.activeSelf)
        {
            root.SetActive(false);
        }

        callback?.Invoke();
    }

    private void ResolveReferences()
    {
        if (target == null)
        {
            target = GetComponent<RectTransform>();
        }

        if (root == null && target != null)
        {
            root = target.gameObject;
        }

        if (root == null)
        {
            root = gameObject;
        }
    }

    private float GetHiddenX()
    {
        if (target == null)
        {
            return ShownX;
        }

        return GetTargetWidth();
    }

    private float GetTargetWidth()
    {
        if (target.rect.width > 0f)
        {
            return target.rect.width;
        }

        return Mathf.Max(0f, target.sizeDelta.x);
    }

    private void SetAnchoredX(float x)
    {
        if (target == null)
        {
            return;
        }

        Vector2 anchoredPosition = target.anchoredPosition;
        anchoredPosition.x = x;
        target.anchoredPosition = anchoredPosition;
    }

    private static float Evaluate(AnimationCurve curve, float progress)
    {
        return curve != null ? curve.Evaluate(progress) : progress;
    }

    private void KillTween()
    {
        if (positionTween == null)
        {
            return;
        }

        positionTween.Kill();
        positionTween = null;
        pendingHiddenCallback = null;
    }
}
