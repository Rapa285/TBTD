using UnityEngine;

// Baris ini memastikan Unity akan otomatis meminta Collider jika belum ada
[RequireComponent(typeof(Collider))]
public class ClickToOutline : MonoBehaviour
{
    private Outline outlineComponent;
    private bool isSelected = false;

    void Start()
    {
        // Mencoba mencari komponen Outline di objek ini
        outlineComponent = GetComponent<Outline>();
        
        // Jika kamu belum memasang komponen Quick Outline secara manual, script ini akan menambahkannya otomatis
        if (outlineComponent == null)
        {
            outlineComponent = gameObject.AddComponent<Outline>();
        }

        // --- PENGATURAN OUTLINE ---
        outlineComponent.OutlineMode = Outline.Mode.OutlineAll; // Mode standar garis tepi
        outlineComponent.OutlineColor = Color.yellow;           // Warna outline (bisa diganti)
        outlineComponent.OutlineWidth = 5f;                     // Ketebalan outline

        // Matikan outline di awal permainan agar tidak langsung menyala
        outlineComponent.enabled = false;
    }

    // Fungsi bawaan Unity: Akan terpanggil otomatis SAAT kursor mengklik area Collider objek ini
    void OnMouseDown() 
    {
        // Balikkan status (Jika false jadi true, jika true jadi false)
        isSelected = !isSelected; 

        // Nyalakan atau matikan komponen outline berdasarkan status isSelected
        outlineComponent.enabled = isSelected; 
    }
}