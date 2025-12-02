using UnityEngine;

public class ArrowProjectile : MonoBehaviour
{
    [Header("Ok Ayarları")]
    public float speed = 20f;
    public int damage = 10;
    public float lifeTime = 5f;

    [Tooltip("Hedefe ne kadar yaklaştığında 'vurmuş' sayalım?")]
    public float hitRadius = 0.7f;

    [Tooltip("Ok spawn olduktan sonra çarpmayı ne kadar geciktirelim? (kendine çarpmaması için)")]
    public float collisionDelay = 0.05f;

    [HideInInspector]
    public Transform target;   // PlayerArcher burayı set ediyor

    float age = 0f;
    bool canCollide => age >= collisionDelay;

    private void Start()
    {
        // Belirli süre sonra otomatik yok olsun
        Destroy(gameObject, lifeTime);
    }

    private void Update()
    {
        age += Time.deltaTime;

        if (target != null)
        {
            // Hedefe doğru homing
            Vector3 toTarget = target.position - transform.position;
            float dist = toTarget.magnitude;

            // Yeterince yaklaştıysak vur
            if (dist <= hitRadius)
            {
                HitTarget(target);
                return;
            }

            Vector3 dir = toTarget.normalized;
            transform.position += dir * speed * Time.deltaTime;

            if (dir.sqrMagnitude > 0.0001f)
            {
                transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
            }
        }
        else
        {
            // Hedef yoksa düz ilerle
            transform.position += transform.forward * speed * Time.deltaTime;
        }
    }

    private void HitTarget(Transform other)
    {
        Health h = other.GetComponent<Health>();
        if (h != null)
        {
            h.TakeDamage(damage);
        }

        Destroy(gameObject);
    }

    private void OnTriggerEnter(Collider other)
    {
        // İlk birkaç milisaniyede çarpışmayı görmezden gel
        if (!canCollide) return;

        // Enemy'e çarparsa garanti vur
        if (other.CompareTag("Enemy"))
        {
            HitTarget(other.transform);
            return;
        }

        // Player'a, okçuya çarpıyorsa yok say
        if (other.CompareTag("Player"))
            return;

        // Onun dışındaki Tüm solid objeler (duvar, bina, zemin vs.) oku durdursun
        if (!other.isTrigger)
        {
            Destroy(gameObject);
        }
    }
}
