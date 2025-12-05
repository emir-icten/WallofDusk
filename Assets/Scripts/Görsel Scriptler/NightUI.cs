using TMPro;
using UnityEngine;

public class NightUI : MonoBehaviour   // İsmi kalsın, içerik artık "Day" sayacı
{
    [Header("Referanslar")]
    public TextMeshProUGUI dayText;

    [Header("Metin Ayarları")]
    public string prefix = "Day ";

    private int currentDay = 0;
    private bool subscribed = false;

    private void OnEnable()
    {
        SubscribeToTimeManager();
        InitializeDayIfNeeded();
        RefreshText();
    }

    private void OnDisable()
    {
        UnsubscribeFromTimeManager();
    }

    private void SubscribeToTimeManager()
    {
        if (subscribed) return;

        if (TimeManager.Instance != null)
        {
            TimeManager.Instance.OnDayStart += HandleDayStart;
            subscribed = true;
        }
    }

    private void UnsubscribeFromTimeManager()
    {
        if (!subscribed) return;

        if (TimeManager.Instance != null)
        {
            TimeManager.Instance.OnDayStart -= HandleDayStart;
        }

        subscribed = false;
    }

    /// <summary>
    /// Oyun başında zaten gündüzse, bunu Day 1 kabul et.
    /// (TimeManager genelde sabah saatte başlıyordu.)
    /// </summary>
    private void InitializeDayIfNeeded()
    {
        if (TimeManager.Instance == null || dayText == null)
            return;

        // Daha hiç gün saymadıysak ve şu an gündüzse → Day 1
        if (currentDay == 0 && TimeManager.Instance.IsDay)
        {
            currentDay = 1;
        }
    }

    private void HandleDayStart()
    {
        currentDay++;
        RefreshText();
    }

    private void RefreshText()
    {
        if (dayText == null) return;

        if (currentDay <= 0)
        {
            dayText.text = "";
        }
        else
        {
            dayText.text = prefix + currentDay;
        }
    }
}
