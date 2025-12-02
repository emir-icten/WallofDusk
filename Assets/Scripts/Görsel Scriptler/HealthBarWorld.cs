using UnityEngine;
using UnityEngine.UI;

public class HealthBarWorld : MonoBehaviour
{
    [Header("Referanslar")]
    public Health targetHealth;   // Canını takip edeceğimiz obje
    public Image fillImage;       // UI Image (fillAmount kullanan)

    [Header("Pozisyon Ayarları")]
    public Vector3 worldOffset = new Vector3(0f, 2f, 0f); // Kafasının üstü gibi

    private Camera mainCam;

    private void Start()
    {
        mainCam = Camera.main;

        if (targetHealth == null)
        {
            Debug.LogWarning("HealthBarWorld: targetHealth atanmadı.", this);
        }
    }

    private void Update()
    {
        if (targetHealth == null || fillImage == null) return;

        // Fill oranı
        float t = (float)targetHealth.currentHealth / targetHealth.maxHealth;
        fillImage.fillAmount = t;

        // Hedefin üstünde konumlandır
        Vector3 worldPos = targetHealth.transform.position + worldOffset;
        Vector3 screenPos = mainCam.WorldToScreenPoint(worldPos);

        transform.position = screenPos;
    }
}
