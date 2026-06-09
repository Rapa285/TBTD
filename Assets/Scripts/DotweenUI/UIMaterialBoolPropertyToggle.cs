using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Drives one boolean-like material property on a UI Graphic without mutating the shared material asset.
/// </summary>
public sealed class UIMaterialBoolPropertyToggle : MonoBehaviour
{
    [SerializeField, Tooltip("UI graphic whose material contains the boolean property.")]
    private Graphic targetGraphic;

    [SerializeField, Tooltip("Shader property written as 1 when true and 0 when false.")]
    private string propertyName = "_Enabled";

    private Material materialInstance;

    public void SetValue(bool value)
    {
        ResolveReferences();

        if (string.IsNullOrWhiteSpace(propertyName))
        {
            return;
        }

        Material material = GetWritableMaterial();
        if (material == null || !material.HasProperty(propertyName))
        {
            return;
        }

        material.SetFloat(propertyName, value ? 1f : 0f);
    }

    private void Awake()
    {
        ResolveReferences();
    }

    private void OnValidate()
    {
        ResolveReferences();
    }

    private void OnDestroy()
    {
        DestroyMaterialInstance();
    }

    private void ResolveReferences()
    {
        if (targetGraphic == null)
        {
            targetGraphic = GetComponent<Graphic>();
        }
    }

    private Material GetWritableMaterial()
    {
        if (targetGraphic == null)
        {
            return null;
        }

        if (materialInstance != null)
        {
            return materialInstance;
        }

        Material sourceMaterial = targetGraphic.material;
        if (sourceMaterial == null || !sourceMaterial.HasProperty(propertyName))
        {
            return null;
        }

        materialInstance = new Material(sourceMaterial)
        {
            name = sourceMaterial.name + " (Runtime)",
            hideFlags = HideFlags.DontSave
        };
        targetGraphic.material = materialInstance;
        return materialInstance;
    }

    private void DestroyMaterialInstance()
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
