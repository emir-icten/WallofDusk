using UnityEngine;

public class PlayerArcher : MonoBehaviour
{
    [Header("Refs")]
    public Animator animator;
    public Transform rotateRoot;
    public Transform shootPoint;

    [Header("Targeting")]
    public LayerMask enemyMask;
    public string enemyTag = "Enemy";
    public float acquireRange = 16f;
    public bool lockTargetDuringAttack = true;

    [Header("Attack")]
    public GameObject arrowPrefab;
    public float attackCooldown = 0.45f;
    public int baseArrowDamage = 10;

    [Header("Rotation While Attacking")]
    public bool rotateToTargetWhileAttacking = true;
    public float attackTurnSpeed = 18f;
    public float snapAngleThreshold = 35f;

    [Header("Animation")]
    public string drawTrigger = "Draw";
    public string shootTrigger = "Shoot";
    public float drawTime = 0.28f;
    public bool useAnimationEvent = false;

    [Header("Global Stats (Opsiyonel)")]
    public PlayerStatsSO globalStats;
    public bool autoFindGlobalStats = true;

    [Header("Debug")]
    public bool drawDebug = false;

    private float _nextAttackTime;
    private Transform _target;
    private bool _isAttacking;

    private readonly Collider[] _overlaps = new Collider[64];

    private void Awake()
    {
        if (!animator) animator = GetComponentInChildren<Animator>();
        if (!rotateRoot) rotateRoot = transform;
        if (!shootPoint) shootPoint = rotateRoot;

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
            RotateToward(_target, attackTurnSpeed, snapAngleThreshold);

        if (Time.time < _nextAttackTime) return;

        Transform candidate = _target;

        if (!_isAttacking || !lockTargetDuringAttack || candidate == null)
            candidate = AcquireTarget();

        if (candidate == null)
            return; // ✅ hedef yoksa anim yok

        _target = candidate;
        StartAttack();
    }

    private void CleanupDeadTarget()
    {
        if (_target == null) return;
        Health h = _target.GetComponentInParent<Health>();
        if (h == null || h.currentHealth <= 0) _target = null;
    }

    private void StartAttack()
    {
        if (_target == null) return; // ✅ güvenlik

        _nextAttackTime = Time.time + attackCooldown;
        _isAttacking = true;

        if (rotateToTargetWhileAttacking)
            RotateToward(_target, attackTurnSpeed * 2f, snapAngleThreshold);

        if (animator) animator.SetTrigger(drawTrigger);

        if (!useAnimationEvent)
        {
            CancelInvoke(nameof(ShootNow));
            Invoke(nameof(ShootNow), drawTime);
        }
    }

    public void AnimEvent_Shoot() => ShootNow();

    private void ShootNow()
    {
        // ✅ draw süresinde hedef öldüyse, shoot iptal
        CleanupDeadTarget();
        if (_target == null)
        {
            _isAttacking = false;
            return;
        }

        if (arrowPrefab == null || shootPoint == null)
        {
            _isAttacking = false;
            return;
        }

        if (animator) animator.SetTrigger(shootTrigger);

        GameObject arrowObj;
        if (PoolManager.Instance != null)
            arrowObj = PoolManager.Instance.Spawn(arrowPrefab, shootPoint.position, Quaternion.identity);
        else
            arrowObj = Instantiate(arrowPrefab, shootPoint.position, Quaternion.identity);

        if (arrowObj != null)
        {
            Vector3 dir = (_target.position - shootPoint.position);
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.001f) dir = shootPoint.forward;

            arrowObj.transform.rotation = Quaternion.LookRotation(dir.normalized, Vector3.up);

            ArrowProjectile proj = arrowObj.GetComponent<ArrowProjectile>();
            if (proj != null)
            {
                proj.damage = GetFinalDamage();
                proj.target = _target;
            }
        }

        _isAttacking = false;
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
        return Mathf.Max(1, Mathf.RoundToInt(baseArrowDamage * mult));
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawDebug) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, acquireRange);
    }
}
