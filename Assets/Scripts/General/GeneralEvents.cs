using UnityEngine;

public struct TowerSelectedEvent
{
    public TowerEntity towerEntity;
}

public struct BaseDamagedEvent
{
    public float DamageAmount;
}

// Contoh event tanpa data (hanya butuh trigger-nya saja)
public struct GamePausedEvent { }
public struct GameUnPausedEvent { }

public struct GameOverEvent { }
public struct SettingsEvent { }

public struct SlowDownGameEvent
{
    public float SlowDownScale;
}

public struct KeyboardInput
{
    public KeyCode inputKey;
}

public readonly struct SettingsChangedEvent
{
    public readonly ConfigData Config;

    public SettingsChangedEvent(ConfigData config)
    {
        Config = config;
    }
}

public readonly struct RetryGameEvent { }

public readonly struct ExitToMainMenuEvent { }