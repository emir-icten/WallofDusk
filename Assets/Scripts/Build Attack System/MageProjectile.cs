using System.Collections;
using UnityEngine;

public class MageProjectile : MonoBehaviour, IPoolable
{
    [Header("Hareket")]
    public float speed = 10f;
    public float lifeTime = 5f;

    [Header("Hasar & Alan")]
    public int damage = 10;
    public float explosionRadius = 3f;
    public string targetTag = "Enemy";
    public LayerMask enemyMask;

    [Header("Efektler")]
    public ParticleSystem impactEffect;

    private Vector3 moveDir;
    private bool hasExploded = false;
    private Transform owner;

    private Coroutine lifeCo;

    public void Init(
        Vector3 direction,
        int damageAmount,
        float radius,
        string targetTag,
        LayerMask enemyMask,
        Transform owner
    )
    {
        moveDir = direction.normalized;
        damage = damageAmount;
        explosionRadius = radius;
        this.targetTag = targetTag;
        this.enemyMask = enemyMask;
        this.owner = owner;
    }

    private void OnEnable()
    {
        hasExploded = false;
    }

    public void OnSpawned()
    {
        hasExploded = false;

        if (lifeCo != null) StopCoroutine(lifeCo);
        lifeCo = StartCoroutine(LifeTimer());
    }

    public void OnDespawned()
    {
        if (lifeCo != null) StopCoroutine(lifeCo);
        lifeCo = null;

        owner = null;
        moveDir = Vector3.zero;
        hasExploded = false;
    }

    private IEnumerator LifeTimer()
    {
        yield return new WaitForSeconds(lifeTime);
        DespawnSelf();
    }

    private void Update()
    {
        if (hasExploded) return;
        transform.position += moveDir * speed * Time.deltaTime;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasExploded) return;

        // kendi kulesine çarpmasın
        if (owner != null && (other.transform == owner || other.transform.IsChildOf(owner)))
            return;

        // hedef veya ground
        if (!other.CompareTag(targetTag) && !other.CompareTag("Ground"))
            return;

        Explode();
    }

    private void Explode()
    {
        hasExploded = true;

        if (impactEffect != null)
        {
            ParticleSystem fx = Instantiate(impactEffect, transform.position, Quaternion.identity);
            fx.Play();
            Destroy(fx.gameObject, fx.main.duration + fx.main.startLifetime.constantMax);
        }

        Collider[] hits;
        if (enemyMask.value != 0)
            hits = Physics.OverlapSphere(transform.position, explosionRadius, enemyMask);
        else
            hits = Physics.OverlapSphere(transform.position, explosionRadius);

        foreach (var hit in hits)
        {
            if (!hit.CompareTag(targetTag)) continue;

            Health h = hit.GetComponent<Health>();
            if (h != null && h.currentHealth > 0)
                h.TakeDamage(damage);
        }

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

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.6f, 0.3f, 1f, 0.4f);
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}
