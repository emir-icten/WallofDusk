using UnityEngine;

public class TowerArrow : MonoBehaviour
{
    [Header("Hareket")]
    public float speed = 12f;
    public float lifeTime = 4f;

    [Header("Saldırı")]
    public int damage = 10;
    public string targetTag = "Enemy";

    private void Start()
    {
        // Belirli süre sonra kaybolsun
        Destroy(gameObject, lifeTime);
    }

    private void Update()
    {
        // Hep ileri doğru hareket et
        transform.position += transform.forward * speed * Time.deltaTime;
    }

    private void OnTriggerEnter(Collider other)
    {
        // Sadece hedef tag'e sahip objeleri vur
        if (!other.CompareTag(targetTag)) return;

        Health h = other.GetComponent<Health>();
        if (h != null && h.currentHealth > 0)
        {
            h.TakeDamage(damage);
        }

        Destroy(gameObject);
    }
}
