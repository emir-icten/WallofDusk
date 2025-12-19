using UnityEngine;

public class ArrowProjectile : MonoBehaviour, IPoolable
{
    [Header("Ok Ayarları")]
    public float speed = 20f;
    public int damage = 10;
    public float lifeTime = 5f;

    [Tooltip("Hedefe ne kadar yaklaştığında 'vurmuş' sayalım?")]
    public float hitRadius = 0.7f;

    [Tooltip("Ok spawn olduktan sonra çarpmayı ne kadar geciktirelim? (kendine çarpmaması için)")]
    public float collisionDelay = 0.05f;

    [HideInInspector]
    public Transform target;

    private float age;
    private bool canCollide => age >= collisionDelay;

    public void OnSpawned()
    {
        age = 0f;
    }

    public void OnDespawned()
    {
        age = 0f;
        target = null;
    }

    private void Update()
    {
        age += Time.deltaTime;

        if (target != null)
        {
            Vector3 toTarget = target.position - transform.position;

            if (toTarget.magnitude <= hitRadius)
            {
                HitTarget(target);
                return;
            }

            Vector3 dir = toTarget.normalized;
            transform.position += dir * speed * Time.deltaTime;

            if (dir.sqrMagnitude > 0.0001f)
                transform.forward = dir;
        }
        else
        {
            transform.position += transform.forward * speed * Time.deltaTime;
        }

        if (age >= lifeTime)
        {
            DespawnSelf();
        }
    }

    private void HitTarget(Transform hitTransform)
    {
        if (hitTransform != null && hitTransform.CompareTag("Enemy"))
        {
            Health h = hitTransform.GetComponent<Health>();
            if (h != null)
                h.TakeDamage(damage);
        }

        DespawnSelf();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!canCollide) return;

        if (other.CompareTag("Enemy"))
        {
            HitTarget(other.transform);
            return;
        }

        if (other.CompareTag("Player"))
            return;

        if (!other.isTrigger)
        {
            DespawnSelf();
        }
    }

    private void DespawnSelf()
    {
        if (PoolManager.Instance != null && GetComponent<PooledObject>() != null)
            PoolManager.Instance.Despawn(gameObject);
        else
            Destroy(gameObject);
    }
}
