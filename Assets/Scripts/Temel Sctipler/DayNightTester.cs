using UnityEngine;

public class DayNightTester : MonoBehaviour
{
    void Start()
    {
        TimeManager.Instance.OnDayStart += () => Debug.Log("GÜN BAŞLADI");
        TimeManager.Instance.OnNightStart += () => Debug.Log("GECE BAŞLADI");
    }
}
