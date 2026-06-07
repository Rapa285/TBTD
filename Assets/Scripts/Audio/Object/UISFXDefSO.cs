using UnityEngine;

[CreateAssetMenu(fileName = "UISFXDef", menuName = "Audio/UI SFX", order = 1)]
public sealed class UISFXDefSO : AudioClipData
{
    [SerializeField] private UISFXID sfxId;

    public UISFXID SfxId => sfxId;
}
