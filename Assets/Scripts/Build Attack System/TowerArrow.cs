using System.Collections;
using UnityEngine;

public class TowerArrow : MonoBehaviour, IPoolable
{
    [Header("Hareket")]
    public float speed = 12f;
    public float lifeTime = 4f;

    [Header("Saldırı")]
    public int damage = 10;
    public string targetTag = "Enemy";

    private Coroutine lifeCo;

    public void OnSpawned()
    {
        if (lifeCo != null) StopCoroutine(lifeCo);
        lifeCo = StartCoroutine(LifeTimer());
    }

    public void OnDespawned()
    {
        if (lifeCo != null) StopCoroutine(lifeCo);
        lifeCo = null;
    }

    private IEnumerator LifeTimer()
    {
        yield return new WaitForSeconds(lifeTime);
        DespawnSelf();
    }

    private void Update()
    {
        transform.position += transform.forward * speed * Time.deltaTime;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(targetTag)) return;

        Health h = other.GetComponent<Health>();
        if (h != null && h.currentHealth > 0)
            h.TakeDamage(damage);

        DespawnSelf();
    }

    private void DespawnSelf()
    {
        if (lifeCo != null)
        {
            StopCoroutine(lifeCo);
            lifeCo = null;
        }

        if (PoolManager.Instance != null && GetComponent<PooledObject>() != null)
            PoolManager.Instance.Despawn(gameObject);
        else
            Destroy(gameObject);
    }
}
