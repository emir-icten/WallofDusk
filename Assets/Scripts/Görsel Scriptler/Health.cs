using UnityEngine;

public class Health : MonoBehaviour, IPoolable
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
        currentHealth -= amount;

        if (currentHealth <= 0)
        {
            currentHealth = 0;
            Die();
        }
    }

    private void Die()
    {
        Debug.Log($"{name} öldü. isBase = {isBase}");

        // Base ölürse oyun biter
        if (isBase && FlowUI.Instance != null)
        {
            Debug.Log("FlowUI.OnGameOver() çağrıldı");
            FlowUI.Instance.OnGameOver();
            return;
        }

        // Enemy vb. objeler ölünce: pooled ise havuza dön, değilse destroy
        if (destroyOnDeath && !isBase)
        {
            if (PoolManager.Instance != null && GetComponent<PooledObject>() != null)
                PoolManager.Instance.Despawn(gameObject);
            else
                Destroy(gameObject);
        }
    }

    // Pool’dan tekrar çıkınca can reset
    public void OnSpawned()
    {
        currentHealth = maxHealth;
    }

    public void OnDespawned()
    {
        // İstersen burada VFX/Bar reset ekleyebilirsin.
    }
}
