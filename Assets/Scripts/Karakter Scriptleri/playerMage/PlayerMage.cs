using UnityEngine;

public class PlayerMage : MonoBehaviour
{
    [Header("Refs")]
    public Animator animator;
    public Transform rotateRoot;
    public Transform castPoint;

    [Header("Targeting")]
    public LayerMask enemyMask;
    public string enemyTag = "Enemy";
    public float acquireRange = 14f;
    public bool lockTargetDuringAttack = true;

    [Header("Attack")]
    public PlayerMageProjectile projectilePrefab;
    public float attackCooldown = 1.6f;
    public int baseDamage = 35;
    public float projectileSpeed = 18f;
    public float explosionRadius = 2.5f;

    [Header("Rotation While Casting")]
    public bool rotateToTargetWhileAttacking = true;
    public float attackTurnSpeed = 16f;
    public float snapAngleThreshold = 35f;

    [Header("Animation")]
    public string castTrigger = "Shoot";
    public bool useAnimationEvent = true;
    public float castDelay = 0.2f;
    public float castWindow = 0.35f;

    [Header("Global Stats (Opsiyonel)")]
    public PlayerStatsSO globalStats;
    public bool autoFindGlobalStats = true;

    [Header("Debug")]
    public bool drawDebug = false;

    private float _nextAttackTime;
    private Transform _target;
    private bool _isCasting;

    private readonly Collider[] _overlaps = new Collider[64];

    private void Awake()
    {
        if (!animator) animator = GetComponentInChildren<Animator>();
        if (!rotateRoot) rotateRoot = transform;
        if (!castPoint) castPoint = rotateRoot;

        if (autoFindGlobalStats && globalStats == null)
        {
            var all = Resources.FindObjectsOfTypeAll<PlayerStatsSO>();
            if (all != null && all.Length > 0) globalStats = all[0];
        }
    }

    private void Update()
    {
        CleanupDeadTarget();

        if (_isCasting && rotateToTargetWhileAttacking && _target != null)
            RotateToward(_target, attackTurnSpeed, snapAngleThreshold);

        if (Time.time < _nextAttackTime) return;

        Transform candidate = _target;

        if (!_isCasting || !lockTargetDuringAttack || candidate == null)
            candidate = AcquireTarget();

        if (candidate == null)
            return; // ✅ hedef yoksa cast anim yok

        _target = candidate;
        StartCast();
    }

    private void CleanupDeadTarget()
    {
        if (_target == null) return;
        Health h = _target.GetComponentInParent<Health>();
        if (h == null || h.currentHealth <= 0) _target = null;
    }

    private void StartCast()
    {
        if (_target == null) return; // ✅ güvenlik

        _nextAttackTime = Time.time + attackCooldown;
        _isCasting = true;

        if (rotateToTargetWhileAttacking)
            RotateToward(_target, attackTurnSpeed * 2f, snapAngleThreshold);

        if (animator) animator.SetTrigger(castTrigger);

        if (!useAnimationEvent)
        {
            CancelInvoke(nameof(FireNow));
            Invoke(nameof(FireNow), castDelay);
        }

        CancelInvoke(nameof(EndCastWindow));
        Invoke(nameof(EndCastWindow), Mathf.Max(0.05f, castWindow));
    }

    public void AnimEvent_Fire() => FireNow();
    public void AnimEvent_EndCast() => EndCastWindow();

    private void EndCastWindow()
    {
        _isCasting = false;
        if (!lockTargetDuringAttack) _target = null;
    }

    private void FireNow()
    {
        // ✅ cast sırasında hedef öldüyse fire iptal
        CleanupDeadTarget();
        if (_target == null) return;

        if (projectilePrefab == null || castPoint == null) return;

        PlayerMageProjectile proj = Instantiate(projectilePrefab, castPoint.position, Quaternion.identity);

        Vector3 dir = (_target.position - castPoint.position);
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.001f) dir = castPoint.forward;

        proj.transform.rotation = Quaternion.LookRotation(dir.normalized, Vector3.up);

        proj.speed = projectileSpeed;
        proj.baseDamage = GetFinalDamage();
        proj.radius = explosionRadius;
    }

    private Transform AcquireTarget()
    {
        Vector3 center = rotateRoot.position;
        int count = Physics.OverlapSphereNonAlloc(center, acquireRange, _overlaps, enemyMask, QueryTriggerInteraction.Ignore);

        float best = float.MaxValue;
        Transform bestT = null;

        for (int i = 0; i < count; i++)
        {
            Collider c = _overlaps[i];
            if (!c) continue;

            Transform t = c.transform;
            Transform enemyRoot = t.CompareTag(enemyTag) ? t : (t.parent != null && t.parent.CompareTag(enemyTag) ? t.parent : null);
            if (enemyRoot == null) continue;

            Health h = enemyRoot.GetComponentInParent<Health>();
            if (h == null || h.currentHealth <= 0) continue;

            float d = (enemyRoot.position - center).sqrMagnitude;
            if (d < best)
            {
                best = d;
                bestT = enemyRoot;
            }
        }

        return bestT;
    }

    private void RotateToward(Transform target, float speed, float snapThresholdDeg)
    {
        Vector3 dir = target.position - rotateRoot.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) return;

        Quaternion desired = Quaternion.LookRotation(dir.normalized, Vector3.up);

        if (snapThresholdDeg > 0f)
        {
            float angle = Quaternion.Angle(rotateRoot.rotation, desired);
            if (angle >= snapThresholdDeg)
            {
                rotateRoot.rotation = desired;
                return;
            }
        }

        rotateRoot.rotation = Quaternion.Slerp(rotateRoot.rotation, desired, speed * Time.deltaTime);
    }

    private int GetFinalDamage()
    {
        float mult = 1f;
        if (globalStats != null) mult += globalStats.globalDamageMultiplier;
        return Mathf.Max(1, Mathf.RoundToInt(baseDamage * mult));
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawDebug) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, acquireRange);
    }
}
