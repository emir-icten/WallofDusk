using UnityEngine;

public class ResourceNode : MonoBehaviour
{
    [Header("Kaynak Bilgisi")]
    public string resourceType = "Wood";
    public int totalResourceAmount = 40;

    [Header("Görsel Ayarlar")]
    public GameObject stumpPrefab;   // Kütük Prefabý
    public GameObject lootUIPrefab;  // Uçan Resim Prefabý

    [Header("Kesme Ayarlarý")]
    public float timeToChop = 4f;

    private float resourcePerSecond;
    private float currentChopTime = 0f;
    private float oneSecondTimer = 0f;
    private bool isHarvesting = false;
    private Canvas mainCanvas;

    void Start()
    {
        // Saniyede kaç tane verecek
        if (timeToChop > 0)
            resourcePerSecond = totalResourceAmount / timeToChop;
        else
            resourcePerSecond = 1;

        // Canvas'ý bul
        mainCanvas = FindFirstObjectByType<Canvas>();
    }

    void Update()
    {
        if (isHarvesting)
        {
            currentChopTime += Time.deltaTime;
            oneSecondTimer += Time.deltaTime;

            // 1 saniye dolduysa PARÇA FIRLAT
            if (oneSecondTimer >= 1.0f)
            {
                SpawnFlyingLoot();
                oneSecondTimer = 0f;
            }

            // Ömür bitti mi?
            if (currentChopTime >= timeToChop)
            {
                SpawnFlyingLoot(); // Son parçayý ver
                SpawnStump();      // Kütüðü yarat
                Destroy(gameObject); // Aðacý yok et
            }
        }
    }

    // --- ANÝMASYONLU ÖDÜL ---
    void SpawnFlyingLoot()
    {
        // Eksik bir þey varsa hata vermesin, direkt puan versin
        if (lootUIPrefab == null || mainCanvas == null || UIManager.instance == null)
        {
            GiveRewardDirectly();
            return;
        }

        int amount = Mathf.RoundToInt(resourcePerSecond);
        Vector3 targetPos = Vector3.zero;

        // Hedefi belirle
        if (resourceType == "Wood" && UIManager.instance.woodText != null)
            targetPos = UIManager.instance.woodText.transform.position;
        else if (resourceType == "Stone" && UIManager.instance.stoneText != null)
            targetPos = UIManager.instance.stoneText.transform.position;

        // Uçan objeyi yarat
        GameObject lootObj = Instantiate(lootUIPrefab, mainCanvas.transform);

        // Ekranda aðacýn olduðu yerde baþlat
        Vector3 screenPos = Camera.main.WorldToScreenPoint(transform.position + Vector3.up * 2f);
        lootObj.transform.position = screenPos;

        // Kuryeyi gönder (LootProjectile scripti lazým!)
        LootProjectile projectile = lootObj.GetComponent<LootProjectile>();
        if (projectile != null)
        {
            projectile.Setup(targetPos, amount, resourceType);
        }
    }

    // --- YEDEK SÝSTEM (Eksik olan fonksiyon buydu) ---
    void GiveRewardDirectly()
    {
        int amount = Mathf.RoundToInt(resourcePerSecond);
        if (resourceType == "Wood")
        {
            if (ResourceManager.instance != null) ResourceManager.AddWood(amount);
        }
        else if (resourceType == "Stone")
        {
            if (ResourceManager.instance != null) ResourceManager.AddStone(amount);
        }
    }

    // --- KÜTÜK SÝSTEMÝ (Bu da eksikti) ---
    void SpawnStump()
    {
        if (stumpPrefab != null)
        {
            Instantiate(stumpPrefab, transform.position, transform.rotation);
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