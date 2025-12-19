using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("Spawn Ayarları")]
    public GameObject enemyPrefab;              // Düşman prefabı

    [Tooltip("Aynı anda sahnede bulunabilecek maksimum canlı düşman sayısı (1. gece için)")]
    public int maxAliveEnemies = 30;

    [Tooltip("Bir gece boyunca toplam kaç düşman spawn edilsin (1. gece için, 0 = sınırsız)")]
    public int maxSpawnPerNight = 100;

    [Header("Spawn Alanı (Plane / Ground)")]
    [Tooltip("Düşmanların rastgele spawn olacağı plane / ground collider")]
    public Collider spawnArea;                  // Zemin collider'ı

    [Header("Mesafe Ayarları")]
    public Transform baseTransform;             // Base binası
    public Transform playerTransform;           // Oyuncu
    public float minDistanceFromBase = 15f;     // Base'e en az bu kadar uzak
    public float minDistanceFromPlayer = 5f;    // Oyuncuya da çok yapışmasın

    [Header("Gündüz/Gece Kontrol")]
    [Tooltip("Sadece geceleri spawn olsun mu?")]
    public bool spawnOnlyAtNight = true;

    [Header("Gece Spawn Eğrisi")]
    [Tooltip("Gece boyunca sürekli spawn olsun mu? (Half-sin eğrisi)")]
    public bool useNightCurveSpawn = true;

    [Tooltip("Gece başı/sonu (en sakin anlarda) efektif interval (saniye)")]
    public float maxSpawnInterval = 5f;

    [Tooltip("Gecenin ortasında (en yoğun anda) efektif interval (saniye)")]
    public float minSpawnInterval = 1f;

    [Header("Zorluk / Gece İlerlemesi")]
    [Tooltip("Her yeni gecede spawn miktarını çarpmak için kullanılan katsayı (1 = sabit zorluk)")]
    public float nightSpawnMultiplier = 1.25f;   // her gece %25 daha fazla düşman

    private readonly List<GameObject> aliveEnemies = new List<GameObject>();

    // Rate mantığı için sayaç
    private float spawnAccumulator = 0f;

    // gece-gündüz geçiş takibi
    private bool lastIsNight = false;

    // gecelerde artan değerler için (1. gece temel)
    private int baseMaxAliveEnemies;
    private int baseMaxSpawnPerNight;

    // Kaçıncı gecedeyiz?
    private int currentNight = 0;
    public int CurrentNight => currentNight;   // NightsSurvivedText buradan okuyor

    private void Start()
    {
        baseMaxAliveEnemies = maxAliveEnemies;
        baseMaxSpawnPerNight = maxSpawnPerNight;
    }

    private void Update()
    {
        if (enemyPrefab == null || spawnArea == null)
            return;

        bool isNight = true;

        if (TimeManager.Instance != null && spawnOnlyAtNight)
        {
            isNight = !TimeManager.Instance.IsDay;
        }

        // Gece / gündüz geçişlerini takip et
        if (TimeManager.Instance != null)
        {
            if (isNight && !lastIsNight)
            {
                // === GECE BAŞLADI ===
                currentNight++;
                Debug.Log("Gece başladı. Gece numarası = " + currentNight);

                // Her gece zorluk artışı (alive / total spawn)
                float mult = Mathf.Pow(nightSpawnMultiplier, currentNight - 1);

                maxAliveEnemies = Mathf.Max(1, Mathf.RoundToInt(baseMaxAliveEnemies * mult));

                if (baseMaxSpawnPerNight <= 0)
                    maxSpawnPerNight = 0; // 0 = sınırsız
                else
                    maxSpawnPerNight = Mathf.Max(1, Mathf.RoundToInt(baseMaxSpawnPerNight * mult));

                spawnedThisNight = 0;
                spawnAccumulator = 0f;
            }

            if (!isNight && lastIsNight)
            {
                // === GÜNDÜZ BAŞLADI ===
                Debug.Log("Gündüz başladı. Düşmanlar güneşte yanacak.");

                // Gündüz başlarken düşmanları yak
                BurnAllAliveEnemies();
            }
        }

        lastIsNight = isNight;

        // Canlı listesinde null temizle
        for (int i = aliveEnemies.Count - 1; i >= 0; i--)
        {
            if (aliveEnemies[i] == null) aliveEnemies.RemoveAt(i);
        }

        // Sadece geceleri spawn
        if (spawnOnlyAtNight && !isNight)
            return;

        // gece değilse çık
        if (spawnOnlyAtNight && TimeManager.Instance != null && TimeManager.Instance.IsDay)
            return;

        // alive limit
        if (aliveEnemies.Count >= maxAliveEnemies)
            return;

        // toplam spawn limiti (0 = sınırsız)
        if (maxSpawnPerNight > 0 && spawnedThisNight >= maxSpawnPerNight)
            return;

        float nightProgress = GetNightProgress(); // 0..1

        float curve = 1f;
        if (useNightCurveSpawn)
        {
            // half-sin: 0->1->0
            curve = Mathf.Sin(nightProgress * Mathf.PI);
        }

        float edgeRate = 1f / Mathf.Max(0.001f, maxSpawnInterval);
        float peakRate = 1f / Mathf.Max(0.001f, minSpawnInterval);

        float spawnRatePerSecond = Mathf.Lerp(edgeRate, peakRate, curve);

        spawnAccumulator += spawnRatePerSecond * Time.deltaTime;

        while (spawnAccumulator >= 1f)
        {
            spawnAccumulator -= 1f;

            // tekrar limit kontrol
            if (aliveEnemies.Count >= maxAliveEnemies) break;
            if (maxSpawnPerNight > 0 && spawnedThisNight >= maxSpawnPerNight) break;

            if (TrySpawnOne())
            {
                spawnedThisNight++;
            }
        }
    }

    private int spawnedThisNight = 0;

    private float GetNightProgress()
    {
        if (TimeManager.Instance == null)
            return 0f;

        // TimeManager senin projende currentTime tutuyor
        float t = TimeManager.Instance.currentTime;
        float dayStart = TimeManager.Instance.dayStartHour;
        float dayEnd = TimeManager.Instance.dayEndHour;

        // gece uzunluğu: (24 - dayEnd) + dayStart
        float nightLength = (24f - dayEnd) + dayStart;

        float timeSinceNightStart;

        if (t >= dayEnd)
            timeSinceNightStart = t - dayEnd;
        else
            timeSinceNightStart = (24f - dayEnd) + t;

        return Mathf.Clamp01(timeSinceNightStart / Mathf.Max(0.0001f, nightLength));
    }

    private bool TrySpawnOne()
    {
        Vector3 spawnPos;
        int safety = 0;

        do
        {
            spawnPos = GetRandomPointOnPlane();
            safety++;
            if (safety > 40)
                return false;
        }
        while (!IsValidSpawnPosition(spawnPos));

        // ✅ Pooling: Instantiate yerine Spawn
        GameObject enemy =
            (PoolManager.Instance != null)
                ? PoolManager.Instance.Spawn(enemyPrefab, spawnPos, Quaternion.identity)
                : Instantiate(enemyPrefab, spawnPos, Quaternion.identity);

        aliveEnemies.Add(enemy);

        // EnemyAI varsa base hedefi ver
        EnemyAI ai = enemy.GetComponent<EnemyAI>();
        if (ai != null && baseTransform != null)
        {
            ai.baseTarget = baseTransform;
        }

        return true;
    }

    private Vector3 GetRandomPointOnPlane()
    {
        Bounds b = spawnArea.bounds;

        float x = Random.Range(b.min.x, b.max.x);
        float z = Random.Range(b.min.z, b.max.z);

        // y = üstten ray atıp zemine oturtma
        float y = b.max.y + 2f;
        Vector3 pos = new Vector3(x, y, z);

        // zemin üzerine indir
        if (Physics.Raycast(pos, Vector3.down, out RaycastHit hit, 50f))
        {
            pos.y = hit.point.y;
        }

        return pos;
    }

    private bool IsValidSpawnPosition(Vector3 pos)
    {
        if (baseTransform != null)
        {
            Vector2 p2 = new Vector2(pos.x, pos.z);
            Vector2 b2 = new Vector2(baseTransform.position.x, baseTransform.position.z);
            if (Vector2.Distance(p2, b2) < minDistanceFromBase)
                return false;
        }

        if (playerTransform != null)
        {
            Vector2 p2 = new Vector2(pos.x, pos.z);
            Vector2 pl2 = new Vector2(playerTransform.position.x, playerTransform.position.z);
            if (Vector2.Distance(p2, pl2) < minDistanceFromPlayer)
                return false;
        }

        return true;
    }

    private void BurnAllAliveEnemies()
    {
        foreach (GameObject enemy in aliveEnemies)
        {
            if (enemy == null) continue;

            EnemySunBurn burn = enemy.GetComponent<EnemySunBurn>();
            if (burn != null)
            {
                burn.StartBurning();
            }
            else
            {
                // Script yoksa: pooled ise despawn, değilse destroy
                if (PoolManager.Instance != null && enemy.GetComponent<PooledObject>() != null)
                    PoolManager.Instance.Despawn(enemy);
                else
                    Destroy(enemy);
            }
        }

        aliveEnemies.Clear();
    }
}
