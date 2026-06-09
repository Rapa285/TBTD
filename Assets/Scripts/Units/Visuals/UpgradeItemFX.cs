using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// DOTween hover/focus scale and sheen reveal effect for one upgrade choice item.
/// </summary>
[RequireComponent(typeof(UpgradeChoiceItem))]
public class UpgradeItemFX : MonoBehaviour
{
    private const string AnimProgressProperty = "_AnimProgress";

    [SerializeField, Tooltip("Transform scaled by this effect. Defaults to this transform.")]
    private Transform target;

    [SerializeField, Tooltip("Local scale multiplier applied while hovered or focused.")]
    private Vector3 hoverScale = Vector3.one * 1.06f;

    [SerializeField, Min(0f), Tooltip("Tween duration in seconds.")]
    private float duration = 0.12f;

    [SerializeField, Tooltip("Ease used when scaling up on hover or focus.")]
    private Ease hoverEase = Ease.OutQuad;

    [SerializeField, Tooltip("Ease used when returning to the base scale.")]
    private Ease normalEase = Ease.OutQuad;

    [SerializeField, Tooltip("Mask RectTransform revealed by animating its right edge from hidden to full width.")]
    private RectTransform maskRectTransform;

    [SerializeField, Tooltip("Color sheen graphic using the SheenUIColor material.")]
    private Graphic colorGraphic;

    [SerializeField, Min(0f), Tooltip("Duration in seconds for the sheen reveal animation.")]
    private float animTime = 0.45f;

    [SerializeField, Tooltip("Curve used to map normalized animation time into the reveal progress.")]
    private AnimationCurve animGraph = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    private UpgradeChoiceItem choiceItem;
    private Tween scaleTween;
    private Tween revealTween;
    private Material colorMaterialInstance;
    private float maskFullRightOffset;
    private bool capturedMaskFullRightOffset;
    private Vector3 baseScale;
    private bool subscribed;

    public bool IsRevealAnimationFinished { get; private set; } = true;

    private void Awake()
    {
        ResolveReferences();
        CaptureBaseScale();
        CaptureMaskFullRightOffset();
        EnsureMaterialInstance(colorGraphic, ref colorMaterialInstance);
        ApplyRevealProgress(1f);
    }

    private void OnEnable()
    {
        ResolveReferences();
        CaptureBaseScale();
        CaptureMaskFullRightOffset();
        EnsureMaterialInstance(colorGraphic, ref colorMaterialInstance);
        Subscribe();
    }

    private void OnDisable()
    {
        Unsubscribe();
        KillRevealTween();
        IsRevealAnimationFinished = true;
        ResetScaleImmediate();
    }

    private void OnDestroy()
    {
        Unsubscribe();
        KillTween();
        KillRevealTween();
        DestroyMaterialInstance(ref colorMaterialInstance);
    }

    private void OnValidate()
    {
        ResolveReferences();
        duration = Mathf.Max(0f, duration);
        animTime = Mathf.Max(0f, animTime);

        if (animGraph == null)
        {
            animGraph = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        }
    }

    private void HandleHovered(bool hovered)
    {
        if (target == null)
        {
            return;
        }

        Vector3 targetScale = hovered
            ? new Vector3(baseScale.x * hoverScale.x, baseScale.y * hoverScale.y, baseScale.z * hoverScale.z)
            : baseScale;

        KillTween();

        if (duration <= 0f)
        {
            target.localScale = targetScale;
            return;
        }

        scaleTween = target.DOScale(targetScale, duration)
            .SetEase(hovered ? hoverEase : normalEase)
            .SetUpdate(true)
            .SetTarget(target);
    }

    private void ResolveReferences()
    {
        if (choiceItem == null)
        {
            choiceItem = GetComponent<UpgradeChoiceItem>();
        }

        if (target == null)
        {
            target = transform;
        }

        if (maskRectTransform == null)
        {
            Transform maskTransform = transform.Find("Mask");
            if (maskTransform != null)
            {
                maskRectTransform = maskTransform.GetComponent<RectTransform>();
            }
        }

        if (colorGraphic == null)
        {
            Transform sheenTransform = transform.Find("Mask/Sheen");
            if (sheenTransform != null)
            {
                colorGraphic = sheenTransform.GetComponent<Graphic>();
            }
        }
    }

    private void CaptureBaseScale()
    {
        if (target != null)
        {
            baseScale = target.localScale;
        }
    }

    private void CaptureMaskFullRightOffset()
    {
        if (!capturedMaskFullRightOffset && maskRectTransform != null)
        {
            maskFullRightOffset = maskRectTransform.offsetMax.x;
            capturedMaskFullRightOffset = true;
        }
    }

    private void Subscribe()
    {
        if (subscribed || choiceItem == null)
        {
            return;
        }

        choiceItem.OnHovered.AddListener(HandleHovered);
        subscribed = true;
    }

    private void Unsubscribe()
    {
        if (!subscribed || choiceItem == null)
        {
            return;
        }

        choiceItem.OnHovered.RemoveListener(HandleHovered);
        subscribed = false;
    }

    private void ResetScaleImmediate()
    {
        KillTween();

        if (target != null)
        {
            target.localScale = baseScale;
        }
    }

    private void KillTween()
    {
        if (scaleTween == null)
        {
            return;
        }

        scaleTween.Kill();
        scaleTween = null;
    }

    public void PrepareRevealAnimation()
    {
        EnsureMaterialInstance(colorGraphic, ref colorMaterialInstance);
        KillRevealTween();
        IsRevealAnimationFinished = false;
        ApplyRevealProgress(EvaluateAnimGraph(0f));
    }

    public void PlayRevealAnimation(float delay)
    {
        EnsureMaterialInstance(colorGraphic, ref colorMaterialInstance);
        KillRevealTween();
        IsRevealAnimationFinished = false;

        float startValue = EvaluateAnimGraph(0f);
        float endValue = EvaluateAnimGraph(1f);
        ApplyRevealProgress(startValue);

        if (animTime <= 0f)
        {
            ApplyRevealProgress(endValue);
            IsRevealAnimationFinished = true;
            return;
        }

        revealTween = DOTween.To(
                () => 0f,
                normalizedTime => ApplyRevealProgress(EvaluateAnimGraph(normalizedTime)),
                1f,
                animTime)
            .SetEase(Ease.Linear)
            .SetDelay(Mathf.Max(0f, delay))
            .SetUpdate(true)
            .OnComplete(() => IsRevealAnimationFinished = true)
            .SetTarget(this);
    }

    private float EvaluateAnimGraph(float normalizedTime)
    {
        if (animGraph == null)
        {
            return normalizedTime;
        }

        return animGraph.Evaluate(normalizedTime);
    }

    private void ApplyRevealProgress(float progress)
    {
        progress = Mathf.Clamp01(progress);
        SetMaskRevealProgress(progress);
        SetAnimProgress(colorMaterialInstance, progress);
    }

    private void SetMaskRevealProgress(float progress)
    {
        if (maskRectTransform == null)
        {
            return;
        }

        float hiddenRightOffset = maskFullRightOffset - GetMaskRevealWidth();
        Vector2 offsetMax = maskRectTransform.offsetMax;
        offsetMax.x = Mathf.Lerp(hiddenRightOffset, maskFullRightOffset, progress);
        maskRectTransform.offsetMax = offsetMax;
    }

    private void SetAnimProgress(Material material, float value)
    {
        if (material == null || !material.HasProperty(AnimProgressProperty))
        {
            return;
        }

        material.SetFloat(AnimProgressProperty, value);
    }

    private float GetMaskRevealWidth()
    {
        RectTransform parentRectTransform = maskRectTransform.parent as RectTransform;
        if (parentRectTransform != null && parentRectTransform.rect.width > 0f)
        {
            return parentRectTransform.rect.width;
        }

        if (maskRectTransform.rect.width > 0f)
        {
            return maskRectTransform.rect.width;
        }

        return Mathf.Max(0f, maskRectTransform.sizeDelta.x);
    }

    private void EnsureMaterialInstance(Graphic graphic, ref Material materialInstance)
    {
        if (graphic == null || materialInstance != null)
        {
            return;
        }

        Material sourceMaterial = graphic.material;
        if (sourceMaterial == null || !sourceMaterial.HasProperty(AnimProgressProperty))
        {
            return;
        }

        materialInstance = new Material(sourceMaterial)
        {
            name = sourceMaterial.name + " (Runtime)",
            hideFlags = HideFlags.DontSave
        };
        graphic.material = materialInstance;
    }

    private void KillRevealTween()
    {
        if (revealTween == null)
        {
            return;
        }

        revealTween.Kill();
        revealTween = null;
    }

    private void DestroyMaterialInstance(ref Material materialInstance)
    {
        if (materialInstance == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(materialInstance);
        }
        else
        {
            DestroyImmediate(materialInstance);
        }

        materialInstance = null;
    }
}
