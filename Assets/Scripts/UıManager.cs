using UnityEngine;
using TMPro; // TextMeshPro kütüphanesi þart

public class UIManager : MonoBehaviour
{
    public static UIManager instance;

    [Header("Arayüzdeki Yazýlar")]
    public TextMeshProUGUI woodText;
    public TextMeshProUGUI stoneText;
    public TextMeshProUGUI coinText;

    void Awake()
    {
        // Bu scripti herkesin ulaþabileceði "Tek Yetkili" yapýyoruz
        if (instance == null)
        {
            instance = this;
        }
    }

    // Odun yazýsýný günceller
    public void UpdateWoodUI(int currentAmount)
    {
        woodText.text = currentAmount.ToString();
    }

    // Taþ yazýsýný günceller
    public void UpdateStoneUI(int currentAmount)
    {
        stoneText.text = currentAmount.ToString();
    }

    // Coin yazýsýný günceller
    public void UpdateCoinUI(int currentAmount)
    {
        coinText.text = currentAmount.ToString();
    }
}