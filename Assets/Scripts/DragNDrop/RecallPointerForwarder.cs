using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Receives forwarded recall pointer input from a child button object.
/// </summary>
public interface IRecallPointerReceiver
{
    void HandleRecallPointerDown(PointerEventData eventData);
    void HandleRecallPointerUp(PointerEventData eventData);
    void HandleRecallPointerExit(PointerEventData eventData);
}

/// <summary>
/// Forwards child recall button pointer input to the owning recall behavior.
/// </summary>
public sealed class RecallPointerForwarder : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    private IRecallPointerReceiver owner;

    public void Initialize(IRecallPointerReceiver recallOwner)
    {
        owner = recallOwner;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        owner?.HandleRecallPointerDown(eventData);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        owner?.HandleRecallPointerUp(eventData);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        owner?.HandleRecallPointerExit(eventData);
    }
}
