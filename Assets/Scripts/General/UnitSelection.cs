using UnityEngine;

public class UnitSelection : MonoBehaviour
{
    void OnMouseDown()
    {
        Debug.Log("Objek " + gameObject.name + " berhasil diklik!");
        // Masukkan kode logikamu di sini (misal: hancurkan objek, kurangi HP, dll)
    }
}