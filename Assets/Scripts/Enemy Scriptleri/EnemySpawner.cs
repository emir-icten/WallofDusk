using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    public static EnemySpawner Instance { get; private set; }

    [Header("--- Temel Ayarlar ---")]
    public GameObject enemyPrefab;
    [Tooltip("Düşmanların doğacağı alan (Tercihen Box Collider)")]
    public Collider spawnArea;
    [Tooltip("Raycast'in çarpacağı zemin katmanı")]
    public LayerMask groundLayer = ~0; 

    [Header("--- Referanslar ---")]
    public Transform baseTransform;
    public Transform playerTransform;

    [Header("--- Mesafe Kısıtlamaları ---")]
    public float minDistanceFromBase = 15f;
    public float minDistanceFromPlayer = 12f;

    [Header("--- Spawn Zamanlaması ---")]
    public float spawnInterval = 1.0f; // Temel bekleme süresi
    [Tooltip("Gece ilerledikçe spawn ne kadar hızlansın? (0 ile 1 arası eğri)")]
    public AnimationCurve nightIntensityCurve = AnimationCurve.Linear(0, 0, 1, 1);

    [Header("--- Zorluk ve Limitler (1. Gece) ---")]
    public int baseMaxAlive = 25;       // Aynı anda sahnede olabilecek max düşman
    public int baseTotalPerNight = 90;  // O gece toplam doğacak düşman
    
    [Header("--- Gece Zorluk Artışı (Çarpanlar) ---")]
    [Tooltip("Her gece spawn sayısı ve limitler % kaç artsın? (0.2 = %20)")]
    public float difficultyGrowthFactor = 0.2f; 
    public float hpGrowth = 0.15f;
    public float dmgGrowth = 0.10f;
    public float speedGrowth = 0.05f;

    [Header("--- Üst Limitler (Cap) ---")]
    public int capMaxAlive = 150;
    public int capTotalPerNight = 800;
    public float capStatMultiplier = 10f; // Can/Hasar max 10 katına çıkabilir

    [Header("--- Özel Etkinlikler ---")]
    public bool useBloodMoon = true;
    public int bloodMoonFrequency = 5; // 5 gecede bir
    [Range(0f, 1f)] public float burstSpawnChance = 0.15f; // %15 ihtimalle sürü doğar

    [Header("--- Temizlik ---")]
    public bool burnEnemiesAtSunrise = true;

    // --- State Variables ---
    private List<GameObject> activeEnemies = new List<GameObject>();
    private Coroutine spawnCoroutine;
    
    private int currentNight = 0;
    // DÜZELTME: Bu satır eksikti, diğer scriptlerin erişmesi için eklendi:
    public int CurrentNight => currentNight; 

    private int enemiesSpawnedTonight = 0;
    
    // O anki gecenin hesaplanmış limitleri
    private int currentMaxAlive;
    private int currentMaxTotal;
    private bool isBloodMoonActive = false;

    // Prefab'in saf değerleri
    private float rawHp, rawDmg, rawSpeed;

    private void Awake()
    {
        // Singleton Pattern
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        CacheBaseStats();
    }

    private void OnEnable()
    {
        if (TimeManager.Instance != null)
        {
            TimeManager.Instance.OnNightStart += StartNightRoutine;
            TimeManager.Instance.OnDayStart += StopNightRoutine;
        }
    }

    private void OnDisable()
    {
        if (TimeManager.Instance != null)
        {
            TimeManager.Instance.OnNightStart -= StartNightRoutine;
            TimeManager.Instance.OnDayStart -= StopNightRoutine;
        }
    }

    private void Start()
    {
        // Oyun başladığında zaten geceyse manuel başlat
        if (TimeManager.Instance != null && !TimeManager.Instance.IsDay)
        {
            StartNightRoutine();
        }
    }

    // --- Gece Döngüsü Başlatma ---
    private void StartNightRoutine()
    {
        currentNight++;
        enemiesSpawnedTonight = 0;
        
        // Liste temizliği (Ölüleri listeden at)
        activeEnemies.RemoveAll(x => x == null);

        // Zorluk Hesapla
        CalculateNightStats();

        // Varsa eski coroutine'i durdur, yenisini başlat
        if (spawnCoroutine != null) StopCoroutine(spawnCoroutine);
        spawnCoroutine = StartCoroutine(SpawnLoop());

        string msg = $"[EnemySpawner] Gece {currentNight} Başladı. Hedef: {currentMaxTotal} Düşman.";
        if (isBloodMoonActive) msg += " <color=red>!!! KANLI AY !!!</color>";
        Debug.Log(msg);
    }

    // --- Geceyi Bitirme ---
    private void StopNightRoutine()
    {
        if (spawnCoroutine != null) StopCoroutine(spawnCoroutine);
        
        Debug.Log("[EnemySpawner] Gündüz oldu. Spawn durduruldu.");

        if (burnEnemiesAtSunrise)
        {
            BurnAllEnemies();
        }
    }

    // --- ANA SPAWN DÖNGÜSÜ (Coroutine) ---
    private IEnumerator SpawnLoop()
    {
        // İlk başta kısa bir gecikme
        yield return new WaitForSeconds(1f);

        while (true)
        {
            // 1. Limit Kontrolü
            if (enemiesSpawnedTonight >= currentMaxTotal || activeEnemies.Count >= currentMaxAlive)
            {
                yield return new WaitForSeconds(0.5f);
                activeEnemies.RemoveAll(x => x == null || !x.activeInHierarchy);
                continue; 
            }

            // 2. Gece Yoğunluk Eğrisi
            float waitTime = spawnInterval;
            if (TimeManager.Instance != null)
            {
                float progress = GetNightProgress();
                float intensity = nightIntensityCurve.Evaluate(progress);
                waitTime = Mathf.Lerp(spawnInterval, spawnInterval * 0.3f, intensity);
            }

            if (isBloodMoonActive) waitTime *= 0.5f;

            // 3. Sürü (Burst) Kontrolü
            int countToSpawn = 1;
            if (Random.value < burstSpawnChance)
            {
                countToSpawn = Random.Range(2, 4); 
            }

            // 4. Spawn İşlemi
            for (int i = 0; i < countToSpawn; i++)
            {
                if (enemiesSpawnedTonight >= currentMaxTotal) break;
                if (activeEnemies.Count >= currentMaxAlive) break;

                TrySpawnEnemy();
                yield return new WaitForSeconds(0.1f);
            }

            yield return new WaitForSeconds(waitTime);
        }
    }

    private void TrySpawnEnemy()
    {
        Vector3 spawnPos;
        if (!FindValidSpawnPosition(out spawnPos)) return;

        GameObject newEnemy;

        if (PoolManager.Instance != null)
        {
            newEnemy = PoolManager.Instance.Spawn(enemyPrefab, spawnPos, Quaternion.identity);
        }
        else
        {
            newEnemy = Instantiate(enemyPrefab, spawnPos, Quaternion.identity);
        }

        if (newEnemy != null)
        {
            ApplyStats(newEnemy);
            activeEnemies.Add(newEnemy);
            enemiesSpawnedTonight++;
        }
    }

    // --- Yardımcı: Pozisyon Bulma ---
    private bool FindValidSpawnPosition(out Vector3 result)
    {
        result = Vector3.zero;
        if (spawnArea == null) return false;

        Bounds b = spawnArea.bounds;

        for (int i = 0; i < 15; i++)
        {
            float x = Random.Range(b.min.x, b.max.x);
            float z = Random.Range(b.min.z, b.max.z);
            
            Vector3 origin = new Vector3(x, b.max.y + 100f, z);

            if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 200f, groundLayer))
            {
                Vector3 candidate = hit.point;

                bool safeFromBase = baseTransform == null || Vector3.Distance(candidate, baseTransform.position) >= minDistanceFromBase;
                bool safeFromPlayer = playerTransform == null || Vector3.Distance(candidate, playerTransform.position) >= minDistanceFromPlayer;

                if (safeFromBase && safeFromPlayer)
                {
                    result = candidate;
                    return true;
                }
            }
        }
        return false;
    }

    // --- Zorluk Hesaplama Sistemi ---
    private void CalculateNightStats()
    {
        isBloodMoonActive = useBloodMoon && (currentNight > 0) && (currentNight % bloodMoonFrequency == 0);

        float multiplier = Mathf.Pow(1f + difficultyGrowthFactor, currentNight - 1);
        float limitMultiplier = isBloodMoonActive ? 1.5f : 1.0f;

        currentMaxAlive = Mathf.Clamp(Mathf.RoundToInt(baseMaxAlive * multiplier * limitMultiplier), 1, capMaxAlive);
        currentMaxTotal = Mathf.Clamp(Mathf.RoundToInt(baseTotalPerNight * multiplier * limitMultiplier), 1, capTotalPerNight);
    }

    private void ApplyStats(GameObject enemy)
    {
        float multiplier = Mathf.Pow(1f + difficultyGrowthFactor, currentNight - 1);
        float moonMult = isBloodMoonActive ? 1.3f : 1.0f; 

        int finalHp = Mathf.RoundToInt(rawHp * (1 + (hpGrowth * (currentNight - 1))) * moonMult);
        int finalDmg = Mathf.RoundToInt(rawDmg * (1 + (dmgGrowth * (currentNight - 1))) * moonMult);
        float finalSpeed = rawSpeed * (1 + (speedGrowth * (currentNight - 1)));
        if (isBloodMoonActive) finalSpeed *= 1.1f;

        finalHp = Mathf.Min(finalHp, Mathf.RoundToInt(rawHp * capStatMultiplier));
        finalDmg = Mathf.Min(finalDmg, Mathf.RoundToInt(rawDmg * capStatMultiplier));

        if (enemy.TryGetComponent(out Health h))
        {
            h.maxHealth = finalHp;
            h.currentHealth = finalHp;
        }

        if (enemy.TryGetComponent(out EnemyAI ai))
        {
            ai.attackDamage = finalDmg;
            ai.moveSpeed = finalSpeed;
            if (ai.baseTarget == null) ai.baseTarget = baseTransform;
        }
    }

    private void BurnAllEnemies()
    {
        for (int i = activeEnemies.Count - 1; i >= 0; i--)
        {
            if (activeEnemies[i] != null)
            {
                if (activeEnemies[i].TryGetComponent(out EnemySunBurn burner))
                {
                    burner.StartBurning();
                }
                else
                {
                    if (PoolManager.Instance != null) PoolManager.Instance.Despawn(activeEnemies[i]);
                    else Destroy(activeEnemies[i]);
                }
            }
        }
        activeEnemies.Clear();
    }

    private void CacheBaseStats()
    {
        if (enemyPrefab == null) return;
        if (enemyPrefab.TryGetComponent(out Health h)) rawHp = h.maxHealth;
        if (enemyPrefab.TryGetComponent(out EnemyAI ai))
        {
            rawDmg = ai.attackDamage;
            rawSpeed = ai.moveSpeed;
        }
    }

    private float GetNightProgress()
    {
        if (TimeManager.Instance == null) return 0f;
        
        float current = TimeManager.Instance.currentTime;
        float start = TimeManager.Instance.dayStartHour; 
        float end = TimeManager.Instance.dayEndHour;     

        float nightLength = (24f - end) + start;
        float timePassed = 0f;

        if (current >= end) timePassed = current - end;
        else timePassed = (24f - end) + current;

        return Mathf.Clamp01(timePassed / nightLength);
    }
}