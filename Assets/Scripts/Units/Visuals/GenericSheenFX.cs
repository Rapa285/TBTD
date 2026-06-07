using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Plays a standalone UI sheen reveal when enabled and resets it when disabled.
/// </summary>
public class GenericSheenFX : MonoBehaviour
{
    private const string AnimProgressProperty = "_AnimProgress";

    [SerializeField, Tooltip("Mask RectTransform revealed by animating its right edge from hidden to full width.")]
    private RectTransform maskRectTransform;

    [SerializeField, Tooltip("Color sheen graphic using the SheenUIColor material.")]
    private Graphic colorGraphic;

    [SerializeField, Min(0f), Tooltip("Duration in seconds for the sheen reveal animation.")]
    private float animTime = 0.45f;

    [SerializeField, Tooltip("Curve used to map normalized animation time into the reveal progress.")]
    private AnimationCurve animGraph = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    private Tween sheenTween;
    private Material colorMaterialInstance;
    private float maskFullRightOffset;
    private bool capturedMaskFullRightOffset;

    private void Awake()
    {
        ResolveReferences();
        CaptureMaskFullRightOffset();
        EnsureMaterialInstance(colorGraphic, ref colorMaterialInstance);
        ApplyRevealProgress(0f);
    }

    private void OnEnable()
    {
        ResolveReferences();
        CaptureMaskFullRightOffset();
        EnsureMaterialInstance(colorGraphic, ref colorMaterialInstance);
        PlaySheenAnimation();
    }

    private void OnDisable()
    {
        KillTween();
        ApplyRevealProgress(0f);
    }

    private void OnDestroy()
    {
        KillTween();
        DestroyMaterialInstance(ref colorMaterialInstance);
    }

    private void OnValidate()
    {
        ResolveReferences();
        animTime = Mathf.Max(0f, animTime);

        if (animGraph == null)
        {
            animGraph = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        }
    }

    private void PlaySheenAnimation()
    {
        KillTween();

        float startValue = EvaluateAnimGraph(0f);
        float endValue = EvaluateAnimGraph(1f);
        ApplyRevealProgress(startValue);

        if (animTime <= 0f)
        {
            ApplyRevealProgress(endValue);
            return;
        }

        sheenTween = DOTween.To(
                () => 0f,
                normalizedTime => ApplyRevealProgress(EvaluateAnimGraph(normalizedTime)),
                1f,
                animTime)
            .SetEase(Ease.Linear)
            .SetUpdate(true)
            .SetTarget(this);
    }

    private void ResolveReferences()
    {
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

    private void CaptureMaskFullRightOffset()
    {
        if (!capturedMaskFullRightOffset && maskRectTransform != null)
        {
            maskFullRightOffset = maskRectTransform.offsetMax.x;
            capturedMaskFullRightOffset = true;
        }
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

    private void KillTween()
    {
        if (sheenTween == null)
        {
            return;
        }

        sheenTween.Kill();
        sheenTween = null;
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
