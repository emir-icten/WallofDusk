using UnityEngine;

public class PlayerArcher : MonoBehaviour
{
    [Header("Ok Ayarları")]
    public GameObject arrowPrefab;
    public Transform shootPoint;
    public float attackRange = 15f;
    public float fireRate = 0.25f;

    [Header("Animasyon")]
    public Animator animator;
    public string drawTrigger = "Draw";
    public string shootTrigger = "Shoot";

    [Tooltip("DrawBow klibinin süresi kadar. İlk atış bundan sonra başlar.")]
    public float drawDuration = 0.35f;

    [Header("Üst Gövde Aim (BlendTree)")]
    public string aimParam = "Aim";      // Float param (BlendTree 1D)
    public float aimAngleMax = 60f;      // 60 derece = Aim +-1
    public float aimSmooth = 10f;        // Yumuşatma hızı

    [Header("Hedefe Bakma (Root Dönüş)")]
    public bool rotateTowardsTarget = true;
    public float rotateSpeed = 10f;

    [Header("Görüş Hattı Ayarı")]
    [Range(0f, 2f)] public float targetHeightOffset = 1.5f;

    private float nextFireTime = 0f;
    private bool hadTarget = false;

    private float aimValue = 0f;

    void Awake()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>();
    }

    void Update()
    {
        if (arrowPrefab == null || shootPoint == null)
        {
            Debug.LogWarning("PlayerArcher: arrowPrefab veya shootPoint atanmadı!", this);
            SetAim(0f);
            hadTarget = false;
            return;
        }

        Transform target = FindLowestHealthVisibleEnemyInRange();
        bool hasTarget = target != null;

        // Hedef yoksa: aim sıfırla, state resetle
        if (!hasTarget)
        {
            hadTarget = false;
            SetAim(0f);
            return;
        }

        // Üst gövde aim (hedef sağ/sol)
        UpdateAimTowards(target);

        // Root dönüş (istersen kapatabilirsin, sadece üst gövdeyle de olur)
        if (rotateTowardsTarget)
            RotateRootTowards(target);

        // İlk kez hedef gördüysek: Draw 1 kere
        if (!hadTarget)
        {
            hadTarget = true;

            if (animator != null)
            {
                animator.ResetTrigger(drawTrigger);
                animator.SetTrigger(drawTrigger);
            }

            nextFireTime = Time.time + drawDuration;
            return;
        }

        // Seri atış
        if (Time.time >= nextFireTime)
        {
            if (animator != null)
            {
                animator.ResetTrigger(shootTrigger);
                animator.SetTrigger(shootTrigger);
            }

            ShootAt(target);
            nextFireTime = Time.time + fireRate;
        }
    }

    void UpdateAimTowards(Transform target)
    {
        Vector3 toTarget = target.position - transform.position;
        toTarget.y = 0f;

        if (toTarget.sqrMagnitude < 0.0001f)
        {
            SetAim(0f);
            return;
        }

        float signedAngle = Vector3.SignedAngle(transform.forward, toTarget.normalized, Vector3.up);
        float targetAim = Mathf.Clamp(signedAngle / aimAngleMax, -1f, 1f);

        aimValue = Mathf.Lerp(aimValue, targetAim, aimSmooth * Time.deltaTime);
        SetAim(aimValue);
    }

    void SetAim(float v)
    {
        if (animator == null) return;
        animator.SetFloat(aimParam, v);
    }

    void RotateRootTowards(Transform target)
    {
        Vector3 dir = target.position - transform.position;
        dir.y = 0f;

        if (dir.sqrMagnitude < 0.001f) return;

        Quaternion lookRot = Quaternion.LookRotation(dir.normalized, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRot, rotateSpeed * Time.deltaTime);
    }

    void ShootAt(Transform target)
    {
        Vector3 dir = target.position - shootPoint.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.001f) return;

        Quaternion rot = Quaternion.LookRotation(dir.normalized, Vector3.up);
        GameObject arrowObj = Instantiate(arrowPrefab, shootPoint.position, rot);

        ArrowProjectile proj = arrowObj.GetComponent<ArrowProjectile>();
        if (proj != null) proj.target = target;
    }

    // En düşük can yüzdesi + line of sight
    Transform FindLowestHealthVisibleEnemyInRange()
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

            if (!HasLineOfSight(hit.transform)) continue;

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

    bool HasLineOfSight(Transform enemy)
    {
        Vector3 origin = shootPoint.position;
        Vector3 targetPos = enemy.position + Vector3.up * targetHeightOffset;

        Vector3 dir = targetPos - origin;
        float dist = dir.magnitude;

        if (dist <= 0.01f) return true;
        dir /= dist;

        if (Physics.Raycast(origin, dir, out RaycastHit hit, dist, ~0, QueryTriggerInteraction.Ignore))
            return hit.collider.CompareTag("Enemy");

        return true;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
