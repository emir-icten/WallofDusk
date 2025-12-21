using UnityEngine;

public class MageProjectileDebug : MonoBehaviour
{
    [Header("Debug")]
    public bool logInit = true;
    public bool logEveryFrame = false;
    public bool logTrigger = true;
    public bool drawForwardRay = true;

    Rigidbody rb;
    Collider col;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();
    }

    void Start()
    {
        if (!logInit) return;

        Debug.Log($"[MageProjDebug] START name={name} pos={transform.position} rot={transform.rotation.eulerAngles} " +
                  $"rb={(rb ? "YES" : "NO")} kinematic={(rb ? rb.isKinematic.ToString() : "-")} gravity={(rb ? rb.useGravity.ToString() : "-")} " +
                  $"col={(col ? "YES" : "NO")} isTrigger={(col ? col.isTrigger.ToString() : "-")}");
    }

    void Update()
    {
        if (drawForwardRay)
            Debug.DrawRay(transform.position, transform.forward * 1.5f, Color.magenta);

        if (!logEveryFrame) return;

        Debug.Log($"[MageProjDebug] UPDATE pos={transform.position} forward={transform.forward}");
    }
void OnEnable()
{
    Debug.Log($"[MageProjDebug] ENABLE name={name}");
}

void OnDisable()
{
    Debug.Log($"[MageProjDebug] DISABLE name={name}");
}

void OnDestroy()
{
    Debug.Log($"[MageProjDebug] DESTROY name={name}");
}

    void OnTriggerEnter(Collider other)
    {
        if (!logTrigger) return;

        Debug.Log($"[MageProjDebug] TRIGGER -> {other.name} tag={other.tag} layer={LayerMask.LayerToName(other.gameObject.layer)}");

        if (other.CompareTag("Enemy"))
        {
            Health health = other.GetComponent<Health>();
            if (health != null)
            {
                health.TakeDamage(10); // Hasar miktarını burada değiştirebilirsin
            }
        }

        Destroy(gameObject); // Mermi çarptıktan sonra yok olsun
    }

    void OnCollisionEnter(Collision collision)
    {
        if (!logTrigger) return;
        Debug.Log($"[MageProjDebug] COLLISION -> {collision.collider.name} tag={collision.collider.tag} layer={LayerMask.LayerToName(collision.collider.gameObject.layer)}");
    }
}
