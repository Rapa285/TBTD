using System;

public class GeneralEventBus<T> where T : struct
{
    // Saluran event untuk tipe data T
    private static event Action<T> OnEvent;

    // Fungsi untuk berlangganan (mendengarkan) event
    public static void Subscribe(Action<T> handler)
    {
        OnEvent += handler;
    }

    // Fungsi untuk berhenti berlangganan
    public static void Unsubscribe(Action<T> handler)
    {
        OnEvent -= handler;
    }

    // Fungsi untuk menyiarkan event ke semua pendengar
    public static void Publish(T eventData)
    {
        OnEvent?.Invoke(eventData);
    }
}
