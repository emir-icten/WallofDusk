using System.Collections.Generic;
using UnityEngine;

public class PlayerKnight : MonoBehaviour
{
    [Header("Refs")]
    public Animator animator;
    public PlayerMovementCC movement;
    public PlayerHarvestTool harvestTool;

    [Header("Hedef Bulma")]
    public float attackRange = 2.3f;
    public LayerMask enemyMask;
    public string enemyTag = "Enemy";

    [Header("Yakın Dövüş")]
    public int damage = 55;
    public float attackCooldown = 1.1f;
    public float hitRadius = 1.3f;
    public Transform hitPoint;
    public bool hitEachEnemyOnce = true;

    [Header("Dönüş")]
    public bool rotateTowardsTarget = true;
    public float rotateSpeed = 14f;

    [Header("Animasyon")]
    public string attackTrigger = "Shoot";
    public bool useAnimationEvent = true;
    public float attackDelay = 0.15f;

    [Header("Kontrol")]
    public bool disableCombatWhileHarvesting = true;

    [Header("Debug")]
    public bool debugLogs = true;
    public bool drawDebug = true;

    float _nextAttackTime;
    Transform _currentTarget;

    readonly HashSet<int> _hitIds = new HashSet<int>();

    void Awake()
    {
        if (!animator) animator = GetComponentInChildren<Animator>();
        if (!movement) movement = GetComponent<PlayerMovementCC>();
        if (!harvestTool) harvestTool = GetComponent<PlayerHarvestTool>();

        // Güvenlik: EnemyMask inspector'da boş gelirse otomatik düzelt
        if (enemyMask.value == 0)
        {
            int auto = LayerMask.GetMask("Enemy");
            if (auto != 0) enemyMask = auto;
        }
    }

    void Update()
    {
        if (disableCombatWhileHarvesting && harvestTool != null && harvestTool.IsHarvesting)
            return;

        if (Time.time < _nextAttackTime) return;

        // 1) Hedef bul
        _currentTarget = FindTarget();

        if (_currentTarget == null)
        {
            if (debugLogs)
            {
                Debug.Log($"[Knight] No target found. enemyMask={enemyMask.value} ({MaskToString(enemyMask)}) " +
                          $"attackRange={attackRange} center={(hitPoint ? hitPoint.position : transform.position)}");
            }
            return;
        }

        // 2) Hedefe dön
        if (rotateTowardsTarget)
            RotateTo(_currentTarget.position);

        // 3) Saldırı tetikle
        TryAttack();
    }

    Transform FindTarget()
    {
        Vector3 center = hitPoint ? hitPoint.position : transform.position;

        // Çok kritik: QueryTriggerInteraction.Collide kullanıyoruz (trigger colliderları da görsün)
        Collider[] cols = Physics.OverlapSphere(center, attackRange, enemyMask, QueryTriggerInteraction.Collide);

        if (debugLogs)
        {
            Debug.Log($"[Knight] OverlapSphere -> found {cols.Length} colliders. center={center} range={attackRange} " +
                      $"enemyMask={enemyMask.value} ({MaskToString(enemyMask)})");
        }

        // Hiç collider bulamazsa: TAG ile 1 kere fallback tarama (debug)
        if (cols.Length == 0)
        {
            // Bu kısım sadece sorunu teşhis etmek için var.
            GameObject[] tagged = GameObject.FindGameObjectsWithTag(enemyTag);
            if (debugLogs)
                Debug.Log($"[Knight][DebugFallback] FindGameObjectsWithTag('{enemyTag}') -> {tagged.Length} object");

            // Yakında olanı seç
            Transform best = null;
            float bestSqr = float.MaxValue;
            for (int i = 0; i < tagged.Length; i++)
            {
                float sqr = (tagged[i].transform.position - center).sqrMagnitude;
                if (sqr < bestSqr)
                {
                    bestSqr = sqr;
                    best = tagged[i].transform;
                }
            }

            // Yalnızca gerçekten yakınsa kabul et
            if (best != null && bestSqr <= attackRange * attackRange)
            {
                if (debugLogs)
                    Debug.Log($"[Knight][DebugFallback] Using TAG target: {best.name} dist={Mathf.Sqrt(bestSqr):0.00}");
                return best;
            }

            return null;
        }

        // Bulunan colliderlar arasında en yakın "Enemy tag" olanı seç
        Transform bestT = null;
        float bestDist = float.MaxValue;

        for (int i = 0; i < cols.Length; i++)
        {
            Collider c = cols[i];
            if (!c) continue;

            // Debug: gerçekten hangi colliderlar geliyor?
            if (debugLogs)
            {
                Debug.Log($"[Knight]   hit collider={c.name} root={c.transform.root.name} tag={c.tag} layer={LayerMask.LayerToName(c.gameObject.layer)}");
            }

            // Tag kontrolü: enemyTag ile eşleşsin
            if (!string.IsNullOrEmpty(enemyTag) && !c.CompareTag(enemyTag))
                continue;

            float d = Vector3.Distance(center, c.bounds.center);
            if (d < bestDist)
            {
                bestDist = d;
                bestT = c.transform;
            }
        }

        return bestT;
    }

    void TryAttack()
    {
        if (_currentTarget == null) return;

        _nextAttackTime = Time.time + attackCooldown;
        _hitIds.Clear();

        if (animator && !string.IsNullOrEmpty(attackTrigger))
        {
            animator.ResetTrigger(attackTrigger);
            animator.SetTrigger(attackTrigger);
        }

        if (debugLogs)
            Debug.Log($"[Knight] Attack triggered. target={_currentTarget.name}");

        if (!useAnimationEvent)
        {
            Invoke(nameof(DoHit), attackDelay);
        }
        // useAnimationEvent true ise anim event'ten DoHit çağrılmalı
    }

    // Animasyondan event ile çağır
    public void AnimEvent_Hit()
    {
        DoHit();
    }

    void DoHit()
    {
        if (!hitPoint) hitPoint = transform;

        Vector3 center = hitPoint.position;

        Collider[] hits = Physics.OverlapSphere(center, hitRadius, enemyMask, QueryTriggerInteraction.Collide);

        if (debugLogs)
            Debug.Log($"[Knight] DoHit overlap -> {hits.Length} colliders. hitPoint={center} hitRadius={hitRadius}");

        for (int i = 0; i < hits.Length; i++)
        {
            Collider c = hits[i];
            if (!c) continue;

            if (!string.IsNullOrEmpty(enemyTag) && !c.CompareTag(enemyTag))
                continue;

            // Aynı düşmana bir swing içinde 1 kere vur (opsiyon)
            int id = c.transform.root.GetInstanceID();
            if (hitEachEnemyOnce && _hitIds.Contains(id)) continue;
            _hitIds.Add(id);

            // Health araması: collider child'da olabilir, root'ta Health olabilir
            Health h = c.GetComponentInParent<Health>();
            if (h == null) h = c.GetComponent<Health>();

            if (h != null)
            {
                h.TakeDamage(damage);
                if (debugLogs) Debug.Log($"[Knight] HIT -> {h.name} dmg={damage} hpNow={h.currentHealth}");
            }
            else
            {
                if (debugLogs) Debug.Log($"[Knight] HIT but no Health found on {c.transform.root.name}");
            }
        }
    }

    void RotateTo(Vector3 worldPos)
    {
        Vector3 dir = worldPos - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) return;

        Quaternion targetRot = Quaternion.LookRotation(dir.normalized, Vector3.up);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, rotateSpeed * 360f * Time.deltaTime);
    }

    string MaskToString(LayerMask mask)
    {
        List<string> names = new List<string>();
        for (int i = 0; i < 32; i++)
        {
            if ((mask.value & (1 << i)) != 0)
            {
                string n = LayerMask.LayerToName(i);
                if (string.IsNullOrEmpty(n)) n = i.ToString();
                names.Add(n);
            }
        }
        return names.Count == 0 ? "Nothing" : string.Join(",", names);
    }

    void OnDrawGizmosSelected()
    {
        if (!drawDebug) return;

        Gizmos.color = Color.yellow;
        Vector3 center = hitPoint ? hitPoint.position : transform.position;
        Gizmos.DrawWireSphere(center, attackRange);

        Gizmos.color = Color.red;
        Vector3 hitC = hitPoint ? hitPoint.position : transform.position;
        Gizmos.DrawWireSphere(hitC, hitRadius);
    }
}
