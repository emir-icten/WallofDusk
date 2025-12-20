using UnityEngine;

public class PlayerMage : MonoBehaviour
{
    [Header("Hedef Bulma")]
    public float attackRange = 14f;
    public LayerMask enemyMask;         // Enemy layer seçili olmalı
    public string enemyTag = "Enemy";

    [Header("Büyü / Projectile")]
    public PlayerMageProjectile projectilePrefab;
    public Transform castPoint;         // Mage_CastPoint
    public float projectileSpeed = 18f;
    public float attackCooldown = 1.8f;
    public int damage = 35;
    public float explosionRadius = 2.5f;

    [Header("AOE Damage Falloff")]
    [Range(0.1f, 1f)] public float edgeDamageMultiplier = 0.5f;

    [Header("Dönüş")]
    public bool rotateTowardsTarget = true;
    public float rotateSpeed = 12f;

    [Header("Animasyon")]
    public Animator animator;
    public string castTrigger = "Cast";

    [Tooltip("Animation Event kullanmıyorsan, büyü bu gecikmeyle çıkar.")]
    public float castDelay = 0.15f;

    [Tooltip("Cast animinde Animation Event (AnimEvent_Fire) kullanıyorsan bunu aç.")]
    public bool useAnimationEvent = true;

    Transform currentTarget;
    float nextAttackTime;

    // Aynı cast içinde iki kez büyü atmasın diye
    bool firedThisCast;

    void Awake()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>();
    }

    void Update()
    {
        currentTarget = FindClosestEnemy();

        if (currentTarget == null)
            return;

        if (rotateTowardsTarget)
            RotateTowards(currentTarget);

        if (Time.time < nextAttackTime)
            return;

        // menzildeyse saldır
        float distSqr = (currentTarget.position - transform.position).sqrMagnitude;
        if (distSqr <= attackRange * attackRange)
        {
            StartCast();
        }
    }

    void StartCast()
    {
        nextAttackTime = Time.time + attackCooldown;
        firedThisCast = false;

        if (animator != null)
        {
            animator.ResetTrigger(castTrigger);
            animator.SetTrigger(castTrigger);
        }

        // Animation Event kullanmıyorsan gecikmeyle fırlat
        if (!useAnimationEvent)
        {
            CancelInvoke(nameof(FireFromDelay));
            Invoke(nameof(FireFromDelay), castDelay);
        }
    }

    void FireFromDelay()
    {
        if (firedThisCast) return;
        firedThisCast = true;

        FireAt(currentTarget);
    }

    /// <summary>
    /// Cast anim clipine event olarak bunu koy: AnimEvent_Fire()
    /// </summary>
    public void AnimEvent_Fire()
    {
        if (!useAnimationEvent) return; // delay modundaysan event çalışmasın
        if (firedThisCast) return;

        firedThisCast = true;
        FireAt(currentTarget);
    }

    void FireAt(Transform target)
    {
        if (projectilePrefab == null)
        {
            Debug.LogWarning("[PlayerMage] projectilePrefab boş.");
            return;
        }
        if (castPoint == null)
        {
            Debug.LogWarning("[PlayerMage] castPoint boş. Mage_CastPoint atamalısın.");
            return;
        }
        if (target == null) return;



        Debug.Log($"[Mage] FireAt! target={(target ? target.name : "NULL")} castPoint={(castPoint ? castPoint.name : "NULL")} prefab={(projectilePrefab ? projectilePrefab.name : "NULL")}");

        Vector3 dir = target.position - castPoint.position;
        dir.y = 0f;

        if (dir.sqrMagnitude < 0.001f) return;

        Quaternion rot = Quaternion.LookRotation(dir.normalized, Vector3.up);
        PlayerMageProjectile proj = Instantiate(projectilePrefab, castPoint.position, rot);

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

    Transform FindClosestEnemy()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, attackRange, enemyMask, QueryTriggerInteraction.Ignore);
        if (hits == null || hits.Length == 0) return null;

        float best = float.MaxValue;
        Transform bestT = null;

        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i] == null) continue;

            Transform t = hits[i].transform;
            if (!t.CompareTag(enemyTag)) continue;

            float d = (t.position - transform.position).sqrMagnitude;
            if (d < best)
            {
                best = d;
                bestT = t;
            }
        }

        return bestT;
    }

    void RotateTowards(Transform target)
    {
        Vector3 dir = target.position - transform.position;
        dir.y = 0f;

        if (dir.sqrMagnitude < 0.001f) return;

        Quaternion lookRot = Quaternion.LookRotation(dir.normalized, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRot, rotateSpeed * Time.deltaTime);
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
#endif
}
