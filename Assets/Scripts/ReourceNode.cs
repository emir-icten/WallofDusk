using UnityEngine;

public class ResourceNode : MonoBehaviour
{
    [Header("Kaynak Bilgisi")]
    public string resourceType = "Wood";     // Wood veya Stone
    public int totalResourceAmount = 40;     // Toplam kaynak

    [Header("Kesme Ayarlarý")]
    public float timeToChop = 4f;            // Kaç saniyede bitecek

    private float resourcePerSecond;         // Saniyede verilecek miktar
    private float currentChopTime = 0f;
    private float oneSecondTimer = 0f;
    private bool isHarvesting = false;

    void Start()
    {
        // Saniyede kaç tane vereceðini hesapla
        if (timeToChop > 0)
            resourcePerSecond = totalResourceAmount / timeToChop;
        else
            resourcePerSecond = 1;
    }

    void Update()
    {
        if (isHarvesting)
        {
            // Zamanlayýcýlarý çalýþtýr
            currentChopTime += Time.deltaTime;
            oneSecondTimer += Time.deltaTime;

            // 1 Saniye dolduysa ödül ver
            if (oneSecondTimer >= 1.0f)
            {
                GiveReward();
                oneSecondTimer = 0f; // Saniyelik sayacý sýfýrla
            }

            // Aðacýn toplam ömrü bitti mi?
            if (currentChopTime >= timeToChop)
            {
                GiveReward();
                Destroy(gameObject); // Yok et
            }
        }
    }

    void GiveReward()
    {
        // Miktarý hesapla
        int amountToGive = Mathf.RoundToInt(resourcePerSecond);

        // Ýlgili kaynaðý ekle
        if (resourceType == "Wood")
        {
            if (ResourceManager.instance != null)
                ResourceManager.AddWood(amountToGive);
        }
        else if (resourceType == "Stone")
        {
            if (ResourceManager.instance != null)
                ResourceManager.AddStone(amountToGive);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player")) isHarvesting = true;
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player")) isHarvesting = false;
    }
}