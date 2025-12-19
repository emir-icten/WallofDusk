using UnityEngine;

public class ResourceNode : MonoBehaviour
{
    [Header("Kaynak Tipi")]
    public ResourceType resourceType = ResourceType.Wood;

    [Header("Miktar / Süre")]
    public int totalResourceAmount = 40;
    public float timeToChop = 4f;

    [Header("UI Loot (opsiyonel)")]
    public GameObject lootUIPrefab;     // LootProjectile olan prefab
    public Canvas mainCanvas;           // boşsa otomatik bulur

    [Header("Stump (opsiyonel)")]
    public GameObject stumpPrefab;

    [Header("Tick Ayarı")]
    public float tickInterval = 1.0f;   // kaç saniyede bir vur/loot

    float resourcePerTick;
    float currentTime;
    float tickTimer;

    bool isHarvesting;
    PlayerHarvestTool currentHarvester;

    void Start()
    {
        if (!mainCanvas) mainCanvas = FindFirstObjectByType<Canvas>();

        // Her tick'te verilecek miktar: toplam / (timeToChop / tickInterval)
        float tickCount = Mathf.Max(1f, timeToChop / Mathf.Max(0.01f, tickInterval));
        resourcePerTick = totalResourceAmount / tickCount;
    }

    void Update()
    {
        if (!isHarvesting) return;

        currentTime += Time.deltaTime;
        tickTimer += Time.deltaTime;

        // Her tick'te 1 kez vur + loot
        if (tickTimer >= tickInterval)
        {
            Tick();
            tickTimer = 0f;
        }

        // süre dolduysa bitir
        if (currentTime >= timeToChop)
        {
            FinishAndDestroy();
        }
    }

    void Tick()
    {
        // Vuruş animasyonu
        if (currentHarvester != null)
            currentHarvester.OnHarvestTick();

        int amount = Mathf.RoundToInt(resourcePerTick);
        if (amount <= 0) amount = 1;

        // UI Loot varsa uçur, yoksa direkt ver
        if (lootUIPrefab != null && mainCanvas != null && UIManager.instance != null)
        {
            Vector3 targetPos;
            if (resourceType == ResourceType.Wood && UIManager.instance.woodText != null)
                targetPos = UIManager.instance.woodText.transform.position;
            else if (resourceType == ResourceType.Stone && UIManager.instance.stoneText != null)
                targetPos = UIManager.instance.stoneText.transform.position;
            else
            {
                GiveRewardDirectly(amount);
                return;
            }

            GameObject lootObj = Instantiate(lootUIPrefab, mainCanvas.transform);

            Camera cam = Camera.main;
            Vector3 worldStart = transform.position + Vector3.up * 2f;
            Vector3 screenPos = cam != null ? cam.WorldToScreenPoint(worldStart) : worldStart;
            lootObj.transform.position = screenPos;

            LootProjectile projectile = lootObj.GetComponent<LootProjectile>();
            if (projectile != null)
                projectile.Setup(targetPos, amount, resourceType.ToString()); // "Wood"/"Stone"
            else
            {
                GiveRewardDirectly(amount);
                Destroy(lootObj);
            }
        }
        else
        {
            GiveRewardDirectly(amount);
        }
    }

    void GiveRewardDirectly(int amount)
    {
        if (amount <= 0) return;

        if (resourceType == ResourceType.Wood) ResourceManager.AddWood(amount);
        else ResourceManager.AddStone(amount);
    }

    void FinishAndDestroy()
    {
        // Harvest kilitli kalmasın diye stop
        if (currentHarvester != null)
            currentHarvester.OnHarvestStop();

        if (stumpPrefab != null)
            Instantiate(stumpPrefab, transform.position, transform.rotation);

        Destroy(gameObject);
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        isHarvesting = true;
        currentTime = 0f;
        tickTimer = 0f;

        currentHarvester = other.GetComponentInParent<PlayerHarvestTool>();
        if (currentHarvester != null)
        {
            currentHarvester.OnHarvestStart(resourceType, transform);

            // İstersen ilk vuruş anında hemen oynasın:
            currentHarvester.OnHarvestTick();
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        isHarvesting = false;

        if (currentHarvester != null)
            currentHarvester.OnHarvestStop();

        currentHarvester = null;
    }
}
