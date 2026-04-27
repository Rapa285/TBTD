using System;
using UnityEngine;

public static class GameEvents
{
    // Ini adalah "saluran" event-nya. 
    // Menerima parameter GameObject (kamera baru).
    public static event Action<GameObject> OnCameraChangeRequest;

    // Fungsi untuk memicu event
    public static void RequestCameraChange(GameObject nextCamera)
    {
        // Tanda ? mengecek apakah ada yang 'mendengarkan' event ini
        // Jika ada, panggil (Invoke) event-nya.
        OnCameraChangeRequest?.Invoke(nextCamera);
    }
}