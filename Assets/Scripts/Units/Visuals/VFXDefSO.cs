using UnityEngine;

public enum VFXType
{
    None,
    BulletHit,
    GrenadeHit,
    LaserHit
}

/// <summary>
/// Authored VFX definition used by scene-level pooled VFX playback.
/// </summary>
[CreateAssetMenu(fileName = "VFXDef", menuName = "TBTD/VFX Definition")]
public sealed class VFXDefSO : ScriptableObject
{
    [SerializeField, Tooltip("Gameplay category for this VFX definition.")]
    private VFXType vfxType = VFXType.None;

    [SerializeField, Tooltip("Prefab spawned by VFXService when this effect is requested.")]
    private GameObject vfxPrefab;

    public VFXType VFXType => vfxType;
    public GameObject VFXPrefab => vfxPrefab;
}
