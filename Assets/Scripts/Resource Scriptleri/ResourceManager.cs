using UnityEngine;

public class ResourceManager : MonoBehaviour
{
    public static ResourceManager instance;

    // === ENVANTER ===
    public static int WoodCount = 0;
    public static int StoneCount = 0;
    public static int Coins = 0;

    void Awake()
    {
        if (instance == null) instance = this;
    }

    // --- EKLEME FONKSÝYONLARI ---
    public static void AddWood(int amount)
    {
        WoodCount += amount;
        // UIManager varsa ekraný güncelle
        if (UIManager.instance != null)
            UIManager.instance.UpdateWoodUI(WoodCount);
    }

    public static void AddStone(int amount)
    {
        StoneCount += amount;
        if (UIManager.instance != null)
            UIManager.instance.UpdateStoneUI(StoneCount);
    }

    public static void AddCoin(int amount)
    {
        Coins += amount;
        if (UIManager.instance != null)
            UIManager.instance.UpdateCoinUI(Coins);
    }
}