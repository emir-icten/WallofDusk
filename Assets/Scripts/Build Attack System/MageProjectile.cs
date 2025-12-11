using UnityEngine;

public class MageProjectile : MonoBehaviour
{
    [Header("Hareket")]
    public float speed = 10f;
    public float lifeTime = 5f;

    [Header("Hasar & Alan")]
    public int damage = 10;
    public float explosionRadius = 3f;
    public string targetTag = "Enemy";
    public LayerMask enemyMask;

    [Header("Efektler")]
    public ParticleSystem impactEffect;   // Çarpma / patlama efekti

    private Vector3 moveDir;
    private bool hasExploded = false;

    // Beni atan kule
    private Transform owner;

    private void OnEnable()
    {
        // Yeni spawn olan her projectile tertemiz başlasın
        hasExploded = false;
    }

    /// <summary>
    /// Kule bu füzeyi hazırlarken çağırıyor.
    /// </summary>
    public void Init(
        Vector3 direction,
        int damageAmount,
        float radius,
        string targetTag,
        LayerMask enemyMask,
        Transform owner
    )
    {
        moveDir = direction.normalized;
        damage = damageAmount;
        explosionRadius = radius;
        this.targetTag = targetTag;
        this.enemyMask = enemyMask;
        this.owner = owner;
    }

    private void Start()
    {
        // Her ihtimale karşı belirli süreden sonra yok olsun
        Destroy(gameObject, lifeTime);
    }

    private void Update()
    {
        if (hasExploded) return;

        transform.position += moveDir * speed * Time.deltaTime;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasExploded) return;

        // 1) KENDİ KULESİNE ÇARPARSA YOK SAY
        if (owner != null &&
            (other.transform == owner || other.transform.IsChildOf(owner)))
        {
            return;
        }

        // 2) Şimdilik sadece hedef tag veya Ground olunca patlat
        if (!other.CompareTag(targetTag) && !other.CompareTag("Ground"))
            return;

        Explode();
    }

    private void Explode()
    {
        hasExploded = true;

        // Patlama efekti
        if (impactEffect != null)
        {
            ParticleSystem fx = Instantiate(impactEffect, transform.position, Quaternion.identity);
            fx.Play();
            Destroy(
                fx.gameObject,
                fx.main.duration + fx.main.startLifetime.constantMax
            );
        }

        // Alan hasarı
        Collider[] hits;
        if (enemyMask.value != 0)
            hits = Physics.OverlapSphere(transform.position, explosionRadius, enemyMask);
        else
            hits = Physics.OverlapSphere(transform.position, explosionRadius);

        foreach (var hit in hits)
        {
            if (!hit.CompareTag(targetTag)) continue;

            Health h = hit.GetComponent<Health>();
            if (h != null && h.currentHealth > 0)
            {
                h.TakeDamage(damage);
            }
        }

        Destroy(gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.6f, 0.3f, 1f, 0.4f);
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}
