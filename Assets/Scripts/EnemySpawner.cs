using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("Spawn Ayarları")]
    public GameObject enemyPrefab;         // Düşman prefabı
    public int maxEnemies = 10;            // Gece başında kaç düşman spawnlansın

    [Header("Spawn Alanı (Plane / Ground)")]
    [Tooltip("Düşmanların rastgele spawn olacağı plane / ground collider")]
    public Collider spawnArea;             // Zemin collider'ı

    [Header("Mesafe Ayarları")]
    public Transform baseTransform;        // Base binası
    public Transform playerTransform;      // Oyuncu
    public float minDistanceFromBase = 15f;   // Base'e en az bu kadar uzak
    public float minDistanceFromPlayer = 5f;  // Oyuncuya da çok yapışmasın

    [Header("Gündüz/Gece Kontrol")]
    public bool spawnOnlyAtNight = true;

    private bool lastIsNight = false;
    private readonly List<GameObject> spawnedEnemies = new List<GameObject>();

    private void Update()
    {
        if (enemyPrefab == null || spawnArea == null)
            return;

        bool isNight = true;

        if (TimeManager.Instance != null && spawnOnlyAtNight)
        {
            isNight = !TimeManager.Instance.IsDay; // IsDay false ise gece
        }

        // Geceye yeni girildi mi?
        if (spawnOnlyAtNight && TimeManager.Instance != null)
        {
            if (isNight && !lastIsNight)
            {
                // GECE BAŞLADI → TEK DALGA SPWAN
                SpawnWave();
            }
            else if (!isNight && lastIsNight)
            {
                // GÜNDÜZ BAŞLADI → TÜM DÜŞMANLARI TEMİZLE
                ClearEnemies();
            }
        }

        lastIsNight = isNight;

        // Eğer sadece geceleri spawn istiyorsak ve şu an gece değilse, update'ten çık
        if (!isNight && spawnOnlyAtNight)
            return;

        // Dilersen burada ekstra gece davranışları ekleyebilirsin
    }

    private void SpawnWave()
    {
        // Önce eski referansları temizle
        spawnedEnemies.RemoveAll(e => e == null);

        int toSpawn = Mathf.Max(0, maxEnemies - spawnedEnemies.Count);

        for (int i = 0; i < toSpawn; i++)
        {
            Vector3 spawnPos;
            int safety = 0;

            // Base / player mesafesine göre geçerli bir nokta bulana kadar dene
            do
            {
                spawnPos = GetRandomPointOnPlane();
                safety++;
                if (safety > 40)
                    break; // Çok uğraşma, alan küçükse kilitlenmesin
            }
            while (!IsValidSpawnPosition(spawnPos));

            GameObject enemy = Instantiate(enemyPrefab, spawnPos, Quaternion.identity);
            spawnedEnemies.Add(enemy);

            // EnemyAI varsa base'i hedef olarak ata
            EnemyAI ai = enemy.GetComponent<EnemyAI>();
            if (ai != null && baseTransform != null)
            {
                ai.target = baseTransform;
            }
        }
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
        foreach (GameObject enemy in spawnedEnemies)
        {
            if (enemy != null)
                Destroy(enemy);
        }
        spawnedEnemies.Clear();
    }
}
