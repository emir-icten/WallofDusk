using System.Collections.Generic;
using UnityEngine;

public class PlayerKnight : MonoBehaviour
{
    [Header("Refs")]
    public Animator animator;
    public Transform rotateRoot;

    [Header("Targeting")]
    public LayerMask enemyMask;
    public string enemyTag = "Enemy";
    public float acquireRange = 3.0f;
    public bool lockTargetDuringAttack = true;

    [Header("Attack")]
    public int baseDamage = 55;
    public float attackCooldown = 1.1f;
    public float hitRadius = 1.3f;
    public Transform hitPoint;
    public bool hitEachEnemyOnce = true;

    [Header("Rotation While Attacking")]
    public bool rotateToTargetWhileAttacking = true;
    public float attackTurnSpeed = 18f;
    public float snapAngleThreshold = 35f;

    [Header("Animation")]
    public string attackTrigger = "Shoot";
    public bool useAnimationEvent = true;
    public float hitDelay = 0.15f;

    [Header("Global Stats (Opsiyonel)")]
    public PlayerStatsSO globalStats;
    public bool autoFindGlobalStats = true;

    [Header("Debug")]
    public bool drawDebug = false;

    private float _nextAttackTime;
    private Transform _target;
    private bool _isAttacking;

    private readonly Collider[] _overlaps = new Collider[48];
    private readonly HashSet<int> _hitIds = new HashSet<int>();

    private void Awake()
    {
        if (!animator) animator = GetComponentInChildren<Animator>();
        if (!rotateRoot) rotateRoot = transform;
        if (!hitPoint) hitPoint = rotateRoot;

        if (autoFindGlobalStats && globalStats == null)
        {
            var all = Resources.FindObjectsOfTypeAll<PlayerStatsSO>();
            if (all != null && all.Length > 0) globalStats = all[0];
        }
    }

    private void Update()
    {
        CleanupDeadTarget();

        if (_isAttacking && rotateToTargetWhileAttacking && _target != null)
        {
            RotateToward(_target, attackTurnSpeed, snapAngleThreshold);
        }

        if (Time.time < _nextAttackTime) return;

        // ✅ hedef yoksa anim tetikleme yok
        Transform candidate = _target;

        if (!_isAttacking || !lockTargetDuringAttack || candidate == null)
            candidate = AcquireTarget();

        if (candidate == null)
            return; // ✅ hedef yok, hiçbir şey yapma

        _target = candidate;
        StartAttack(); // ✅ hedef var, saldır
    }

    private void CleanupDeadTarget()
    {
        if (_target == null) return;

        Health h = _target.GetComponentInParent<Health>();
        if (h == null || h.currentHealth <= 0) _target = null;
    }

    private void StartAttack()
    {
        // ✅ son güvenlik: hedef yoksa kesinlikle anim yok
        if (_target == null) return;

        _nextAttackTime = Time.time + attackCooldown;
        _isAttacking = true;
        _hitIds.Clear();

        if (rotateToTargetWhileAttacking)
            RotateToward(_target, attackTurnSpeed * 2f, snapAngleThreshold);

        if (animator) animator.SetTrigger(attackTrigger);

        if (!useAnimationEvent)
        {
            CancelInvoke(nameof(DoHit));
            Invoke(nameof(DoHit), hitDelay);

            CancelInvoke(nameof(EndAttackWindow));
            Invoke(nameof(EndAttackWindow), Mathf.Max(0.1f, hitDelay + 0.1f));
        }
    }

    public void AnimEvent_Hit() => DoHit();
    public void AnimEvent_EndAttack() => EndAttackWindow();

    private void EndAttackWindow()
    {
        _isAttacking = false;
        if (!lockTargetDuringAttack) _target = null;
    }

    private void DoHit()
    {
        Vector3 center = hitPoint ? hitPoint.position : rotateRoot.position;
        int count = Physics.OverlapSphereNonAlloc(center, hitRadius, _overlaps, enemyMask, QueryTriggerInteraction.Ignore);

        int dmg = GetFinalDamage();

        for (int i = 0; i < count; i++)
        {
            Collider c = _overlaps[i];
            if (!c) continue;

            Transform t = c.transform;
            Transform enemyRoot = t.CompareTag(enemyTag) ? t : (t.parent != null && t.parent.CompareTag(enemyTag) ? t.parent : null);
            if (enemyRoot == null) continue;

            Health h = c.GetComponentInParent<Health>();
            if (h == null || h.currentHealth <= 0) continue;

            int id = h.gameObject.GetInstanceID();
            if (hitEachEnemyOnce && _hitIds.Contains(id)) continue;

            h.TakeDamage(dmg);
            _hitIds.Add(id);
        }

        if (useAnimationEvent)
        {
            CancelInvoke(nameof(EndAttackWindow));
            Invoke(nameof(EndAttackWindow), 0.15f);
        }
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

        Gizmos.color = Color.red;
        Vector3 p = hitPoint ? hitPoint.position : transform.position;
        Gizmos.DrawWireSphere(p, hitRadius);
    }
}
