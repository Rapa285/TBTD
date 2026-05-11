using System.Linq;
using UnityEngine;

public abstract class StatusEffect
{
    public string EffectName;
    public float Duration;
    public float ElapsedTime;
    public Sprite EffectIcon;
    public float EffectStrength;
    public bool IsUnique=true;

    public virtual void OnApply(GameObject target) { }

    public virtual void OnTick(GameObject target, float deltaTime) { }

    public virtual void OnRemove(GameObject target) { }

    protected void AutoLoadIcon()
    {
        Sprite[] allSprites=Resources.LoadAll<Sprite>("StatusIcons/StatusIcons");
        if (allSprites == null || allSprites.Length == 0)
        {
            Debug.LogWarning("No sprites found in Resources/StatusIcons/StatusIcons");
            return;
        }
        EffectIcon=allSprites.FirstOrDefault(s => s.name == this.EffectName);
        if (EffectIcon == null)
        {
            Debug.LogWarning("No icon found for effect: " + EffectName);
        }
    }
}
