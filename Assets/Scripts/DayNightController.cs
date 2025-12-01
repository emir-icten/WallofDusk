using UnityEngine;

public class DayNightController : MonoBehaviour
{
    [SerializeField] private float cycleLength = 240f; // Tam gün süresi (saniye)
    [SerializeField, Range(0f,1f)] private float startTime = 0.25f; // 0=gece, 0.25=sabah, 0.5=öğlen, 0.75=akşam
    [SerializeField] private Gradient sunColor;         // Işık rengi
    [SerializeField] private AnimationCurve sunIntensity; // Işık şiddeti

    private Light sunLight;
    private float time;

    private void Start()
    {
        // Bu script SunLight objesinde olmalı
        sunLight = GetComponent<Light>();
        time = startTime;
    }

    private void Update()
    {
        time += Time.deltaTime / cycleLength;
        if (time > 1f) time = 0f;

        // Güneşin sahnede doğup batması (rotation)
        float sunAngle = time * 360f;
        sunLight.transform.rotation = Quaternion.Euler(sunAngle - 90f, 170f, 0);

        // Renk ve yoğunluk
        sunLight.color = sunColor.Evaluate(time);
        sunLight.intensity = sunIntensity.Evaluate(time);

        // Ambient (gece tamamen siyah olmaması için)
        RenderSettings.ambientLight = Color.Lerp(
            Color.black,
            sunLight.color,
            sunLight.intensity * 0.5f
        );
    }

    public float GetTime()
    {
        return time; // 0–1 arası normalize zaman
    }
}
