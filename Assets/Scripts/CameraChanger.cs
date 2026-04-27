using UnityEngine;
using UnityEngine.Video;
using UnityEngine.EventSystems;

public class CameraChanger : MonoBehaviour, IPointerClickHandler
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    public GameObject camera;
    private bool cameraActive = false;
    public KeyCode tombolPemicu = KeyCode.Escape;

    private void Awake()
    {
        if (camera == null)
        {

            Debug.Log("Tidak ada Camera dalam tower");
            
        }

    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            if (camera != null)
            {
                Debug.Log($"Objek {gameObject.name} diklik! Pindah ke {camera.name}");
                
                // Memanggil event bus yang sudah kita buat sebelumnya
                GameEvents.RequestCameraChange(camera);
            }
            else
            {
                Debug.LogWarning("Kamera tujuan belum diisi di Inspector!");
            }
        }

    }

    private void Update()
    {
        if (cameraActive != false){
            if (Input.GetKeyDown(tombolPemicu))
            {
                if (camera != null)
                {
                    Debug.Log($"Tombol {tombolPemicu} ditekan! Mengganti kamera...");
                    // Memanggil event bus
                    GameEvents.RequestCameraChange(camera);
                }
                else
                {
                    Debug.LogWarning("Kamera tujuan belum diisi di Inspector!");
                }
            }
        }
    }



}
