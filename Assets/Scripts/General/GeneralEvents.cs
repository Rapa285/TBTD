using UnityEngine;

// Event untuk mengganti kamera (seperti yang kita bahas sebelumnya)
public struct TowerSelectedEvent
{
    public GameObject TargetCamera;
}

// Contoh event lain: saat player kena damage
public struct BaseDamagedEvent
{
    public float DamageAmount;
}

// Contoh event tanpa data (hanya butuh trigger-nya saja)
public struct GamePausedEvent { }

public struct GameOverEvent { }