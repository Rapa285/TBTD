using UnityEngine;

public class GameManager : MonoBehaviour
{
    [SerializeField]
    private float baseHealth = 100f;
    private GameObject SelectedTower;


    private void OnEnable()
    {
        // Mulai mendengarkan event BaseDamagedEvent
        GeneralEventBus<BaseDamagedEvent>.Subscribe(DamageBase);
    }

    private void OnDisable()
    {
        // Mulai mendengarkan event BaseDamagedEvent
        GeneralEventBus<BaseDamagedEvent>.Unsubscribe(DamageBase);

    }

    private void DamageBase(BaseDamagedEvent eventData)
    {
        baseHealth -= eventData.DamageAmount;
        if (baseHealth <= 0)
        {
            GameOver();
        }
    }

    // Menyiarkan Event Game Over
    private void GameOver()
    {
        GeneralEventBus<GameOverEvent>.Publish(new GameOverEvent{});
        GeneralEventBus<GamePausedEvent>.Publish(new GamePausedEvent{});
    }
}
