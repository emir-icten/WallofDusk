using UnityEngine;

public class ResourceManager : MonoBehaviour
{
    // === PARA BÝRÝMLERÝ (Yaratýklardan ve Meta Geliþimden Gelir) ===
    public static int Coins = 0;      // Düþmanlardan kazanýlan para (In-game Upgrade)
    public static int Crystals = 0;   // Kalýcý geliþim parasý (Meta Progression)

    // === ENVANTER (HAMMADDE - Sadece Kaynak Toplamadan Gelir) ===
    public static int WoodCount = 0;  // Odun Malzemesi
    public static int StoneCount = 0; // Taþ Malzemesi

    // YENÝ EKLEME FONKSÝYONLARI:
    public static void AddWood(int amount)
    {
        WoodCount += amount;
        Debug.Log("Odun eklendi! Toplam: " + WoodCount);
    }

    public static void AddStone(int amount)
    {
        StoneCount += amount;
        Debug.Log("Taþ eklendi! Toplam: " + StoneCount);
    }

    public static void AddCoin(int amount)
    {
        Coins += amount;
        Debug.Log("Coin Eklendi. Toplam: " + Coins);
    }

    // Kristal ekleme fonksiyonunu silmedik, ileride kullanacaðýz
    public static void AddCrystals(int amount)
    {
        Crystals += amount;
    }
}