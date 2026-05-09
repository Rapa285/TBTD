using System;
using System.Collections.Generic;
using UnityEngine;

public struct NewWaveEvent
{
    public int waveNumber;
    public int enemyCount;
    public List<GameObject> enemyToSpawn;

    public NewWaveEvent(int waveNumber, int enemyCount, List<GameObject> enemyToSpawn)
    {
        this.waveNumber = waveNumber;
        this.enemyCount = enemyCount;
        this.enemyToSpawn = enemyToSpawn;
    }
}

[DefaultExecutionOrder(-1000)]
public class WaveEventBus : MonoBehaviour
{
  public event Action<NewWaveEvent> NewWave;

  private void Awake()
    {
        RegisterWithServiceLocator();
    }

    private void OnDestroy()
    {
        ServiceLocator.Unregister<WaveEventBus>(this);
    }

    public void RaiseNewWave(NewWaveEvent eventData)
    {
        NewWave?.Invoke(eventData);
    }

    private void RegisterWithServiceLocator()
    {
        if (ServiceLocator.TryResolve<WaveEventBus>(out WaveEventBus existingEventBus)
            && existingEventBus != null
            && existingEventBus != this)
        {
            Debug.LogWarning(
                $"{nameof(WaveEventBus)} on '{name}' replaced the previously registered {nameof(WaveEventBus)} on '{existingEventBus.name}'.",
                this);
        }

        ServiceLocator.Register<WaveEventBus>(this);
    }
}
