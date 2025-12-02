using UnityEngine;

// Unity'de sað týklayýp bu dosyayý oluþturabileceksiniz
[CreateAssetMenu(fileName = "GlobalPlayerStats", menuName = "GameData/PlayerStats")]
public class PlayerStatsSO : ScriptableObject
{
    [Header("Kalýcý Kristal Geliþtirmeleri")]
    [Tooltip("Tüm ok/silah hasarlarýna eklenecek YÜZDE (%) bonus")]
    [Range(0f, 1f)] // %0 ile %100 arasý
    public float globalDamageMultiplier = 0f;

    [Tooltip("Karakter hareket hýzýna eklenecek YÜZDE (%) bonus")]
    [Range(0f, 0.5f)] // %0 ile %50 arasý
    public float globalSpeedMultiplier = 0f;

    [Tooltip("Kule hasarlarýna eklenecek YÜZDE (%) bonus")]
    [Range(0f, 1f)]
    public float globalTowerDamageMultiplier = 0f;

    // NOT: Oyun bittiðinde kristal harcamalarý bu dosyadaki deðerleri kalýcý olarak artýracak.
}