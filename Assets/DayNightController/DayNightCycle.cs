using UnityEngine;

public class DayNightController : MonoBehaviour
{
    [SerializeField] private float cycleLength = 240f; // toplam gün süresi (saniye)
    [SerializeField, Range(0f,1f)] private float startTime = 0.25f; // başlangıç zamanı (0=siyah gece, 0.25=sabah, 0.5=öğlen, 0.75=akşam)
    [SerializeField] private Gradient sunColor;        // ışık rengi için gradient
    [SerializeField] private AnimationCurve sunIntensity; // ışık şiddeti için eğri

    private Light sunLight;
    private float time;

    private void Start()
    {
        // Bu scripti SunLight (Directional Light) objesine ekle
        sunLight = GetComponent<Light>();
        time = startTime;
    }

    private void Update()
    {
        // Zamanı ilerlet
        time += Time.deltaTime / cycleLength;
        if (time > 1f) time = 0f;

        // Güneşin hareketi (rotation)
        float sunAngle = time * 360f; // tam tur
        sunLight.transform.rotation = Quaternion.Euler(sunAngle - 90f, 170f, 0);

        // Renk ve yoğunluk
        sunLight.color = sunColor.Evaluate(time);
        sunLight.intensity = sunIntensity.Evaluate(time);

        // Ortam ışığı (gece tamamen siyah olmasın)
        RenderSettings.ambientLight = Color.Lerp(Color.black, sunLight.color, sunLight.intensity * 0.5f);
    }
}
