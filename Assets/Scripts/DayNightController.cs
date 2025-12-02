using UnityEngine;

public class DayNightController : MonoBehaviour
{
    [Header("References")]
    public Transform sun;          // Güneş objesi
    public Light sunLight;         // Güneş ışığı component
    public Transform moon;         // Ay objesi (opsiyonel)
    public Light moonLight;        // Ay ışığı (opsiyonel)

    [Header("Lighting Settings")]
    public float sunIntensityDay = 2f;   // Gündüz ışık yoğunluğu
    public float sunIntensityNight = 0f;
    public float moonIntensityDay = 0f;  // Gündüz ay ışığı kapalı
    public float moonIntensityNight = 0.05f; // Gece ay ışığı (kısık)

    [Header("Skybox & Ambient")]
    public Gradient skyColor;      // Skybox renkleri
    public Gradient ambientColor;  // Ambient ışık renkleri
    public Gradient fogColor;      // Fog renkleri

    void Start()
    {
        // Gradientleri koddan oluşturuyoruz
        skyColor = BuildSkyGradient();
        ambientColor = BuildAmbientGradient();
        fogColor = BuildFogGradient();
    }

    private Gradient BuildSkyGradient()
    {
        var g = new Gradient();

        GradientColorKey[] colors = new GradientColorKey[5];
        GradientAlphaKey[] alphas = new GradientAlphaKey[2];

        // 00:00 - gece (koyu lacivert)
        colors[0].color = new Color(0.05f, 0.05f, 0.20f);
        colors[0].time  = 0.0f;

        // 06:00 - şafak
        colors[1].color = new Color(1.00f, 0.50f, 0.40f);
        colors[1].time  = 0.25f;

        // 12:00 - öğlen (açık mavi gökyüzü)
        colors[2].color = new Color(0.53f, 0.81f, 0.98f);
        colors[2].time  = 0.50f;

        // 18:00 - gün batımı
        colors[3].color = new Color(1.00f, 0.40f, 0.30f);
        colors[3].time  = 0.75f;

        // 24:00 - tekrar gece
        colors[4].color = new Color(0.05f, 0.05f, 0.20f);
        colors[4].time  = 1.0f;

        alphas[0].alpha = 1f; alphas[0].time = 0f;
        alphas[1].alpha = 1f; alphas[1].time = 1f;

        g.SetKeys(colors, alphas);
        return g;
    }

    private Gradient BuildAmbientGradient()
    {
        var g = new Gradient();

        GradientColorKey[] colors = new GradientColorKey[5];
        GradientAlphaKey[] alphas = new GradientAlphaKey[2];

        // 00:00 - neredeyse siyah
        colors[0].color = new Color(0.01f, 0.01f, 0.01f);
        colors[0].time  = 0.0f;

        // 06:00 - sabah
        colors[1].color = new Color(0.70f, 0.60f, 0.50f);
        colors[1].time  = 0.25f;

        // 12:00 - parlak öğlen
        colors[2].color = new Color(1.00f, 1.00f, 1.00f);
        colors[2].time  = 0.50f;

        // 18:00 - sıcak turuncu ton
        colors[3].color = new Color(0.80f, 0.50f, 0.40f);
        colors[3].time  = 0.75f;

        // 24:00 - tekrar çok koyu
        colors[4].color = new Color(0.01f, 0.01f, 0.01f);
        colors[4].time  = 1.0f;

        alphas[0].alpha = 1f; alphas[0].time = 0f;
        alphas[1].alpha = 1f; alphas[1].time = 1f;

        g.SetKeys(colors, alphas);
        return g;
    }

    private Gradient BuildFogGradient()
    {
        var g = new Gradient();

        GradientColorKey[] colors = new GradientColorKey[5];
        GradientAlphaKey[] alphas = new GradientAlphaKey[2];

        // 00:00 - koyu gece sisi
        colors[0].color = new Color(0.04f, 0.04f, 0.09f);
        colors[0].time  = 0.0f;

        // 06:00 - sabah sisi
        colors[1].color = new Color(1.00f, 0.70f, 0.50f);
        colors[1].time  = 0.25f;

        // 12:00 - öğlen sisi (açık mavi)
        colors[2].color = new Color(0.60f, 0.80f, 1.00f);
        colors[2].time  = 0.50f;

        // 18:00 - gün batımı sisi
        colors[3].color = new Color(0.80f, 0.30f, 0.20f);
        colors[3].time  = 0.75f;

        // 24:00 - tekrar koyu gece sisi
        colors[4].color = new Color(0.04f, 0.04f, 0.09f);
        colors[4].time  = 1.0f;

        alphas[0].alpha = 1f; alphas[0].time = 0f;
        alphas[1].alpha = 1f; alphas[1].time = 1f;

        g.SetKeys(colors, alphas);
        return g;
    }

    void Update()
    {
        if (TimeManager.Instance == null) return;

        // 0–1 arası normalize zaman
        float t = TimeManager.Instance.currentTime / 24f;

        // 1) Güneş ve Ay dönüşü
        if (sun != null)
            sun.localRotation = Quaternion.Euler((t * 360f) - 90f, 0f, 0f);
        if (moon != null)
            moon.localRotation = Quaternion.Euler((t * 360f) + 90f, 0f, 0f);

        // 2) Işık yoğunluğu (gündüz/ gece karışımı)
        float daylight = Mathf.Clamp01(Mathf.Sin(t * Mathf.PI * 2f - Mathf.PI / 2f) * 0.5f + 0.5f);
        float nightlight = Mathf.Pow(1f - daylight, 1.5f);  // gece etkisini yumuşattık

        if (sunLight != null)
            sunLight.intensity = Mathf.Lerp(sunIntensityNight, sunIntensityDay, daylight);

        if (moonLight != null)
            moonLight.intensity = Mathf.Lerp(moonIntensityDay, moonIntensityNight, nightlight);

        // 3) Skybox, ambient ve fog renkleri
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
