using UnityEngine;

// Event untuk mengganti kamera (seperti yang kita bahas sebelumnya)
public struct TowerSelectedEvent
{
    public TowerEntity towerEntity;
}

// Contoh event lain: saat player kena damage
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

