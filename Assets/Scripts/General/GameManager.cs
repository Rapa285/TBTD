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
        GeneralEventBus<PauseState>.Subscribe(PauseGame);
    }

    private void OnDisable()
    {
        // Mulai mendengarkan event BaseDamagedEvent
        GeneralEventBus<BaseDamagedEvent>.Unsubscribe(DamageBase);
        GeneralEventBus<PauseState>.Unsubscribe(PauseGame);

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
        GeneralEventBus<PauseState>.Publish(new PauseState{});
    }

    private void PauseGame(PauseState eventData)
    {
        Time.timeScale = 0f;
    }


}
