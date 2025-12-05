using UnityEngine;

public class Health : MonoBehaviour
{
    [Header("Can Ayarları")]
    public int maxHealth = 100;
    [HideInInspector] public int currentHealth;
    public bool destroyOnDeath = true;

    [Header("Özel Bayraklar")]
    [Tooltip("Bu obje ölürse Game Over olsun (Base vs.)")]
    public bool isBase = false;

    private void Awake()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(int amount)
    {
        if (currentHealth <= 0) return;

        currentHealth -= amount;
        if (currentHealth <= 0)
        {
            currentHealth = 0;
            Die();
        }
    }

    public void Heal(int amount)
    {
        if (currentHealth <= 0) return;
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
    }

    private void Die()
    {
        Debug.Log($"{name} öldü. isBase = {isBase}");

        if (isBase && FlowUI.Instance != null)
        {
            Debug.Log("FlowUI.OnGameOver() çağrıldı");
            FlowUI.Instance.OnGameOver();
        }

        if (destroyOnDeath && !isBase)
        {
            Destroy(gameObject);
        }
    }
}
