using System;
using System.Collections.Generic;
using UnityEngine;

public struct NewWaveEvent
{
    public bool isSpecialWave;
    public int waveNumber;
    public int enemyCount;
    public List<GameObject> enemyToSpawn;

    public NewWaveEvent(bool isSpecialWave, int waveNumber, int enemyCount, List<GameObject> enemyToSpawn)
    {
        this.isSpecialWave = isSpecialWave;
        this.waveNumber = waveNumber;
        this.enemyCount = enemyCount;
        this.enemyToSpawn = enemyToSpawn;
    }
}

public struct WaveTimerTickEvent
{
    public float timeRemaining;

    public WaveTimerTickEvent(float timeRemaining)
    {
        this.timeRemaining = timeRemaining;
    }
}

public struct GraceTimerTickEvent
{
    public float timeRemaining;

    public GraceTimerTickEvent(float timeRemaining)
    {
        this.timeRemaining = timeRemaining;
    }
}

[DefaultExecutionOrder(-1000)]
public class WaveEventBus : MonoBehaviour
{
  public event Action<NewWaveEvent> NewWave;
  public event Action<WaveTimerTickEvent> WaveTimerTick;
  public event Action<GraceTimerTickEvent> GraceTimerTick;
  public event Action GraceTimerEnded;
  public event Action SpecialWave;
  public event Action SpecialWaveEnded;
  public event Action InfiniteRoundTriggered;

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

    public void RaiseWaveTimerTick(WaveTimerTickEvent eventData)
    {
        WaveTimerTick?.Invoke(eventData);
    }

    public void RaiseGraceTimerTick(GraceTimerTickEvent eventData)
    {
        GraceTimerTick?.Invoke(eventData);
    }

    public void RaiseGraceTimerEnded()
    {
        GraceTimerEnded?.Invoke();
    }

    public void RaiseSpecialWave()
    {
        SpecialWave?.Invoke();
    }

    public void RaiseSpecialWaveEnded()
    {
        SpecialWaveEnded?.Invoke();
    }

    public void RaiseInfiniteRoundTriggered()
    {
        InfiniteRoundTriggered?.Invoke();
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
