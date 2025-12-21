using UnityEngine;

public class PlayerMage : MonoBehaviour
{
    [Header("Hedef Bulma")]
    public float attackRange = 14f;
    public LayerMask enemyMask;
    public string enemyTag = "Enemy";

    [Header("Büyü / Projectile")]
    public PlayerMageProjectile projectilePrefab;
    public Transform castPoint;
    public float projectileSpeed = 18f;
    public float attackCooldown = 1.8f;
    public int damage = 35;
    public float explosionRadius = 2.5f;

    [Range(0.1f, 1f)]
    public float edgeDamageMultiplier = 0.5f;

    [Tooltip("Projectile'ın castPoint'ten ne kadar önde doğacağı (Base'e çarpmamak için)")]
    public float spawnForwardOffset = 0.35f;

    [Tooltip("Projectile'ın ne kadar yukarıda doğacağı (zemin/Base çarpışmalarını azaltır)")]
    public float spawnUpOffset = 1.0f;

    [Header("Dönüş")]
    public bool rotateTowardsTarget = true;
    public float rotateSpeed = 12f;

    [Header("Animasyon")]
    public Animator animator;
    public string castTrigger = "Cast";
    public float castDelay = 0.15f;
    public bool useAnimationEvent = true;

    // Runtime
    Transform currentTarget;
    float nextAttackTime;
    bool isCasting;

    void Reset()
    {
        animator = GetComponentInChildren<Animator>();
    }

    void Update()
    {
        // Güvenlik kontrolleri
        if (animator == null || castPoint == null || projectilePrefab == null)
            return;

        // Hedef seç
        currentTarget = FindClosestEnemyInRange();

        // Hedefe doğru dön
        if (rotateTowardsTarget && currentTarget != null)
            RotateTowards(currentTarget.position);

        // Saldırı koşulları
        if (currentTarget == null) return;
        if (Time.time < nextAttackTime) return;
        if (isCasting) return;

        StartCast();
    }

    void StartCast()
    {
        isCasting = true;
        nextAttackTime = Time.time + attackCooldown;

        // Animasyonu tetikle
        if (!string.IsNullOrEmpty(castTrigger))
            animator.SetTrigger(castTrigger);

        // Eğer anim event kullanmıyorsak, delay ile ateşle
        if (!useAnimationEvent)
            Invoke(nameof(FireFromDelay), castDelay);
    }

    void FireFromDelay()
    {
        // Delay geldiğinde hedef kaçmış olabilir
        if (currentTarget != null)
            FireAt(currentTarget);

        isCasting = false;
    }

    /// <summary>
    /// Animation Event veya Relay tarafından çağrılır.
    /// Cast anim klibinde AnimEvent_Fire event'i bu fonksiyona bağlanmalı.
    /// </summary>
    public void AnimEvent_Fire()
    {
        if (!useAnimationEvent)
            return; // yanlışlıkla iki kez ateşlemeyi engeller

        if (currentTarget != null)
            FireAt(currentTarget);

        isCasting = false;
    }

    void FireAt(Transform target)
    {
        if (projectilePrefab == null || castPoint == null || target == null)
            return;

        // Spawn pozisyonu: castPoint'in önüne ve yukarısına al (Base/Sato çarpışmasını azaltır)
        Vector3 spawnPos = castPoint.position
                         + castPoint.forward * spawnForwardOffset
                         + castPoint.up * spawnUpOffset;

        // Hedefin göğsüne doğru nişan al (Y sıfırlama yok)
        Vector3 targetPos = target.position + Vector3.up * 1.0f;
        Vector3 dir = targetPos - spawnPos;

        if (dir.sqrMagnitude < 0.0001f)
            dir = castPoint.forward;

        Quaternion rot = Quaternion.LookRotation(dir.normalized, Vector3.up);

        Debug.Log($"[Mage] FireAt! target={target.name} spawnPos={spawnPos} dir={dir.normalized}");

        // Instantiate
        PlayerMageProjectile proj = Instantiate(projectilePrefab, spawnPos, rot);

        // Init: owner çarpışmasını ignore eden sistem + hasar parametreleri
        proj.Init(
            owner: transform,
            speed: projectileSpeed,
            baseDamage: damage,
            radius: explosionRadius,
            enemyMask: enemyMask,
            enemyTag: enemyTag,
            edgeMultiplier: edgeDamageMultiplier
        );
    }

    Transform FindClosestEnemyInRange()
    {
        Collider[] hits = Physics.OverlapSphere(
            transform.position,
            attackRange,
            enemyMask,
            QueryTriggerInteraction.Ignore
        );

        Transform best = null;
        float bestDist = float.MaxValue;

        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i] == null) continue;
            if (!hits[i].CompareTag(enemyTag)) continue;

            float d = (hits[i].transform.position - transform.position).sqrMagnitude;
            if (d < bestDist)
            {
                bestDist = d;
                best = hits[i].transform;
            }
        }

        return best;
    }

    void RotateTowards(Vector3 worldPos)
    {
        Vector3 flatDir = worldPos - transform.position;
        flatDir.y = 0f;
        if (flatDir.sqrMagnitude < 0.0001f) return;

        Quaternion targetRot = Quaternion.LookRotation(flatDir.normalized, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotateSpeed * Time.deltaTime);
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
#endif
}
    