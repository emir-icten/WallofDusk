using UnityEngine;

public class Health : MonoBehaviour
{
    [Header("Health Ayarları")]
    public int maxHealth = 100;
    public bool destroyOnDeath = false;   // Öldüğünde objeyi yok etsin mi?

    //[HideInInspector]
    public int currentHealth;

    private bool isDead = false;

    private void Awake()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(int amount)
    {
        if (isDead) return;
        if (amount <= 0) return;

        currentHealth -= amount;
        if (currentHealth <= 0)
        {
            currentHealth = 0;
            Die();
        }
    }

    public void Heal(int amount)
    {
        if (isDead) return;
        if (amount <= 0) return;

        currentHealth += amount;
        if (currentHealth > maxHealth)
            currentHealth = maxHealth;
    }

    private void Die()
    {
        isDead = true;

        // Burada isteğe göre ekstra şey yapabilirsin:
        // - Enemy ise exp / coin ver
        // - Base ise Game Over
        // - Player ise "ölme animasyonu" + yeniden doğma

        if (destroyOnDeath)
        {
            Destroy(gameObject);
        }
    }
}
