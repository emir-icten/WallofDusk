using UnityEngine;

[RequireComponent(typeof(Health))]
public class PlayerContactDamageReceiver : MonoBehaviour
{
    [Header("Temas Hasarı")]
    public int damage = 5;
    public float interval = 1.0f;

    [Header("Algılama")]
    [Tooltip("Enemy layer'ını seç. (LayerMask)")]
    public LayerMask enemyMask;

    [Tooltip("Karakterin etrafında bu yarıçapta enemy varsa temas sayılır.")]
    public float checkRadius = 1.0f;

    [Tooltip("Yarıçapın merkez offseti (bel hizası için y=1 önerilir).")]
    public Vector3 centerOffset = new Vector3(0f, 1f, 0f);

    [Header("Opsiyonel Filtre")]
    [Tooltip("Enemy'lerin tag'i. Boş bırakırsan tag kontrolü yapmaz.")]
    public string enemyTag = "Enemy";

    float timer;
    Health myHealth;

    private void Awake()
    {
        myHealth = GetComponent<Health>();

        // EnemyMask boşsa otomatik Enemy layer'ını dene
        if (enemyMask.value == 0)
            enemyMask = LayerMask.GetMask("Enemy");
    }

    private void Update()
    {
        if (myHealth == null) return;

        timer += Time.deltaTime;
        if (timer < interval) return;

        Vector3 center = transform.position + centerOffset;

        // Enemy var mı?
        Collider[] hits = Physics.OverlapSphere(center, checkRadius, enemyMask, QueryTriggerInteraction.Ignore);

        if (hits == null || hits.Length == 0) return;

        // Tag filtresi isteniyorsa kontrol et
        if (!string.IsNullOrEmpty(enemyTag))
        {
            bool foundTaggedEnemy = false;
            for (int i = 0; i < hits.Length; i++)
            {
                if (hits[i] != null && hits[i].CompareTag(enemyTag))
                {
                    foundTaggedEnemy = true;
                    break;
                }
            }

            if (!foundTaggedEnemy) return;
        }

        // Temas var: hasarı uygula ve timer sıfırla
        timer = 0f;
        myHealth.TakeDamage(damage);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Vector3 center = transform.position + centerOffset;
        Gizmos.DrawWireSphere(center, checkRadius);
    }
#endif
}
