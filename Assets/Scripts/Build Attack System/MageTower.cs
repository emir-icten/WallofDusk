using UnityEngine;

public class MageTower : MonoBehaviour
{
    [Header("Hedef Ayarları")]
    public float attackRange = 8f;
    public LayerMask enemyMask;
    public string enemyTag = "Enemy";

    [Header("Saldırı Ayarları")]
    public float attackInterval = 1.5f;
    public int damagePerHit = 8;          // Okçuya göre biraz düşük
    public float explosionRadius = 3f;

    [Header("Projectile Ayarları")]
    public Transform shootPoint;          // Büyü topunun çıkacağı nokta
    public GameObject magicProjectilePrefab;  // MageProjectile script'i olan prefab

    [Header("Efektler (opsiyonel)")]
    public ParticleSystem castEffect;     // Kuledeki "cast" efekti

    private float attackTimer = 0f;
    private Transform currentTarget;
    private Health currentTargetHealth;

    private void Update()
    {
        attackTimer += Time.deltaTime;

        // Geçersiz hedefi temizle
        if (currentTargetHealth == null || currentTargetHealth.currentHealth <= 0)
        {
            currentTarget = null;
            currentTargetHealth = null;
        }

        if (currentTarget == null)
        {
            FindTarget();
        }

        if (currentTarget == null) return;

        Vector3 toTarget = currentTarget.position - transform.position;
        float dist = toTarget.magnitude;

        // Hedef uzaklaştıysa bırak
        if (dist > attackRange * 1.3f)
        {
            currentTarget = null;
            currentTargetHealth = null;
            return;
        }

        // Kuleyi hedefe doğru döndür (isteğe bağlı)
        toTarget.y = 0f;
        if (toTarget.sqrMagnitude > 0.01f)
        {
            Quaternion lookRot = Quaternion.LookRotation(toTarget.normalized);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRot, 10f * Time.deltaTime);
        }

        // Saldırı zamanı geldiyse projectile gönder
        if (dist <= attackRange && attackTimer >= attackInterval)
        {
            ShootProjectile();
            attackTimer = 0f;
        }
    }

    private void FindTarget()
    {
        Collider[] hits;

        if (enemyMask.value != 0)
            hits = Physics.OverlapSphere(transform.position, attackRange, enemyMask);
        else
            hits = Physics.OverlapSphere(transform.position, attackRange);

        Transform best = null;
        Health bestHealth = null;
        float bestDistSqr = Mathf.Infinity;

        foreach (var hit in hits)
        {
            if (!hit.CompareTag(enemyTag)) continue;

            Health h = hit.GetComponent<Health>();
            if (h == null || h.currentHealth <= 0) continue;

            float sqr = (hit.transform.position - transform.position).sqrMagnitude;
            if (sqr < bestDistSqr)
            {
                bestDistSqr = sqr;
                best = hit.transform;
                bestHealth = h;
            }
        }

        currentTarget = best;
        currentTargetHealth = bestHealth;
    }

    private void ShootProjectile()
    {
        if (shootPoint == null || magicProjectilePrefab == null || currentTarget == null)
            return;

        // Kule cast ederken küçük bir efekt oynat
        if (castEffect != null)
            castEffect.Play();

        // Hedefin biraz göğüs hizasına nişan al
        Vector3 targetPos = currentTarget.position + Vector3.up * 1f;
        Vector3 dir = (targetPos - shootPoint.position).normalized;

        Quaternion rot = Quaternion.LookRotation(dir);
        GameObject projObj = Instantiate(magicProjectilePrefab, shootPoint.position, rot);

        MageProjectile proj = projObj.GetComponent<MageProjectile>();
        if (proj != null)
        {
            proj.Init(
                dir,
                damagePerHit,
                explosionRadius,
                enemyTag,
                enemyMask
            );
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.4f, 0.6f, 1f, 0.5f);
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
