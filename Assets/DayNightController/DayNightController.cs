using UnityEngine;

public class DayNightController : MonoBehaviour
{
    public float cycleLength = 20f; // toplam süre (saniye)
    public float dayRatio = 0.6f;   // %60 gündüz, %40 gece
    private float timer = 0f;
    private bool isDay = true;

    public Light directionalLight; // sahnedeki Directional Light
    public Color dayColor = Color.white;
    public Color nightColor = Color.blue;

    void Update()
    {
        timer += Time.deltaTime;

        if (isDay && timer > cycleLength * dayRatio)
        {
            SwitchToNight();
        }
        else if (!isDay && timer > cycleLength)
        {
            SwitchToDay();
        }
    }

    void SwitchToDay()
    {
        isDay = true;
        timer = 0f;
        if (directionalLight != null)
            directionalLight.color = dayColor;
        Debug.Log("Gündüz başladı!");
    }

    void SwitchToNight()
    {
        isDay = false;
        // timer devam eder, cycleLength dolunca tekrar gündüz olacak
        if (directionalLight != null)
            directionalLight.color = nightColor;
        Debug.Log("Gece başladı!");
    }
}
