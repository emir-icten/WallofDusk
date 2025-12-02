using UnityEngine;

public class ResourceManager : MonoBehaviour
{
    // === PARA BÝRÝMLERÝ ===
    public static int Coins = 0;
    public static int Crystals = 0;

    // === ENVANTER (HAMMADDE) ===
    public static int WoodCount = 0;
    public static int StoneCount = 0;

    // YENÝ EKLEME FONKSÝYONLARI:
    public static void AddWood(int amount)
    {
        WoodCount += amount;
        Debug.Log("Odun eklendi! Toplam: " + WoodCount);

        // Ekrana Haber Veriyoruz:
        if (UIManager.instance != null)
            UIManager.instance.UpdateWoodUI(WoodCount);
    }

    public static void AddStone(int amount)
    {
        StoneCount += amount;
        Debug.Log("Taþ eklendi! Toplam: " + StoneCount);

        // Ekrana Haber Veriyoruz:
        if (UIManager.instance != null)
            UIManager.instance.UpdateStoneUI(StoneCount);
    }

    public static void AddCoin(int amount)
    {
        Coins += amount;
        Debug.Log("Coin Eklendi. Toplam: " + Coins);

        // Ekrana Haber Veriyoruz:
        if (UIManager.instance != null)
            UIManager.instance.UpdateCoinUI(Coins);
    }

    public static void AddCrystals(int amount)
    {
        Crystals += amount;
    }
}