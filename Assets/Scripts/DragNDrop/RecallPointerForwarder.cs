using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Forwards child recall button pointer input to the owning UnitUIRecall.
/// </summary>
public sealed class RecallPointerForwarder : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    private UnitUIRecall owner;

    public void Initialize(UnitUIRecall recallOwner)
    {
        owner = recallOwner;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        owner?.OnPointerDown(eventData);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        owner?.OnPointerUp(eventData);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        owner?.OnPointerExit(eventData);
    }
}
