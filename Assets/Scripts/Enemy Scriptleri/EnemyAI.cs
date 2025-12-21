using UnityEngine;

[RequireComponent(typeof(Health))]
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
public class EnemyAI : MonoBehaviour
{
    [Header("Ana Hedef (Base)")]
    public Transform baseTarget;

    [Header("Hareket Ayarları")]
    public float moveSpeed = 3f;
    public float stopDistance = 2f;

    [Header("Saldırı Ayarları")]
    public int attackDamage = 5;
    public float attackInterval = 1.5f;
    public float attackRange = 2.5f;

    [Header("Yapı Hedefleme")]
    public float structureDetectRadius = 6f;
    public string[] structureTags = { "Base", "Building", "Tower" };

    [Header("Duvar Ayarları")]
    public string wallTag = "Wall";
    public float wallTargetStickTime = 2f;

    [Header("Retarget")]
    public float retargetInterval = 0.5f;

    [Header("Çarpışma / Duvar İçi Önleme")]
    [Tooltip("Enemy'nin çarpışması gereken katmanlar: Wall/Building/Base gibi. Player'ı BURAYA koyma.")]
    public LayerMask obstacleMask;

    [Tooltip("Cast ile collider arasında bırakılacak min boşluk (duvara yapışmasın)")]
    public float collisionSkin = 0.03f;

    private Transform currentTarget;
    private Health currentTargetHealth;
    private float attackTimer = 0f;

    private Health selfHealth;
    private float retargetTimer = 0f;

    private Transform lastWallTarget;
    private float lastWallTime = -999f;

    // Components
    private Rigidbody rb;
    private CapsuleCollider cap;

    private void Awake()
    {
        selfHealth = GetComponent<Health>();
        rb = GetComponent<Rigidbody>();
        cap = GetComponent<CapsuleCollider>();

        // Player'ı itmesin diye kinematic kalabilir
        rb.isKinematic = true;
        rb.useGravity = false;

        // Kinematic + MovePosition için iyi bir seçenek
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
    }

    private void Start()
    {
        if (baseTarget != null)
        {
            currentTarget = baseTarget;
            currentTargetHealth = baseTarget.GetComponent<Health>();
        }
    }

    private void Update()
    {
        if (selfHealth != null && selfHealth.currentHealth <= 0)
            return;

        if (currentTargetHealth == null || currentTargetHealth.currentHealth <= 0)
        {
            currentTarget = null;
            currentTargetHealth = null;
        }

        retargetTimer += Time.deltaTime;
        if (retargetTimer >= retargetInterval)
        {
            retargetTimer = 0f;
            UpdateTarget();
        }

        if (currentTarget == null) return;

        // Hedefe yönel
        Vector3 toTarget = currentTarget.position - transform.position;
        toTarget.y = 0f;

        float dist = toTarget.magnitude;

        // 1) Hedefe doğru yürü (duvara girmeyecek şekilde)
        if (dist > stopDistance)
        {
            Vector3 moveDir = (dist > 0.0001f) ? (toTarget / dist) : Vector3.zero;
            Vector3 desiredMove = moveDir * moveSpeed * Time.deltaTime;

            Vector3 finalMove = ClipMoveByCapsuleCast(desiredMove);

            if (finalMove.sqrMagnitude > 0f)
            {
                rb.MovePosition(rb.position + finalMove);

                // Bakış yönü
                if (moveDir.sqrMagnitude > 0.001f)
                {
                    Quaternion lookRot = Quaternion.LookRotation(moveDir);
                    transform.rotation = Quaternion.Slerp(transform.rotation, lookRot, 10f * Time.deltaTime);
                }
            }
        }

        // 2) Saldırı menzilindeyse vur
        if (currentTargetHealth != null && dist <= attackRange)
        {
            attackTimer += Time.deltaTime;
            if (attackTimer >= attackInterval)
            {
                attackTimer = 0f;
                currentTargetHealth.TakeDamage(attackDamage);
            }
        }
    }

    /// <summary>
    /// Enemy capsule'ını kullanarak duvar/building içine girmeden hareketi kırpar.
    /// </summary>
    private Vector3 ClipMoveByCapsuleCast(Vector3 desiredMove)
    {
        if (desiredMove.sqrMagnitude < 0.0000001f)
            return Vector3.zero;

        Vector3 dir = desiredMove.normalized;
        float distance = desiredMove.magnitude;

        // Capsule world noktaları
        // Unity capsule collider local axis'i: direction (0=X,1=Y,2=Z)
        // Sende çoğunlukla Y ekseni olur. Biz güvenli şekilde hesaplayalım.
        float radius = Mathf.Max(0.01f, cap.radius * Mathf.Max(transform.lossyScale.x, transform.lossyScale.z));
        float height = Mathf.Max(radius * 2f, cap.height * transform.lossyScale.y);

        Vector3 center = transform.TransformPoint(cap.center);

        // Y eksenli capsule varsayımı (senin enemylerde öyle)
        float half = Mathf.Max(0f, (height * 0.5f) - radius);

        Vector3 p1 = center + Vector3.up * half;
        Vector3 p2 = center - Vector3.up * half;

        // Kısa mesafe cast
        if (Physics.CapsuleCast(p2, p1, radius, dir, out RaycastHit hit, distance + collisionSkin, obstacleMask, QueryTriggerInteraction.Ignore))
        {
            // Duvara girmeden en fazla şu kadar ilerle
            float allowed = Mathf.Max(0f, hit.distance - collisionSkin);
            return dir * allowed;
        }

        return desiredMove;
    }

    private void UpdateTarget()
    {
        // Eğer kısa süre önce duvara çarptıysak ve duvar hala yaşıyorsa, hedefi koru
        if (lastWallTarget != null && Time.time - lastWallTime <= wallTargetStickTime)
        {
            Health wallHealth = lastWallTarget.GetComponent<Health>();
            if (wallHealth != null && wallHealth.currentHealth > 0)
            {
                currentTarget = lastWallTarget;
                currentTargetHealth = wallHealth;
                return;
            }
        }

        Transform best = null;
        Health bestHealth = null;
        float bestDistSqr = Mathf.Infinity;

        Vector3 myPos = transform.position;

        // 1) Önce etraftaki yapıları ara (Base, Tower vs) – duvar HARİÇ
        Collider[] hits = Physics.OverlapSphere(myPos, structureDetectRadius);
        foreach (var hit in hits)
        {
            if (hit == null) continue;
            Transform t = hit.transform;

            // Duvarları bu taramada es geçiyoruz
            if (t.CompareTag(wallTag))
                continue;

            // Tag uygun mu?
            if (!HasValidStructureTag(t.tag))
                continue;

            Health h = t.GetComponent<Health>();
            if (h == null || h.currentHealth <= 0)
                continue;

            float sqr = (t.position - myPos).sqrMagnitude;
            if (sqr < bestDistSqr)
            {
                bestDistSqr = sqr;
                best = t;
                bestHealth = h;
            }
        }

        if (best != null)
        {
            currentTarget = best;
            currentTargetHealth = bestHealth;
        }
        else
        {
            if (baseTarget != null)
            {
                currentTarget = baseTarget;
                currentTargetHealth = baseTarget.GetComponent<Health>();
            }
            else
            {
                currentTarget = null;
                currentTargetHealth = null;
            }
        }
    }

    private bool HasValidStructureTag(string tagToCheck)
    {
        if (structureTags == null) return false;
        for (int i = 0; i < structureTags.Length; i++)
        {
            if (tagToCheck == structureTags[i])
                return true;
        }
        return false;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.collider != null && collision.collider.CompareTag(wallTag))
        {
            Transform wall = collision.collider.transform;
            Health h = wall.GetComponent<Health>();
            if (h != null && h.currentHealth > 0)
            {
                lastWallTarget = wall;
                lastWallTime = Time.time;

                currentTarget = wall;
                currentTargetHealth = h;
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, structureDetectRadius);
    }
}
