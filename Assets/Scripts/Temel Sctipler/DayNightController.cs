using UnityEngine;

public class DayNightController : MonoBehaviour
{
    [Header("References")]
    public Transform sun;          // Güneş objesi
    public Light sunLight;         // Güneş ışığı component
    public Transform moon;         // Ay objesi (opsiyonel)
    public Light moonLight;        // Ay ışığı (opsiyonel)

    [Header("Lighting Settings")]
    public float sunIntensityDay = 2f;   // Gündüz ışık yoğunluğunu artırdık
    public float sunIntensityNight = 0f;
    public float moonIntensityDay = 0f;  // gündüz ay ışığı kapalı
    public float moonIntensityNight = 0.25f;

    [Header("Skybox & Ambient")]
    public Gradient skyColor;      // Skybox renkleri
    public Gradient ambientColor;  // Ambient ışık renkleri
    public Gradient fogColor;      // Fog renkleri

    void Update()
    {
        if (TimeManager.Instance == null) return;

        // 0–1 arası normalize edilmiş zaman
        float t = TimeManager.Instance.currentTime / 24f;

        // -------------------
        // 1) Güneş ve Ay dönüşü
        // -------------------
        if (sun != null)
            sun.localRotation = Quaternion.Euler((t * 360f) - 90f, 0f, 0f);  // Y rotasyonu 0 yapıldı, ışık sahneyi direkt aydınlatsın
        if (moon != null)
            moon.localRotation = Quaternion.Euler((t * 360f) + 90f, 0f, 0f);

        // -------------------
        // 2) Işık yoğunluğu blend (doğal gün ışığı)
        // -------------------
        float daylight = Mathf.Clamp01(Mathf.Sin(t * Mathf.PI * 2f - Mathf.PI / 2f) * 0.5f + 0.5f);
        float nightlight = 1f - daylight;

        if (sunLight != null)
            sunLight.intensity = Mathf.Lerp(sunIntensityNight, sunIntensityDay, daylight);
        if (moonLight != null)
            moonLight.intensity = Mathf.Lerp(moonIntensityDay, moonIntensityNight, nightlight);

        // -------------------
        // 3) Skybox, ambient ve fog renkleri
        // -------------------
        if (skyColor != null && RenderSettings.skybox != null)
        {
            if (RenderSettings.skybox.HasProperty("_Tint"))
                RenderSettings.skybox.SetColor("_Tint", skyColor.Evaluate(t));
        }

        if (ambientColor != null)
            RenderSettings.ambientLight = ambientColor.Evaluate(t);

        if (fogColor != null)
            RenderSettings.fogColor = fogColor.Evaluate(t);
    }
}
