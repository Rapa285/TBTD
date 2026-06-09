using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Offer-facing weapon evolution definition that resolves to one tower-facing upgrade leaf.
/// </summary>
[CreateAssetMenu(fileName = "Evolution", menuName = "TBTD/Evolution")]
public sealed class EvolutionSO : ScriptableObject
{
    /// <summary>
    /// One required multi-upgrade line level needed before this evolution can be offered.
    /// </summary>
    [Serializable]
    public struct Prerequisite
    {
        [SerializeField, Tooltip("Square-node multi-upgrade line required by this evolution.")]
        private MultiUpgradeSO multiUpgrade;

        [SerializeField, Min(1), Tooltip("Minimum selected level required in this line.")]
        private int minimumLevel;

        public MultiUpgradeSO MultiUpgrade => multiUpgrade;
        public int MinimumLevel => Mathf.Max(1, minimumLevel);
    }

    [SerializeField, Tooltip("Tower-facing upgrade leaf applied when this evolution is selected.")]
    private UpgradeSO resolvedUpgrade;

    [SerializeField, Tooltip("Required square-node upgrade levels before this evolution can be offered.")]
    private List<Prerequisite> prerequisites = new List<Prerequisite>();

    public UpgradeSO ResolvedUpgrade => resolvedUpgrade;
    public IReadOnlyList<Prerequisite> Prerequisites => prerequisites;
    public bool HasResolvedUpgrade => resolvedUpgrade != null;
}
