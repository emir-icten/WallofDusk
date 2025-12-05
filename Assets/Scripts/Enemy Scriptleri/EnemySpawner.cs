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

    // Rate mantığı için sayaç (interval yerine)
    private float spawnAccumulator = 0f;
    private bool lastIsNight = false;
    private int spawnedThisNight = 0;

    // Zorluk için temel değerler
    private int baseMaxAliveEnemies;
    private int baseMaxSpawnPerNight;

    // Kaçıncı gecedeyiz?
    private int currentNight = 0;
    public int CurrentNight => currentNight;   // NightUI bu değeri okuyacak

    private void Start()
    {
        // Inspector'da verdiğin ilk değerleri "1. gece temel" olarak kaydediyoruz
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
            isNight = !TimeManager.Instance.IsDay; // IsDay false ise gece
        }

        // Gece / gündüz geçişlerini takip et
        if (TimeManager.Instance != null)
        {
            if (isNight && !lastIsNight)
            {
                // === GECE BAŞLADI ===
                currentNight++;
                Debug.Log("Gece başladı. Gece numarası = " + currentNight);

                // Sayaçları sıfırla
                spawnedThisNight = 0;
                spawnAccumulator = 0f;
                aliveEnemies.RemoveAll(e => e == null);

                // Zorluk faktörü: 1. gece = 1, 2. gece = nightSpawnMultiplier, 3. gece = multiplier^2 ...
                float diffFactor = Mathf.Pow(nightSpawnMultiplier, Mathf.Max(0, currentNight - 1));

                maxAliveEnemies  = Mathf.RoundToInt(baseMaxAliveEnemies  * diffFactor);
                maxSpawnPerNight = Mathf.RoundToInt(baseMaxSpawnPerNight * diffFactor);

                Debug.Log($"Zorluk çarpanı = {diffFactor:F2}, maxAliveEnemies = {maxAliveEnemies}, maxSpawnPerNight = {maxSpawnPerNight}");
            }
            else if (!isNight && lastIsNight)
            {
                // === GÜNDÜZ BAŞLADI ===
                ClearEnemies();
                spawnAccumulator = 0f;
            }
        }

        lastIsNight = isNight;

        if (spawnOnlyAtNight && !isNight)
            return;

        if (useNightCurveSpawn)
        {
            UpdateContinuousSpawn(isNight);
        }
    }

    private void UpdateContinuousSpawn(bool isNight)
    {
        // Ölüleri listeden at
        aliveEnemies.RemoveAll(e => e == null);

        // Aynı anda sahnedeki maksimum düşman kontrolü
        if (maxAliveEnemies > 0 && aliveEnemies.Count >= maxAliveEnemies)
            return;

        // Gece başına toplam spawn limiti
        if (maxSpawnPerNight > 0 && spawnedThisNight >= maxSpawnPerNight)
            return;

        if (spawnOnlyAtNight && !isNight)
            return;

        float nightProgress = GetNightProgress();   // 0..1 (gece değilse 0)

        // Gecenin dışında isek spawnlama
        if (spawnOnlyAtNight && TimeManager.Instance != null && TimeManager.Instance.IsDay)
            return;

        // 🔥 Yarım sinüs eğrisi: 0 → 1 → 0
        // progress = 0   -> sin(0)   = 0
        // progress = 0.5 -> sin(π/2) = 1
        // progress = 1   -> sin(π)   = 0
        float curve = Mathf.Sin(nightProgress * Mathf.PI);   // 0..1..0

        // İstersen kenarlarda da biraz spawn olsun:
        // curve = Mathf.Clamp01(0.2f + 0.8f * curve);

        // Interval değerlerinden "saniye başına rate" türetiyoruz
        float edgeRate = 1f / Mathf.Max(0.001f, maxSpawnInterval); // gece başı/sonu
        float peakRate = 1f / Mathf.Max(0.001f, minSpawnInterval); // gecenin ortası

        // Eğriye göre gerçek spawnRate
        float spawnRatePerSecond = Mathf.Lerp(edgeRate, peakRate, curve);

        // Rate'i zamana göre biriktiriyoruz
        spawnAccumulator += spawnRatePerSecond * Time.deltaTime;

        // Biriktikçe 1'lik paketler halinde spawn denemesi yap
        while (spawnAccumulator >= 1f)
        {
            // Bu sırada limitleri tekrar kontrol et
            aliveEnemies.RemoveAll(e => e == null);

            if (maxAliveEnemies > 0 && aliveEnemies.Count >= maxAliveEnemies)
            {
                spawnAccumulator = 0f;
                break;
            }

            if (maxSpawnPerNight > 0 && spawnedThisNight >= maxSpawnPerNight)
            {
                spawnAccumulator = 0f;
                break;
            }

            if (TrySpawnOne())
            {
                spawnedThisNight++;
            }

            spawnAccumulator -= 1f;
        }
    }

    /// <summary>
    /// Gecenin başlangıcından şu ana kadar geçen oran (0=gece başı, 1=gece sonu).
    /// Gündüzse 0 döner.
    /// </summary>
    private float GetNightProgress()
    {
        if (TimeManager.Instance == null)
            return 0f;

        TimeManager tm = TimeManager.Instance;

        if (tm.IsDay)
            return 0f;

        float dayStart = tm.dayStartHour; // örn. 6
        float dayEnd   = tm.dayEndHour;   // örn. 18
        float t        = tm.currentTime;  // 0–24

        // Gece: [dayEnd, 24) U [0, dayStart)
        float nightLength = (24f - dayEnd) + dayStart;

        float timeSinceNightStart;
        if (t >= dayEnd)
        {
            timeSinceNightStart = t - dayEnd;
        }
        else
        {
            timeSinceNightStart = (24f - dayEnd) + t;
        }

        return Mathf.Clamp01(timeSinceNightStart / nightLength);
    }

    private bool TrySpawnOne()
    {
        Vector3 spawnPos;
        int safety = 0;

        // Base / player mesafesine göre geçerli bir nokta bulana kadar dene
        do
        {
            spawnPos = GetRandomPointOnPlane();
            safety++;
            if (safety > 40)
                return false; // Çok uğraşma, alan küçükse kilitlenmesin
        }
        while (!IsValidSpawnPosition(spawnPos));

        GameObject enemy = Instantiate(enemyPrefab, spawnPos, Quaternion.identity);
        aliveEnemies.Add(enemy);

        // EnemyAI varsa base'i hedef olarak ata
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

        float startY = b.max.y + 10f;
        Vector3 startPos = new Vector3(x, startY, z);

        if (Physics.Raycast(startPos, Vector3.down, out RaycastHit hit, 50f))
        {
            return hit.point;
        }

        return new Vector3(x, b.center.y, z);
    }

    private bool IsValidSpawnPosition(Vector3 pos)
    {
        Vector2 p2 = new Vector2(pos.x, pos.z);

        if (baseTransform != null)
        {
            Vector2 b2 = new Vector2(baseTransform.position.x, baseTransform.position.z);
            if (Vector2.Distance(p2, b2) < minDistanceFromBase)
                return false; // base'e fazla yakın
        }

        if (playerTransform != null)
        {
            Vector2 pl2 = new Vector2(playerTransform.position.x, playerTransform.position.z);
            if (Vector2.Distance(p2, pl2) < minDistanceFromPlayer)
                return false; // player'a fazla yakın
        }

        return true;
    }

    private void ClearEnemies()
    {
        foreach (GameObject enemy in aliveEnemies)
        {
            if (enemy != null)
                Destroy(enemy);
        }
        aliveEnemies.Clear();
    }
}
