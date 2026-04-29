using System.Collections.Generic;
using UnityEngine;

public class StatusEffectManager : MonoBehaviour
{
    private List<StatusEffect> activeEffects = new List<StatusEffect>();

    // call by tower when applying effect to enemy
    public void AddEffect(StatusEffect newEffect)
    {
        if (newEffect == null) return;

        newEffect.OnApply(gameObject);
        activeEffects.Add(newEffect);

        Debug.Log($"{gameObject.name} terkena efek {newEffect.EffectName}!");
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

                Debug.Log($"{gameObject.name} kehilangan efek {effect.EffectName}.");
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
