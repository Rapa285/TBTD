using UnityEngine;

/// <summary>
/// Visual-only radial setup progress display for a tower footing sprite.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteRenderer))]
public sealed class TowerPreparationFootingDisplay : MonoBehaviour
{
    private const string RadialFillShaderName = "TBTD/Sprite Radial Fill";

    private static readonly int FillAmountId = Shader.PropertyToID("_FillAmount");
    private static readonly int FilledColorId = Shader.PropertyToID("_FilledColor");
    private static readonly int EmptyColorId = Shader.PropertyToID("_EmptyColor");
    private static readonly int FeatherId = Shader.PropertyToID("_Feather");
    private static readonly int ClockwiseId = Shader.PropertyToID("_Clockwise");
    private static readonly int StartAngleId = Shader.PropertyToID("_StartAngle");

    [SerializeField, Tooltip("Tower whose setup timer drives this footing. Defaults to the nearest parent TowerEntity.")]
    private TowerEntity tower;

    [SerializeField, Tooltip("Ground footing sprite to render as preparation progress. Defaults to this SpriteRenderer.")]
    private SpriteRenderer footingRenderer;

    [SerializeField, Tooltip("Color multiplied over the filled part of the sprite. White preserves the renderer tint.")]
    private Color filledColor = Color.white;

    [SerializeField, Tooltip("Color multiplied over the unfilled part of the sprite. Alpha dims the pending area.")]
    private Color emptyColor = new Color(1f, 1f, 1f, 0.18f);

    [SerializeField, Range(0f, 0.05f), Tooltip("Softness at the radial fill edge, expressed as normalized circle amount.")]
    private float edgeFeather = 0.01f;

    [SerializeField, Tooltip("When enabled, the footing fills clockwise from the top.")]
    private bool fillClockwise = true;

    [SerializeField, Range(-360f, 360f), Tooltip("Degrees offset from the top start position.")]
    private float startAngleDegrees;

    private Material originalMaterial;
    private Material radialMaterial;
    private bool capturedOriginalMaterial;

    private void Awake()
    {
        ResolveReferences();
    }

    private void OnEnable()
    {
        ResolveReferences();
    }

    private void LateUpdate()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        ResolveReferences();

        if (footingRenderer == null)
        {
            return;
        }

        if (!ShouldUseRadialMaterial())
        {
            RestoreOriginalMaterial();
            return;
        }

        if (!EnsureRadialMaterial())
        {
            return;
        }

        ApplyProgress(GetFillAmount());
    }

    private void OnDisable()
    {
        if (Application.isPlaying)
        {
            RestoreOriginalMaterial();
        }
    }

    private void OnDestroy()
    {
        if (radialMaterial == null)
        {
            return;
        }

        Destroy(radialMaterial);
        radialMaterial = null;
    }

    private void OnValidate()
    {
        ResolveReferences();
        edgeFeather = Mathf.Clamp(edgeFeather, 0f, 0.05f);
    }

    private void Reset()
    {
        ResolveReferences();
    }

    private void ResolveReferences()
    {
        if (footingRenderer == null)
        {
            footingRenderer = GetComponent<SpriteRenderer>();
        }

        if (tower == null)
        {
            tower = GetComponentInParent<TowerEntity>();
        }
    }

    private bool ShouldUseRadialMaterial()
    {
        return tower == null || tower.Deployed;
    }

    private float GetFillAmount()
    {
        if (tower == null || !tower.Deployed || tower.SetupTimeDuration <= 0f || !tower.IsInSetupTime)
        {
            return 1f;
        }

        return 1f - tower.SetupTimeNormalizedRemaining;
    }

    private bool EnsureRadialMaterial()
    {
        CaptureOriginalMaterialIfNeeded();

        if (radialMaterial == null)
        {
            Shader shader = Shader.Find(RadialFillShaderName);
            if (shader == null)
            {
                return false;
            }

            radialMaterial = new Material(shader)
            {
                name = $"{RadialFillShaderName} Instance"
            };
        }

        if (footingRenderer.sharedMaterial != radialMaterial)
        {
            footingRenderer.sharedMaterial = radialMaterial;
        }

        return true;
    }

    private void CaptureOriginalMaterialIfNeeded()
    {
        if (capturedOriginalMaterial || footingRenderer == null)
        {
            return;
        }

        originalMaterial = footingRenderer.sharedMaterial;
        capturedOriginalMaterial = true;
    }

    private void RestoreOriginalMaterial()
    {
        if (!capturedOriginalMaterial || footingRenderer == null || footingRenderer.sharedMaterial == originalMaterial)
        {
            return;
        }

        footingRenderer.sharedMaterial = originalMaterial;
    }

    private void ApplyProgress(float fillAmount)
    {
        radialMaterial.SetFloat(FillAmountId, Mathf.Clamp01(fillAmount));
        radialMaterial.SetColor(FilledColorId, filledColor);
        radialMaterial.SetColor(EmptyColorId, emptyColor);
        radialMaterial.SetFloat(FeatherId, edgeFeather);
        radialMaterial.SetFloat(ClockwiseId, fillClockwise ? 1f : 0f);
        radialMaterial.SetFloat(StartAngleId, startAngleDegrees);
    }
}
