using UnityEngine;

public class PlayerMageProjectile : MonoBehaviour
{
    [Header("Projectile")]
    public float speed = 18f;
    public int baseDamage = 35;
    public float radius = 2.5f;
    [Range(0.1f, 1f)] public float edgeMultiplier = 0.5f;

    [Header("Targeting")]
    public LayerMask enemyMask;
    public string enemyTag = "Enemy";

    Transform owner;
    float life = 4f;
    bool exploded;

    /// <summary>
    /// Projectile spawn edilirken PlayerMage tarafından çağrılır
    /// </summary>
    public void Init(
        Transform owner,
        float speed,
        int baseDamage,
        float radius,
        LayerMask enemyMask,
        string enemyTag,
        float edgeMultiplier)
    {
        this.owner = owner;
        this.speed = speed;
        this.baseDamage = baseDamage;
        this.radius = radius;
        this.enemyMask = enemyMask;
        this.enemyTag = enemyTag;
        this.edgeMultiplier = Mathf.Clamp(edgeMultiplier, 0.1f, 1f);

        // 🔥 EN ÖNEMLİ KISIM: owner ile çarpışmayı tamamen kapat
        Collider projCol = GetComponent<Collider>();
        if (projCol != null && owner != null)
        {
            Collider[] ownerCols = owner.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < ownerCols.Length; i++)
            {
                if (ownerCols[i] != null)
                    Physics.IgnoreCollision(projCol, ownerCols[i], true);
            }
        }
    }

    void Update()
    {
        if (exploded) return;

        transform.position += transform.forward * speed * Time.deltaTime;

        life -= Time.deltaTime;
        if (life <= 0f)
            Explode();


Collider[] hits = Physics.OverlapSphere(transform.position, 0.3f, enemyMask, QueryTriggerInteraction.Ignore);
if (hits.Length > 0) Explode();
    
    
    
    
}

    void OnTriggerEnter(Collider other)
    {
        if (exploded) return;

        // Owner ile çarpışmayı zaten kapattık ama ekstra güvenlik
        if (owner != null && (other.transform == owner || other.transform.IsChildOf(owner)))
            return;

        // Düşmana veya zemine çarpınca patla
        if (other.CompareTag(enemyTag) || other.CompareTag("Ground"))
            Explode();
    }

    void Explode()
    {
        if (exploded) return;
        exploded = true;

        Collider[] hits = Physics.OverlapSphere(
            transform.position,
            radius,
            enemyMask,
            QueryTriggerInteraction.Ignore
        );

        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i] == null) continue;
            if (!hits[i].CompareTag(enemyTag)) continue;

            float dist = Vector3.Distance(transform.position, hits[i].transform.position);
            float t = Mathf.Clamp01(dist / radius);

            float mult = Mathf.Lerp(1f, edgeMultiplier, t);
            int finalDamage = Mathf.RoundToInt(baseDamage * mult);

            // Health root objede olabilir
            Health h = hits[i].GetComponentInParent<Health>();
            if (h != null)
                h.TakeDamage(finalDamage);
        }

        Destroy(gameObject);
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
#endif
}
