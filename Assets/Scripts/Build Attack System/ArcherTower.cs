using UnityEngine;

public class ArcherTower : MonoBehaviour
{
    [Header("Hedef Ayarları")]
    public float attackRange = 8f;
    public LayerMask enemyMask;        // Enemy layer'i (yoksa 0 bırak, tag ile bulur)
    public string enemyTag = "Enemy";  // Düşmanların tag'i

    [Header("Saldırı Ayarları")]
    public float fireInterval = 1.0f;  // Kaç saniyede bir ok atar
    public int damagePerShot = 10;

    [Header("Ok Ayarları")]
    public Transform shootPoint;       // Okun çıkacağı nokta
    public GameObject arrowPrefab;     // TowerArrow script'i takılı prefab

    private float fireTimer = 0f;
    private Transform currentTarget;
    private Health currentTargetHealth;

    private void Update()
    {
        fireTimer += Time.deltaTime;

        // Geçersiz hedefi temizle
        if (currentTargetHealth == null || currentTargetHealth.currentHealth <= 0)
        {
            currentTarget = null;
            currentTargetHealth = null;
        }

        // Hedef yoksa yenisini bul
        if (currentTarget == null)
        {
            FindTarget();
        }

        if (currentTarget == null) return;

        // Hedefe olan mesafe
        Vector3 toTarget = currentTarget.position - transform.position;
        float dist = toTarget.magnitude;

        // Çok uzaklaşmışsa hedefi bırak
        if (dist > attackRange * 1.2f)   // biraz tolerans
        {
            currentTarget = null;
            currentTargetHealth = null;
            return;
        }

       

        // Menzil içindeyse ve ateş süresi dolduysa ok gönder
        if (dist <= attackRange && fireTimer >= fireInterval)
        {
            Shoot();
            fireTimer = 0f;
        }
    }

    private void FindTarget()
    {
        Transform best = null;
        Health bestHealth = null;
        float bestDistSqr = Mathf.Infinity;

        Collider[] hits;

        // Enemy layer'ı ayarlıysa onu kullan, yoksa tüm collider'lardan tag ile filtrele
        if (enemyMask.value != 0)
            hits = Physics.OverlapSphere(transform.position, attackRange, enemyMask);
        else
            hits = Physics.OverlapSphere(transform.position, attackRange);

        foreach (var hit in hits)
        {
            // Tag kontrolü
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

    private void Shoot()
    {
        if (shootPoint == null || arrowPrefab == null || currentTarget == null) return;

        // Hedefin gövde ortasına nişan al
        Vector3 targetPos = currentTarget.position + Vector3.up * 1f;
        Vector3 dir = (targetPos - shootPoint.position).normalized;

        Quaternion rot = Quaternion.LookRotation(dir);
        GameObject arrowObj = Instantiate(arrowPrefab, shootPoint.position, rot);

        // TowerArrow varsa damage'ini ayarla
        TowerArrow arrow = arrowObj.GetComponent<TowerArrow>();
        if (arrow != null)
        {
            arrow.damage = damagePerShot;
            arrow.targetTag = enemyTag;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
