using UnityEngine;

public class MageTower : MonoBehaviour
{
    [Header("Hedef Ayarları")]
    public float attackRange = 8f;
    public LayerMask enemyMask;
    public string enemyTag = "Enemy";

    [Header("Saldırı Ayarları")]
    public float attackInterval = 1.5f;
    public int damagePerHit = 8;
    public float explosionRadius = 3f;

    [Header("Projectile Ayarları")]
    public Transform shootPoint;                  // Büyü topunun çıkacağı nokta
    public GameObject magicProjectilePrefab;      // MageProjectile script'i olan prefab

    [Header("Efektler (opsiyonel)")]
    public ParticleSystem castEffect;             // Kuledeki "cast" efekti

    private float attackTimer = 0f;
    private Transform currentTarget;
    private Health currentTargetHealth;

    private void Update()
    {
        // Zaman sayacı
        attackTimer += Time.deltaTime;

        // 1) Hedef hâlâ geçerli mi?
        CleanupTarget();

        // 2) Hedef yoksa yenisini bul
        if (currentTarget == null)
        {
            FindTarget();
        }

        // 3) Hâlâ hedef yok → hiçbir şey yapma
        if (currentTarget == null)
            return;

        // 4) Hedef menzil içinde mi?
        float dist = Vector3.Distance(transform.position, currentTarget.position);

        // Hedef menzil dışına çıktıysa hedefi sıfırla (bir sonraki frame'de tekrar aranacak)
        if (dist > attackRange)
        {
            currentTarget = null;
            currentTargetHealth = null;
            return;
        }

        // 5) Saldırı süresi dolduysa ateş et
        if (attackTimer >= attackInterval)
        {
            ShootProjectile();
            attackTimer = 0f;
        }
    }

    /// <summary>
    /// Mevcut hedef ölmüş / yok olmuşsa temizler.
    /// </summary>
    private void CleanupTarget()
    {
        if (currentTargetHealth == null)
        {
            currentTarget = null;
            currentTargetHealth = null;
            return;
        }

        if (currentTargetHealth.currentHealth <= 0)
        {
            currentTarget = null;
            currentTargetHealth = null;
        }
    }

    /// <summary>
    /// Menzil içinden en yakın düşmanı seçer.
    /// </summary>
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
            if (!hit.CompareTag(enemyTag))
                continue;

            Health h = hit.GetComponent<Health>();
            if (h == null || h.currentHealth <= 0)
                continue;

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

        // Yeni hedef bulunduysa, yeni atış için sayacı sıfırla
        if (currentTarget != null)
        {
            attackTimer = 0f;
        }
    }

    /// <summary>
    /// Hedefe doğru bir büyü topu fırlatır.
    /// </summary>
   private void ShootProjectile()
{
    if (magicProjectilePrefab == null || currentTarget == null)
        return;

    // Çıkış noktası: varsa ShootPoint, yoksa kule merkezi
    Vector3 spawnPos = shootPoint != null ? shootPoint.position : transform.position;

    // Kule cast ederken küçük bir efekt oynat
    if (castEffect != null)
        castEffect.Play();

    // Hedefin biraz göğüs hizasına nişan al
    Vector3 targetPos = currentTarget.position + Vector3.up * 1f;
    Vector3 dir = (targetPos - spawnPos).normalized;

    Quaternion rot = Quaternion.LookRotation(dir);
    GameObject projObj = Instantiate(magicProjectilePrefab, spawnPos, rot);

    MageProjectile proj = projObj.GetComponent<MageProjectile>();
    if (proj != null)
    {
        proj.Init(
            dir,
            damagePerHit,
            explosionRadius,
            enemyTag,
            enemyMask,
            transform          // beni atan kule
        );
    }
}

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.4f, 0.6f, 1f, 0.5f);
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
