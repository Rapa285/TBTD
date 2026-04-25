using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Runtime weapon, augment, and projectile-modifier instance composition for <see cref="TowerEntity"/>.
/// </summary>
public partial class TowerEntity
{
    private void ApplyRuntimeComposition(
        AttackBehaviour replacementPrefab,
        IReadOnlyList<AttackBehaviour> augmentWeaponPrefabs,
        IReadOnlyList<ProjectileModifierBehaviour> projectileModifierPrefabs)
    {
        if (!Application.isPlaying)
        {
            return;
        }

        AttackBehaviour previousPrimaryAttackBehaviour = GetActiveAttackBehaviour();

        ResolveAttackBehaviourReferences();
        UpdateRuntimeReplacement(replacementPrefab);
        UpdateRuntimeAugments(augmentWeaponPrefabs);
        UpdateRuntimeProjectileModifiers(projectileModifierPrefabs);

        // Preserve the serialized default weapon unless an applied upgrade provides an override.
        activeAttackBehaviour = runtimeReplacementAttackBehaviour != null
            ? runtimeReplacementAttackBehaviour
            : defaultAttackBehaviour;

        if (previousPrimaryAttackBehaviour != null && previousPrimaryAttackBehaviour != activeAttackBehaviour)
        {
            previousPrimaryAttackBehaviour.ResetAmmoConsumptionState();
        }

        ConfigureAttackBehaviour(activeAttackBehaviour, projectileModifierPrefabs, true);
        for (int i = 0; i < runtimeAugmentAttackBehaviours.Count; i++)
        {
            ConfigureAttackBehaviour(runtimeAugmentAttackBehaviours[i], projectileModifierPrefabs, false);
        }

        RebuildActiveAttackBehaviourList();
    }

    private void ConfigureAttackBehaviour(
        AttackBehaviour behaviour,
        IReadOnlyList<ProjectileModifierBehaviour> projectileModifierPrefabs,
        bool usesTowerAmmo)
    {
        if (behaviour != null)
        {
            behaviour.ConfigureRuntime(this, transform, activeProjectileModifiers, projectileModifierPrefabs, usesTowerAmmo);
        }
    }

    private void ResolveAttackBehaviourReferences()
    {
        if (attackBehaviour == null)
        {
            attackBehaviour = GetComponent<AttackBehaviour>();
        }

        if (defaultAttackBehaviour == null
            || defaultAttackBehaviour == runtimeReplacementAttackBehaviour
            || runtimeAugmentAttackBehaviours.Contains(defaultAttackBehaviour))
        {
            defaultAttackBehaviour = attackBehaviour;
        }

        if (activeAttackBehaviour == null)
        {
            activeAttackBehaviour = defaultAttackBehaviour;
        }
    }

    private void UpdateRuntimeReplacement(AttackBehaviour replacementPrefab)
    {
        if (currentReplacementPrefab == replacementPrefab
            && (replacementPrefab == null || runtimeReplacementAttackBehaviour != null))
        {
            return;
        }

        DestroyRuntimeReplacement();
        currentReplacementPrefab = replacementPrefab;

        if (replacementPrefab == null)
        {
            return;
        }

        // Replacement weapons are instantiated at runtime so upgrade changes never modify prefab-authored defaults.
        runtimeReplacementAttackBehaviour = Instantiate(replacementPrefab, transform);
        runtimeReplacementAttackBehaviour.name = $"{replacementPrefab.name} (Upgrade Runtime)";
    }

    private void DestroyRuntimeReplacement()
    {
        if (runtimeReplacementAttackBehaviour != null)
        {
            Destroy(runtimeReplacementAttackBehaviour.gameObject);
            runtimeReplacementAttackBehaviour = null;
        }
    }

    private void UpdateRuntimeAugments(IReadOnlyList<AttackBehaviour> augmentWeaponPrefabs)
    {
        if (HasSameAugmentWeaponPrefabs(augmentWeaponPrefabs) && HasLiveRuntimeAugments())
        {
            return;
        }

        DestroyRuntimeAugments();
        currentAugmentWeaponPrefabs.Clear();

        for (int i = 0; i < augmentWeaponPrefabs.Count; i++)
        {
            AddRuntimeAugment(augmentWeaponPrefabs[i]);
        }
    }

    private void AddRuntimeAugment(AttackBehaviour augmentPrefab)
    {
        if (augmentPrefab == null)
        {
            return;
        }

        currentAugmentWeaponPrefabs.Add(augmentPrefab);
        AttackBehaviour augmentInstance = Instantiate(augmentPrefab, transform);
        augmentInstance.name = $"{augmentPrefab.name} (Augment Runtime)";
        runtimeAugmentAttackBehaviours.Add(augmentInstance);
    }

    private void DestroyRuntimeAugments()
    {
        for (int i = 0; i < runtimeAugmentAttackBehaviours.Count; i++)
        {
            AttackBehaviour augment = runtimeAugmentAttackBehaviours[i];
            if (augment != null)
            {
                Destroy(augment.gameObject);
            }
        }

        runtimeAugmentAttackBehaviours.Clear();
    }

    private void UpdateRuntimeProjectileModifiers(IReadOnlyList<ProjectileModifierBehaviour> projectileModifierPrefabs)
    {
        if (HasSameProjectileModifierPrefabs(projectileModifierPrefabs) && HasLiveRuntimeProjectileModifiers())
        {
            return;
        }

        if (IsCurrentProjectileModifierPrefix(projectileModifierPrefabs))
        {
            // Append-only upgrade flow can add new modifier instances without rebuilding existing state.
            for (int i = currentProjectileModifierPrefabs.Count; i < projectileModifierPrefabs.Count; i++)
            {
                AddRuntimeProjectileModifier(projectileModifierPrefabs[i]);
            }

            return;
        }

        DestroyRuntimeProjectileModifiers();
        currentProjectileModifierPrefabs.Clear();

        for (int i = 0; i < projectileModifierPrefabs.Count; i++)
        {
            AddRuntimeProjectileModifier(projectileModifierPrefabs[i]);
        }
    }

    private void AddRuntimeProjectileModifier(ProjectileModifierBehaviour modifierPrefab)
    {
        if (modifierPrefab == null)
        {
            return;
        }

        currentProjectileModifierPrefabs.Add(modifierPrefab);
        ProjectileModifierBehaviour modifierInstance = Instantiate(modifierPrefab, transform);
        modifierInstance.name = $"{modifierPrefab.name} (Tower Modifier Runtime)";
        activeProjectileModifiers.Add(modifierInstance);
    }

    private void DestroyRuntimeProjectileModifiers()
    {
        for (int i = 0; i < activeProjectileModifiers.Count; i++)
        {
            ProjectileModifierBehaviour modifier = activeProjectileModifiers[i];
            if (modifier != null)
            {
                Destroy(modifier.gameObject);
            }
        }

        activeProjectileModifiers.Clear();
    }

    private bool HasSameProjectileModifierPrefabs(IReadOnlyList<ProjectileModifierBehaviour> projectileModifierPrefabs)
    {
        return HasSamePrefabList(currentProjectileModifierPrefabs, projectileModifierPrefabs);
    }

    private bool IsCurrentProjectileModifierPrefix(IReadOnlyList<ProjectileModifierBehaviour> projectileModifierPrefabs)
    {
        return IsPrefabListPrefix(currentProjectileModifierPrefabs, projectileModifierPrefabs)
            && HasLiveRuntimeProjectileModifiers();
    }

    private bool HasLiveRuntimeProjectileModifiers()
    {
        return HasLiveRuntimeList(activeProjectileModifiers, currentProjectileModifierPrefabs.Count);
    }

    private bool HasSameAugmentWeaponPrefabs(IReadOnlyList<AttackBehaviour> augmentWeaponPrefabs)
    {
        return HasSamePrefabList(currentAugmentWeaponPrefabs, augmentWeaponPrefabs);
    }

    private bool HasLiveRuntimeAugments()
    {
        return HasLiveRuntimeList(runtimeAugmentAttackBehaviours, currentAugmentWeaponPrefabs.Count);
    }

    private static bool HasSamePrefabList<T>(IReadOnlyList<T> currentPrefabs, IReadOnlyList<T> targetPrefabs)
        where T : UnityEngine.Object
    {
        if (currentPrefabs.Count != targetPrefabs.Count)
        {
            return false;
        }

        for (int i = 0; i < targetPrefabs.Count; i++)
        {
            if (currentPrefabs[i] != targetPrefabs[i])
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsPrefabListPrefix<T>(IReadOnlyList<T> currentPrefabs, IReadOnlyList<T> targetPrefabs)
        where T : UnityEngine.Object
    {
        if (currentPrefabs.Count > targetPrefabs.Count)
        {
            return false;
        }

        for (int i = 0; i < currentPrefabs.Count; i++)
        {
            if (currentPrefabs[i] != targetPrefabs[i])
            {
                return false;
            }
        }

        return true;
    }

    private static bool HasLiveRuntimeList<T>(IReadOnlyList<T> runtimeInstances, int expectedCount)
        where T : UnityEngine.Object
    {
        if (runtimeInstances.Count != expectedCount)
        {
            return false;
        }

        for (int i = 0; i < runtimeInstances.Count; i++)
        {
            if (runtimeInstances[i] == null)
            {
                return false;
            }
        }

        return true;
    }

    private void RebuildActiveAttackBehaviourList()
    {
        activeAttackBehaviours.Clear();

        AttackBehaviour primaryAttackBehaviour = GetActiveAttackBehaviour();
        if (primaryAttackBehaviour != null)
        {
            activeAttackBehaviours.Add(primaryAttackBehaviour);
        }

        for (int i = 0; i < runtimeAugmentAttackBehaviours.Count; i++)
        {
            AttackBehaviour augmentAttackBehaviour = runtimeAugmentAttackBehaviours[i];
            if (augmentAttackBehaviour != null)
            {
                activeAttackBehaviours.Add(augmentAttackBehaviour);
            }
        }
    }

    private AttackBehaviour GetActiveAttackBehaviour()
    {
        if (activeAttackBehaviour != null)
        {
            return activeAttackBehaviour;
        }

        if (runtimeReplacementAttackBehaviour != null)
        {
            return runtimeReplacementAttackBehaviour;
        }

        return attackBehaviour;
    }
}
