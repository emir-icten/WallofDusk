using UnityEngine;

public class EnemyAI : MonoBehaviour
{
    [Header("Ana Hedef (Base)")]
    public Transform baseTarget;          // Base binasının Transform'u

    [Header("Hareket Ayarları")]
    public float moveSpeed = 3f;
    public float stopDistance = 2f;       // Ne kadar yakında duracak

    [Header("Saldırı Ayarları")]
    public int attackDamage = 5;
    public float attackInterval = 1.5f;   // Kaç saniyede bir vuracak
    public float attackRange = 2.5f;      // Bu mesafeden yakınsa vurabilir

    [Header("Yapı Hedefleme")]
    public float structureDetectRadius = 4f;    // Yakınındaki yapıları bu yarıçap içinde arar
    [Tooltip("Hedef olarak sayılacak Tag'ler: örn. Base, Building")]
    public string[] structureTags = { "Base", "Building" };

    private Transform currentTarget;
    private Health currentTargetHealth;
    private float attackTimer = 0f;

    private void Start()
    {
        // Başlangıçta base'i ana hedef yap
        if (baseTarget != null)
        {
            currentTarget = baseTarget;
            currentTargetHealth = baseTarget.GetComponent<Health>();
        }
        else
        {
            Debug.LogWarning("EnemyAI: baseTarget atanmadı!", this);
        }
    }

    private void Update()
    {
        // Hedef seçimini güncelle (yakındaki yapı varsa ona yönel)
        UpdateTarget();

        if (currentTarget == null) return;

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
                // Debug.Log($"{name} -> {currentTarget.name} hasar verdi: {attackDamage}");
            }
        }
        else
        {
            // İstersen menzil dışına çıkınca saldırı sayacını sıfırlayabilirsin:
            // attackTimer = 0f;
        }
    }

    private void UpdateTarget()
    {
        Transform bestStructure = null;
        Health bestHealth = null;
        float bestDistSqr = Mathf.Infinity;

        // Yakındaki collider'lar içinde yapı arıyoruz
        Collider[] hits = Physics.OverlapSphere(transform.position, structureDetectRadius);
        foreach (var hit in hits)
        {
            // Sağlıklı bir Health component'i var mı?
            Health h = hit.GetComponent<Health>();
            if (h == null || h.currentHealth <= 0) continue;

            // Tag kontrolü: Base / Building vs.
            if (!HasValidStructureTag(hit.tag)) continue;

            float sqr = (hit.transform.position - transform.position).sqrMagnitude;
            if (sqr < bestDistSqr)
            {
                bestDistSqr = sqr;
                bestStructure = hit.transform;
                bestHealth = h;
            }
        }

        if (bestStructure != null)
        {
            // Yakında yapı buldu, onu hedefle
            currentTarget = bestStructure;
            currentTargetHealth = bestHealth;
        }
        else
        {
            // Yakında yapı yok, ana hedef base
            if (baseTarget != null)
            {
                currentTarget = baseTarget;
                currentTargetHealth = baseTarget.GetComponent<Health>();
            }
        }
    }

    private bool HasValidStructureTag(string tagToCheck)
    {
        for (int i = 0; i < structureTags.Length; i++)
        {
            if (tagToCheck == structureTags[i])
                return true;
        }
        return false;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, structureDetectRadius);
    }
}
