using System.Collections.Generic;
using UnityEngine;

public class StatusEffectManager : MonoBehaviour
{
    private List<StatusEffect> activeEffects = new List<StatusEffect>();
    public List<StatusEffect> ActiveEffects => activeEffects;

    // call by tower when applying effect to enemy
    public void AddEffect(StatusEffect newEffect)
    {
        if (newEffect == null) return;

        if (newEffect.IsUnique)
        {
            StatusEffect existing=null;
            for (int i = 0; i < activeEffects.Count; i++)
            {
                if (activeEffects[i] != null && activeEffects[i].EffectName == newEffect.EffectName)
                {
                    existing = activeEffects[i];
                    break;
                }
            }

            if (existing != null)
            {
                if (newEffect.EffectStrength > existing.EffectStrength)
                {
                    existing.OnRemove(gameObject);
                    activeEffects.Remove(existing);

                    newEffect.OnApply(gameObject);
                    activeEffects.Add(newEffect);
                    Debug.Log($"Reapplying effect --> {gameObject.name}");
                }
                else
                {
                    float remainingDuration=existing.Duration - existing.ElapsedTime;
                    existing.Duration=Mathf.Max(remainingDuration, newEffect.Duration);
                    existing.ElapsedTime=0f;
                    Debug.Log($"Refreshing effect --> {gameObject.name}");
                }
                return;
            }
        }

        newEffect.OnApply(gameObject);
        activeEffects.Add(newEffect);

        Debug.Log($"{gameObject.name} got {newEffect.EffectName}!");
    }

    // for callers that cannot reference StatusEffect types directly
    public void AddDot(float damagePerTick, int totalTicks, float tickInterval)
    {
        var effect = new DotEffect(damagePerTick, totalTicks, tickInterval);
        AddEffect(effect);
    }
    private void Update()
    {
        for (int i = activeEffects.Count - 1; i >= 0; i--)
        {
            StatusEffect effect = activeEffects[i];
            if (effect == null)
            {
                activeEffects.RemoveAt(i);
                continue;
            }

            effect.ElapsedTime += Time.deltaTime;
            effect.OnTick(gameObject, Time.deltaTime);

            if (effect.ElapsedTime >= effect.Duration)
            {
                effect.OnRemove(gameObject);
                activeEffects.RemoveAt(i);

                Debug.Log($"{gameObject.name} lost effect {effect.EffectName}.");
            }
        }
    }

    public void ClearAllEffects()
    {
        for (int i = activeEffects.Count - 1; i >= 0; i--)
        {
            var effect = activeEffects[i];
            if (effect != null)
            {
                effect.OnRemove(gameObject);
            }
        }

        activeEffects.Clear();
    }

    private void OnDisable()
    {
        ClearAllEffects();
    }

    private void OnDestroy()
    {
        ClearAllEffects();
    }
}
