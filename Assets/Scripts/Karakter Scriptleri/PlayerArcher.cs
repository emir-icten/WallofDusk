using UnityEngine;

public class PlayerArcher : MonoBehaviour
{
    [Header("Ok Ayarları")]
    public GameObject arrowPrefab;
    public Transform shootPoint;     // Okun çıkacağı nokta
    public float attackRange = 15f;  // Hedef arama yarıçapı
    public float fireRate = 0.7f;    // Atışlar arası süre (saniye)

    [Header("Hedefe Bakma")]
    public bool rotateTowardsTarget = true;
    public float rotateSpeed = 10f;

    [Header("Görüş Hattı Ayarı")]
    [Tooltip("Raycast hedefin neresine doğru atılsın? (0 = ayak, 1 = kafa)")]
    [Range(0f, 2f)] public float targetHeightOffset = 1.5f;

    private float nextFireTime = 0f;

    private void Update()
    {
        if (arrowPrefab == null || shootPoint == null)
        {
            Debug.LogWarning("PlayerArcher: arrowPrefab veya shootPoint atanmadı!", this);
            return;
        }

        // En düşük can yüzdesine sahip, GÖRÜLEBİLEN düşmanı bul
        Transform target = FindLowestHealthVisibleEnemyInRange();
        if (target == null)
        {
            return; // Menzilde, görünen uygun hedef yok
        }

        // Debug çizgi
        Debug.DrawLine(shootPoint.position, target.position + Vector3.up * targetHeightOffset, Color.red);

        // Hedefe dön
        Vector3 dir = target.position - transform.position;
        dir.y = 0f;

        if (rotateTowardsTarget && dir.sqrMagnitude > 0.001f)
        {
            Quaternion lookRot = Quaternion.LookRotation(dir.normalized, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRot, rotateSpeed * Time.deltaTime);
        }

        // Atış cooldown
        if (Time.time >= nextFireTime)
        {
            ShootAt(target);
            nextFireTime = Time.time + fireRate;
        }
    }

    private void ShootAt(Transform target)
    {
        Vector3 dir = target.position - shootPoint.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.001f) return;

        Quaternion rot = Quaternion.LookRotation(dir.normalized, Vector3.up);

        GameObject arrowObj = Instantiate(arrowPrefab, shootPoint.position, rot);

        ArrowProjectile proj = arrowObj.GetComponent<ArrowProjectile>();
        if (proj != null)
        {
            proj.target = target;
        }
    }

    // 🔥 En düşük can yüzdesi + line of sight
    private Transform FindLowestHealthVisibleEnemyInRange()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, attackRange);

        Transform bestTarget = null;
        float lowestHealthRatio = 1.1f;
        float bestDistSqr = Mathf.Infinity;

        foreach (var hit in hits)
        {
            if (!hit.CompareTag("Enemy")) continue;

            Health h = hit.GetComponent<Health>();
            if (h == null || h.currentHealth <= 0) continue;

            // Önce görüş hattı kontrolü
            if (!HasLineOfSight(hit.transform))
                continue;

            float ratio = (float)h.currentHealth / h.maxHealth;
            float distSqr = (hit.transform.position - transform.position).sqrMagnitude;

            if (ratio < lowestHealthRatio ||
                (Mathf.Approximately(ratio, lowestHealthRatio) && distSqr < bestDistSqr))
            {
                lowestHealthRatio = ratio;
                bestDistSqr = distSqr;
                bestTarget = hit.transform;
            }
        }

        return bestTarget;
    }

    // 👀 Görüş hattı kontrolü: Aradaki ilk collider Enemy mi?
    private bool HasLineOfSight(Transform enemy)
    {
        if (shootPoint == null) return false;

        Vector3 origin = shootPoint.position;
        Vector3 targetPos = enemy.position + Vector3.up * targetHeightOffset;
        Vector3 dir = targetPos - origin;
        float dist = dir.magnitude;

        if (dist <= 0.01f) return true;

        dir /= dist; // normalize

        if (Physics.Raycast(origin, dir, out RaycastHit hit, dist, ~0, QueryTriggerInteraction.Ignore))
        {
            // İlk çarpan şey düşmanın kendisiyse, görüş var
            if (hit.collider.CompareTag("Enemy"))
                return true;

            // Başka bir şeye çarptıysa (duvar, bina vs.) arada engel var demektir
            return false;
        }

        // Hiçbir şeye çarpmadıysa, boşlukta demektir → görüş var
        return true;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
