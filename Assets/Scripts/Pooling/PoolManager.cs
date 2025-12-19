using System;
using System.Collections.Generic;
using UnityEngine;

public class PoolManager : MonoBehaviour
{
    [Serializable]
    public class PoolConfig
    {
        public GameObject prefab;
        public int initialSize = 20;
        public bool expandable = true;
    }

    public static PoolManager Instance { get; private set; }

    [Header("Pools")]
    public List<PoolConfig> pools = new();

    private readonly Dictionary<GameObject, Queue<GameObject>> _poolMap = new();
    private readonly Dictionary<GameObject, Transform> _poolRoots = new();

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        foreach (var p in pools)
        {
            if (p.prefab == null) continue;
            CreatePool(p.prefab, p.initialSize);
        }
    }

    private void CreatePool(GameObject prefab, int count)
    {
        if (_poolMap.ContainsKey(prefab)) return;

        _poolMap[prefab] = new Queue<GameObject>(count);

        var root = new GameObject($"[Pool] {prefab.name}").transform;
        root.SetParent(transform);
        _poolRoots[prefab] = root;

        for (int i = 0; i < count; i++)
        {
            var obj = Instantiate(prefab, root);
            PrepareNewPooledObject(obj, prefab);
            obj.SetActive(false);
            _poolMap[prefab].Enqueue(obj);
        }
    }

    private void PrepareNewPooledObject(GameObject obj, GameObject prefabKey)
    {
        var po = obj.GetComponent<PooledObject>();
        if (po == null) po = obj.AddComponent<PooledObject>();
        po.prefabKey = prefabKey;
        po.owner = this;
    }

    public GameObject Spawn(GameObject prefab, Vector3 pos, Quaternion rot)
    {
        if (prefab == null) return null;

        if (!_poolMap.ContainsKey(prefab))
            CreatePool(prefab, 0);

        GameObject obj = null;
        var q = _poolMap[prefab];

        while (q.Count > 0 && obj == null)
            obj = q.Dequeue(); // null olmuşsa atla

        if (obj == null)
        {
            // expandable mı?
            var cfg = pools.Find(x => x.prefab == prefab);
            bool expandable = cfg == null || cfg.expandable;

            if (!expandable) return null;

            obj = Instantiate(prefab, _poolRoots[prefab]);
            PrepareNewPooledObject(obj, prefab);
        }

        obj.transform.SetPositionAndRotation(pos, rot);
        obj.SetActive(true);

        // IPoolable varsa haber ver
        foreach (var p in obj.GetComponentsInChildren<MonoBehaviour>(true))
            if (p is IPoolable ip) ip.OnSpawned();

        return obj;
    }

    public void Despawn(GameObject obj)
    {
        if (obj == null) return;

        var po = obj.GetComponent<PooledObject>();
        if (po == null || po.owner != this || po.prefabKey == null)
        {
            Destroy(obj);
            return;
        }

        // IPoolable varsa haber ver
        foreach (var p in obj.GetComponentsInChildren<MonoBehaviour>(true))
            if (p is IPoolable ip) ip.OnDespawned();

        obj.SetActive(false);
        obj.transform.SetParent(_poolRoots[po.prefabKey], true);
        _poolMap[po.prefabKey].Enqueue(obj);
    }
}
