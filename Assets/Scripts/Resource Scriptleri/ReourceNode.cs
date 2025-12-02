using UnityEngine;

public class ResourceNode : MonoBehaviour
{
    [Header("Kaynak Bilgisi")]
    public string resourceType = "Wood";
    public int totalResourceAmount = 40;

    [Header("Görsel Ayarlar")]
    public GameObject stumpPrefab;   // Kütük Prefabı
    public GameObject lootUIPrefab;  // Uçan Resim Prefabı

    [Header("Kesme Ayarları")]
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

        // Canvas'ı bul
        mainCanvas = FindFirstObjectByType<Canvas>();
    }

    void Update()
    {
        if (!isHarvesting) return;

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
            SpawnFlyingLoot(); // Son parçayı ver
            SpawnStump();      // Kütüğü yarat
            Destroy(gameObject); // Ağacı yok et
        }
    }

    // --- ANİMASYONLU ÖDÜL ---
    void SpawnFlyingLoot()
    {
        int amount = Mathf.RoundToInt(resourcePerSecond);

        // Eksik bir şey varsa, direkt ödül ver
        if (lootUIPrefab == null || mainCanvas == null || UIManager.instance == null)
        {
            GiveRewardDirectly(amount);
            return;
        }

        Vector3 targetPos = Vector3.zero;

        // Hedefi belirle
        if (resourceType == "Wood" && UIManager.instance.woodText != null)
            targetPos = UIManager.instance.woodText.transform.position;
        else if (resourceType == "Stone" && UIManager.instance.stoneText != null)
            targetPos = UIManager.instance.stoneText.transform.position;
        else
        {
            // Hedef UI yoksa, doğrudan ödül ver
            GiveRewardDirectly(amount);
            return;
        }

        // Uçan objeyi yarat
        GameObject lootObj = Instantiate(lootUIPrefab, mainCanvas.transform);

        // Ekranda ağacın olduğu yerde başlat
        Vector3 screenPos = Camera.main.WorldToScreenPoint(transform.position + Vector3.up * 2f);
        lootObj.transform.position = screenPos;

        // Kuryeyi gönder (LootProjectile scripti lazımsa)
        LootProjectile projectile = lootObj.GetComponent<LootProjectile>();
        if (projectile != null)
        {
            projectile.Setup(targetPos, amount, resourceType);
        }
        else
        {
            // LootProjectile yoksa, direkt ödül ver ve UI objesini öldür
            GiveRewardDirectly(amount);
            Destroy(lootObj);
        }
    }

    // --- YEDEK SİSTEM ---
    void GiveRewardDirectly(int amount)
    {
        if (amount <= 0) return;

        if (resourceType == "Wood")
        {
            ResourceManager.AddWood(amount);
        }
        else if (resourceType == "Stone")
        {
            ResourceManager.AddStone(amount);
        }
    }

    // --- KÜTÜK SİSTEMİ ---
    void SpawnStump()
    {
        if (stumpPrefab != null)
        {
            Instantiate(stumpPrefab, transform.position, transform.rotation);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
            isHarvesting = true;
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
            isHarvesting = false;
    }
}
