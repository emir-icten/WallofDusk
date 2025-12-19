using System.Collections;
using UnityEngine;

public class AutoDespawn : MonoBehaviour, IPoolable
{
    public float lifeTime = 5f;
    private Coroutine _co;

    public void OnSpawned()
    {
        if (_co != null) StopCoroutine(_co);
        _co = StartCoroutine(DespawnLater());
    }

    public void OnDespawned()
    {
        if (_co != null) StopCoroutine(_co);
        _co = null;
    }

    private IEnumerator DespawnLater()
    {
        yield return new WaitForSeconds(lifeTime);
        if (PoolManager.Instance != null) PoolManager.Instance.Despawn(gameObject);
        else Destroy(gameObject);
    }
}
