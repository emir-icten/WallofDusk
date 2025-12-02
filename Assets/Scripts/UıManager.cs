using UnityEngine;
using TMPro; // TextMeshPro kütüphanesi

public class UIManager : MonoBehaviour
{
    public static UIManager instance;

    [Header("Arayüzdeki Yazýlar (Otomatik Bulunur)")]
    public TextMeshProUGUI woodText;
    public TextMeshProUGUI stoneText;
    public TextMeshProUGUI coinText;

    void Awake()
    {
        // 1. Singleton (Tek Yetkili) Ayarý
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject); // Eðer baþka bir tane varsa kendini yok et
            return;
        }

        // 2. OTOMATÝK BAÐLANTI SÝSTEMÝ
        // Kod diyor ki: "Sahneye git, ismi tam olarak 'WoodText' olan objeyi bul ve onu benim woodText deðiþkenime baðla."

        if (woodText == null) // Eðer elle baðlanmamýþsa otomatik ara
        {
            GameObject foundObj = GameObject.Find("WoodText");
            if (foundObj != null)
                woodText = foundObj.GetComponent<TextMeshProUGUI>();
            else
                Debug.LogError("HATA: Sahnede 'WoodText' isminde bir obje bulunamadý!");
        }

        if (stoneText == null)
        {
            GameObject foundObj = GameObject.Find("StoneText");
            if (foundObj != null)
                stoneText = foundObj.GetComponent<TextMeshProUGUI>();
            else
                Debug.LogError("HATA: Sahnede 'StoneText' isminde bir obje bulunamadý!");
        }

        if (coinText == null)
        {
            GameObject foundObj = GameObject.Find("CoinText");
            if (foundObj != null)
                coinText = foundObj.GetComponent<TextMeshProUGUI>();
            else
                Debug.LogError("HATA: Sahnede 'CoinText' isminde bir obje bulunamadý!");
        }
    }

    // GÜNCELLEME FONKSÝYONLARI (Ayný Kalýyor)
    public void UpdateWoodUI(int currentAmount)
    {
        if (woodText != null) woodText.text = currentAmount.ToString();
    }

    public void UpdateStoneUI(int currentAmount)
    {
        if (stoneText != null) stoneText.text = currentAmount.ToString();
    }

    public void UpdateCoinUI(int currentAmount)
    {
        if (coinText != null) coinText.text = currentAmount.ToString();
    }
}