using UnityEngine;

public class ResourceNode : MonoBehaviour
{
    [Header("Kaynak Bilgisi")]
    public string resourceType = "Wood";
    public int resourceAmount = 10; // Toplanacak toplam Coin miktarý

    [Header("Kesme Ayarlarý")]
    [Tooltip("Kaynak toplama süresi (saniye)")]
    public float timeToChop = 3f;
    public float harvestRange = 2.5f; // Çemberin kod tarafýndaki ayarý

    private float currentChopTime = 0f;
    private bool isHarvesting = false;

    // Sadece Is Trigger kapalý olan ana Collider'ýn Start'ta Rigidbody'ye ihtiyacý var.
    // Biz burada manuel Sphere Collider eklediðimiz için Start'a gerek kalmadý.

    void Update()
    {
        // Eðer toplama iþlemi baþladýysa
        if (isHarvesting)
        {
            // Zamandan düþ
            currentChopTime += Time.deltaTime;

            // Zaman dolduysa
            if (currentChopTime >= timeToChop)
            {
                CollectResources();
                Destroy(gameObject);
                isHarvesting = false; // Ýþlemi durdur
            }
        }
    }

    // Karakter görünmez çembere (Sphere Collider) girdiðinde
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isHarvesting = true;
            Debug.Log("Toplama baþladý: " + resourceType);
        }
    }

    // Karakter görünmez çemberden çýktýðýnda
    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isHarvesting = false;
            currentChopTime = 0f; // Ýlerlemeyi sýfýrla
            Debug.Log("Toplama Durdu! Ýlerleme Sýfýrlandý.");
        }
    }

    // Artýk OnCollisionEnter'a ihtiyacýmýz yok, çünkü OnTrigger ile yapýyoruz.
    // Ancak oyuncu aðaca çarptýðýnda (katý çarpýþma) durmasýný istiyoruz,
    // o yüzden OnCollisionEnter'ý kaldýrdýk ve Trigger'ý ekledik.

    // ... (Diðer kodlar ve deðiþkenler ayný kalacak) ...

    void CollectResources()
    {
        // Toplanan kaynak tipine göre ilgili static fonksiyonu çaðýr
        if (resourceType == "Wood")
        {
            // Odunsa odun sayýsýný artýrýr
            ResourceManager.AddWood(resourceAmount);
        }
        else if (resourceType == "Stone")
        {
            // Taþsa taþ sayýsýný artýrýr
            ResourceManager.AddStone(resourceAmount);
        }
        else
        {
            // Tanýmlanmamýþ bir türse hata verir (Örn: "Wood" yerine "wood" yazarsan)
            Debug.LogError("Bilinmeyen Kaynak Tipi: " + resourceType + " - Kontrol ediniz.");
        }

        // Coin ekleme komutu tamamen kaldýrýldý.
        Debug.Log("Kaynak Baþarýyla Toplandý: " + resourceType);
    }
}