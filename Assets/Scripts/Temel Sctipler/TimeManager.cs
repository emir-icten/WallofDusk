using UnityEngine;
using System;

public class TimeManager : MonoBehaviour
{
    public static TimeManager Instance { get; private set; }

    [Header("Zaman Ayarları")]
    [Tooltip("Bir oyun günü kaç gerçek saniye sürsün?")]
    [Min(10f)]
    public float dayDurationInSeconds = 300f; // default 5 dakika

    [Tooltip("Başlangıç saati (0-24 arasında)")]
    [Range(0f, 24f)]
    public float currentTime = 6f; // default 06:00'da başlasın

    [Header("Gün Gece Saatleri")]
    [Tooltip("Gündüz başlangıç saati (ör. 6)")]
    public float dayStartHour = 6f;
    [Tooltip("Gündüz bitiş saati (ör. 18)")]
    public float dayEndHour = 18f;

    public bool IsDay { get; private set; }

    public event Action OnDayStart;
    public event Action OnNightStart;
    public event Action<float> OnTimeChanged; // float = currentTime (0-24)

    private float timeSpeed; // saat / saniye

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        Instance = this;
        // isteğe bağlı: sahneler arası koru
        // DontDestroyOnLoad(this.gameObject);
    }

    private void Start()
    {
        // saat dönüşümünü hesapla
        timeSpeed = 24f / Mathf.Max(1f, dayDurationInSeconds);
        IsDay = currentTime >= dayStartHour && currentTime < dayEndHour;

        // Başlangıç eventleri
        if (IsDay) OnDayStart?.Invoke(); else OnNightStart?.Invoke();
    }

    private void Update()
    {
        // zaman ilerlet
        currentTime += timeSpeed * Time.deltaTime;
        if (currentTime >= 24f) currentTime -= 24f;

        OnTimeChanged?.Invoke(currentTime);

        CheckTransitions();
    }

    private void CheckTransitions()
    {
        bool nowIsDay = currentTime >= dayStartHour && currentTime < dayEndHour;
        if (nowIsDay != IsDay)
        {
            IsDay = nowIsDay;
            if (IsDay) OnDayStart?.Invoke(); else OnNightStart?.Invoke();
        }
    }

    // Yardımcı: saat formatı döndür
    public string GetTimeString()
    {
        int hour = Mathf.FloorToInt(currentTime);
        int minute = Mathf.FloorToInt((currentTime - hour) * 60f);
        return $"{hour:00}:{minute:00}";
    }

    private void OnDestroy()
    {
        // temizle
        OnDayStart = null;
        OnNightStart = null;
        OnTimeChanged = null;
        if (Instance == this) Instance = null;
    }
}
