using UnityEngine;

public class StealthComponent : MonoBehaviour
{
    [Tooltip("Pilih Layer yang TIDAK dimasukkan ke targetLayers UnitVision menara biasa")]
    [SerializeField] private string stealthLayerName = "StealthEnemy";

    private void Start()
    {
        int layerIndex = LayerMask.NameToLayer(stealthLayerName);
        if (layerIndex != -1)
        {
            gameObject.layer = layerIndex;
            foreach (Transform child in transform)
            {
                child.gameObject.layer = layerIndex;
            }
        }
    }
}