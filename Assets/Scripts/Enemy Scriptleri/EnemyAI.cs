using UnityEngine;

[RequireComponent(typeof(Health))]
public class EnemyAI : MonoBehaviour
{
    [Header("Ana Hedef (Base)")]
    public Transform baseTarget;              // Base binasının Transform'u

    [Header("Hareket Ayarları")]
    public float moveSpeed = 3f;
    public float stopDistance = 2f;           // Hedefe bu kadar yaklaştığında durur

    [Header("Saldırı Ayarları")]
    public int attackDamage = 5;
    public float attackInterval = 1.5f;       // Kaç saniyede bir vuracak
    public float attackRange = 2.5f;          // Bu mesafeden yakınsa vurabilir

    [Header("Yapı Hedefleme")]
    [Tooltip("Yakınındaki yapıları bu yarıçap içinde arar (Base, Tower, Building vs)")]
    public float structureDetectRadius = 6f;

    [Tooltip("Hedef alınabilecek bina tag'leri (Base, Building, Tower vs)")]
    public string[] structureTags = { "Base", "Building", "Tower" };

    [Header("Duvar Ayarları")]
    [Tooltip("Duvar segmentlerinin tag'i")]
    public string wallTag = "Wall";

    [Tooltip("Duvara tosladıktan sonra kaç saniye boyunca o duvarı hedefte tutsun")]
    public float wallTargetStickTime = 2f;

    [Header("Retarget")]
    [Tooltip("Kaç saniyede bir yeni hedef arayacak")]
    public float retargetInterval = 0.5f;

    private Transform currentTarget;
    private Health currentTargetHealth;
    private float attackTimer = 0f;

    private Health selfHealth;

    // hedef güncelleme için
    private float retargetTimer = 0f;

    // Çarpılan son duvar
    private Transform lastWallTarget;
    private float lastWallTime = -999f;

    private void Awake()
    {
        selfHealth = GetComponent<Health>();
    }

    private void Start()
    {
        // Başlangıçta base'i ana hedef yap
        if (baseTarget != null)
        {
            currentTarget = baseTarget;
            currentTargetHealth = baseTarget.GetComponent<Health>();
        }
    }

    private void Update()
    {
        // Düşman öldüyse hiçbir şey yapma
        if (selfHealth != null && selfHealth.currentHealth <= 0)
            return;

        // Hedef öldüyse / yok olduysa sıfırla
        if (currentTargetHealth == null || currentTargetHealth.currentHealth <= 0)
        {
            currentTarget = null;
            currentTargetHealth = null;
        }

        // Belirli aralıklarla hedef güncelle
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

        // 1) Hedefe doğru yürü
        if (dist > stopDistance)
        {
            Vector3 moveDir = toTarget.normalized;
            transform.position += moveDir * moveSpeed * Time.deltaTime;

            if (moveDir.sqrMagnitude > 0.001f)
            {
                Quaternion lookRot = Quaternion.LookRotation(moveDir);
                transform.rotation = Quaternion.Slerp(transform.rotation, lookRot, 10f * Time.deltaTime);
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
        else
        {
            // istersen menzil dışına çıkınca sıfırlayabilirsin
            // attackTimer = 0f;
        }
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
            // Yakında başka bina yoksa ana hedef base
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
        // Duvara çarptıysa ve duvar yaşıyorsa bu duvarı hedef al
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
