using System.Collections.Generic;
using UnityEngine;

public class MaterialOverrider : MonoBehaviour
{
    private struct RendererMaterialState
    {
        public Renderer renderer;
        public Material[] originalMaterials;
    }

    [SerializeField] private Material neutralPreviewMaterial;
    [SerializeField] private Material validPlacementMaterial;
    [SerializeField] private Material invalidPlacementMaterial;

    private readonly List<RendererMaterialState> rendererStates = new List<RendererMaterialState>();
    private bool initialized;

    private void Awake()
    {
        CacheRenderers();
    }

    private void OnEnable()
    {
        CacheRenderers();
    }

    public void ShowNeutralPreview()
    {
        ApplyMaterial(neutralPreviewMaterial);
    }

    public void ShowValidPlacement()
    {
        ApplyMaterial(validPlacementMaterial);
    }

    public void ShowInvalidPlacement()
    {
        ApplyMaterial(invalidPlacementMaterial);
    }

    public void RestoreOriginalMaterials()
    {
        CacheRenderers();

        for (int i = 0; i < rendererStates.Count; i++)
        {
            RendererMaterialState state = rendererStates[i];
            if (state.renderer == null)
            {
                continue;
            }

            state.renderer.sharedMaterials = state.originalMaterials;
        }
    }

    private void ApplyMaterial(Material material)
    {
        if (material == null)
        {
            return;
        }

        CacheRenderers();

        for (int i = 0; i < rendererStates.Count; i++)
        {
            RendererMaterialState state = rendererStates[i];
            if (state.renderer == null)
            {
                continue;
            }

            int materialCount = Mathf.Max(1, state.originalMaterials.Length);
            Material[] replacementMaterials = new Material[materialCount];
            for (int materialIndex = 0; materialIndex < replacementMaterials.Length; materialIndex++)
            {
                replacementMaterials[materialIndex] = material;
            }

            state.renderer.sharedMaterials = replacementMaterials;
        }
    }

    private void CacheRenderers()
    {
        if (initialized)
        {
            return;
        }

        rendererStates.Clear();

        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null || renderer is LineRenderer)
            {
                continue;
            }

            rendererStates.Add(new RendererMaterialState
            {
                renderer = renderer,
                originalMaterials = renderer.sharedMaterials
            });
        }

        initialized = true;
    }
}
