using System.Collections.Generic;
using UnityEngine;

public class ResourceSpawner : MonoBehaviour
{
    [Header("Kaynak Prefabları")]
    public GameObject woodPrefab;
    public GameObject stonePrefab;

    [Header("Spawn Miktarı")]
    public int woodPerDay = 10;
    public int stonePerDay = 6;

    [Header("Zemin Ayarı")]
    public Collider spawnArea;   // Plane veya Terrain collider

    [Header("Base Mesafe Ayarı")]
    public Transform baseTransform;
    public float minDistanceFromBase = 20f;

    [Header("Debug")]
    public bool showSpawnPoints = false;

    private bool lastIsDay = false;
    private readonly List<GameObject> spawnedResources = new();

    private void Start()
    {
        if (TimeManager.Instance != null)
        {
            lastIsDay = TimeManager.Instance.IsDay;

            if (lastIsDay)
                SpawnResourcesForNewDay();
        }
    }

    private void Update()
    {
        if (TimeManager.Instance == null) return;
        if (spawnArea == null) return;

        bool isDay = TimeManager.Instance.IsDay;

        // GECE -> GÜNDÜZ
        if (isDay && !lastIsDay)
        {
            ClearResources();
            SpawnResourcesForNewDay();
        }
        // GÜNDÜZ -> GECE
        else if (!isDay && lastIsDay)
        {
            ClearResources();
        }

        lastIsDay = isDay;
    }

    private void SpawnResourcesForNewDay()
    {
        SpawnBatch(woodPrefab, woodPerDay);
        SpawnBatch(stonePrefab, stonePerDay);
    }

    private void SpawnBatch(GameObject prefab, int count)
    {
        if (prefab == null) return;
        if (count <= 0) return;

        for (int i = 0; i < count; i++)
        {
            int safety = 0;
            Vector3 spawnPos;

            do
            {
                spawnPos = GetRandomPointOnPlane();
                safety++;
                if (safety > 40)
                    break;
            }
            while (!IsValidSpawnPosition(spawnPos));

            GameObject obj = Instantiate(prefab, spawnPos, Quaternion.identity);
            spawnedResources.Add(obj);
        }
    }

    private Vector3 GetRandomPointOnPlane()
    {
        Bounds b = spawnArea.bounds;

        float x = Random.Range(b.min.x, b.max.x);
        float z = Random.Range(b.min.z, b.max.z);

        float startY = b.max.y + 5f;
        Vector3 startPos = new Vector3(x, startY, z);

        if (Physics.Raycast(startPos, Vector3.down, out RaycastHit hit, 20f))
            return hit.point;

        return new Vector3(x, b.center.y, z);
    }

    private bool IsValidSpawnPosition(Vector3 pos)
    {
        if (baseTransform == null) return true;

        float dist = Vector2.Distance(
            new Vector2(pos.x, pos.z),
            new Vector2(baseTransform.position.x, baseTransform.position.z)
        );

        return dist >= minDistanceFromBase;
    }

    private void ClearResources()
    {
        foreach (GameObject obj in spawnedResources)
        {
            if (obj != null)
                Destroy(obj);
        }

        spawnedResources.Clear();
    }
}
