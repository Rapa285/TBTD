using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Data bridge that identifies the collider used for player tower selection.
/// </summary>
public sealed class TowerSelectionTarget : MonoBehaviour
{
    [SerializeField, Tooltip("Tower selected when this click target is hit. Defaults to the parent tower.")]
    private TowerEntity tower;

    [SerializeField, Tooltip("Collider that receives selection pointer hits. Assign this directly in the Inspector.")]
    private Collider selectionCollider;

    [SerializeField, Tooltip("Invoked when the resolved tower becomes selected.")]
    private UnityEvent onSelected = new UnityEvent();

    [SerializeField, Tooltip("Invoked when the resolved tower becomes deselected.")]
    private UnityEvent onDeselected = new UnityEvent();

    private TowerEntity subscribedTower;

    public TowerEntity Tower => ResolveTower();
    public Collider SelectionCollider => selectionCollider;
    public UnityEvent OnSelected => onSelected;
    public UnityEvent OnDeselected => onDeselected;

    private void Awake()
    {
        ResolveTower();
        SubscribeToTowerSelectionEvents();
    }

    private void OnEnable()
    {
        SubscribeToTowerSelectionEvents();
    }

    private void OnValidate()
    {
        ResolveTower();
    }

    private void OnDisable()
    {
        UnsubscribeFromTowerSelectionEvents();
    }

    private void OnDestroy()
    {
        UnsubscribeFromTowerSelectionEvents();
    }

    public bool TryGetSelectableTower(out TowerEntity selectableTower)
    {
        selectableTower = ResolveTower();
        return selectableTower != null && selectableTower.Deployed;
    }

    public bool IsSelectionCollider(Collider hitCollider)
    {
        return selectionCollider != null
            && hitCollider != null
            && hitCollider == selectionCollider;
    }

    private TowerEntity ResolveTower()
    {
        if (tower == null)
        {
            tower = GetComponentInParent<TowerEntity>();
        }

        return tower;
    }

    private void SubscribeToTowerSelectionEvents()
    {
        TowerEntity resolvedTower = ResolveTower();
        if (subscribedTower == resolvedTower)
        {
            return;
        }

        UnsubscribeFromTowerSelectionEvents();
        subscribedTower = resolvedTower;
        if (subscribedTower == null)
        {
            return;
        }

        subscribedTower.Selected += HandleTowerSelected;
        subscribedTower.Deselected += HandleTowerDeselected;
    }

    private void UnsubscribeFromTowerSelectionEvents()
    {
        if (subscribedTower == null)
        {
            return;
        }

        subscribedTower.Selected -= HandleTowerSelected;
        subscribedTower.Deselected -= HandleTowerDeselected;
        subscribedTower = null;
    }

    private void HandleTowerSelected()
    {
        onSelected?.Invoke();
    }

    private void HandleTowerDeselected()
    {
        onDeselected?.Invoke();
    }
}
