using UnityEngine;

public class ResourceNode : MonoBehaviour
{
    [Header("Kaynak Bilgisi")]
    public string resourceType = "Wood";
    public int resourceAmount = 10; // Toplanacak toplam kaynak miktarý

    [Header("Kesme Ayarlarý")]
    [Tooltip("Kaynak toplama süresi (saniye)")]
    public float timeToChop = 3f;

    // Not: Bu range deðiþkenini kodda kullanmýyoruz çünkü SphereCollider kullanýyoruz, 
    // ama inspector'da bilgi olarak kalmasýnda sakýnca yok.
    public float harvestRange = 2.5f;

    private float currentChopTime = 0f;
    private bool isHarvesting = false;

    void Update()
    {
        // Eðer toplama iþlemi baþladýysa
        if (isHarvesting)
        {
            // Süreyi artýr (Kaldýðý yerden devam eder)
            currentChopTime += Time.deltaTime;

            // Debug ile süreyi konsoldan takip edebilirsin (Ýsteðe baðlý)
            // Debug.Log("Kesiliyor: " + currentChopTime);

            // Zaman dolduysa
            if (currentChopTime >= timeToChop)
            {
                CollectResources();
                Destroy(gameObject); // Kaynaðý yok et
                isHarvesting = false;
            }
        }
    }

    // Karakter görünmez çembere (Sphere Collider) girdiðinde
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isHarvesting = true;
            Debug.Log("Toplama Devam Ediyor: " + resourceType);
        }
    }

    // Karakter görünmez çemberden çýktýðýnda
    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isHarvesting = false;
            // DÝKKAT: currentChopTime = 0f; kodunu SÝLDÝK.
            // Artýk uzaklaþýnca süre sýfýrlanmýyor, olduðu yerde duruyor.

            Debug.Log("Toplama Duraklatýldý! (Ýlerleme Korunuyor)");
        }
    }

    void CollectResources()
    {
        // Toplanan kaynak tipine göre ilgili static fonksiyonu çaðýr
        if (resourceType == "Wood")
        {
            ResourceManager.AddWood(resourceAmount);
        }
        else if (resourceType == "Stone")
        {
            ResourceManager.AddStone(resourceAmount);
        }
        else
        {
            Debug.LogError("Bilinmeyen Kaynak Tipi: " + resourceType + " - Ýsmi kontrol ediniz.");
        }

        Debug.Log("Kaynak Baþarýyla Toplandý: " + resourceType);
    }
}