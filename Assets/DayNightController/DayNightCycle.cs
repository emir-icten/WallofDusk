using UnityEngine;

public class DayNightCycle : MonoBehaviour
{
    public Light sunLight;
    public float dayLength = 240f;   // 4 dakika = 240 saniye
    public float dayRatio = 0.5f;    // %50 gündüz, %50 gece
    public Gradient sunColor;
    public AnimationCurve sunIntensity;

    private float time;

    void Update()
    {
        time += Time.deltaTime / dayLength;
        if (time > 1f) time = 0f;

        // Güneşin yay çizerek hareketi
        float sunHeight = Mathf.Sin(time * Mathf.PI);
        float sunRotationY = Mathf.Lerp(90f, -90f, time);

        sunLight.transform.rotation = Quaternion.Euler(sunHeight * 90f - 10f, sunRotationY, 0);

        sunLight.color = sunColor.Evaluate(time);
        sunLight.intensity = sunIntensity.Evaluate(time);

        RenderSettings.ambientLight = sunLight.color * 0.5f;
    }
}
