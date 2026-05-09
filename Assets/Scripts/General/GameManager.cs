using UnityEditor;
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
        GeneralEventBus<GamePausedEvent>.Subscribe(PauseGame);
        GeneralEventBus<GameUnPausedEvent>.Subscribe(UnPauseGame);
    }

    private void OnDisable()
    {
        // Mulai mendengarkan event BaseDamagedEvent
        GeneralEventBus<BaseDamagedEvent>.Unsubscribe(DamageBase);
        GeneralEventBus<GamePausedEvent>.Unsubscribe(PauseGame);
        GeneralEventBus<GameUnPausedEvent>.Unsubscribe(UnPauseGame);

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

    private void PauseGame(GamePausedEvent eventData)
    {
        Time.timeScale = 0f;
    }

    private void UnPauseGame(GameUnPausedEvent eventData)
    {
        Time.timeScale = 1f;
    }


}
