using TMPro;
using UnityEngine;

public class NightsSurvivedText : MonoBehaviour
{
    [Header("Referanslar")]
    public EnemySpawner spawner;
    public TextMeshProUGUI text;

    [Header("Metin Ayarları")]
    public string prefix = "Toplam ";
    public string suffix = " gece hayatta kaldın.";
    public string zeroNightMessage = "Hiç gece hayatta kalamadın...";

    private void OnEnable()
    {
        if (text == null || spawner == null)
            return;

        int night = spawner.CurrentNight;

        if (night <= 0)
        {
            text.text = zeroNightMessage;
        }
        else
        {
            text.text = $"{prefix}{night}{suffix}";
        }
    }
}
