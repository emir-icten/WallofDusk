using UnityEngine;

public class Health : MonoBehaviour, IPoolable
{
    [Header("Health")]
    public int maxHealth = 100;
    public int currentHealth;

    [Header("Death Settings")]
    public bool destroyOnDeath = true;
    public bool isBase = false;

    [Header("Hit Feedback")]
    public bool playHitAnimation = true;

    Animator animator;

    private void Awake()
    {
        currentHealth = maxHealth;
        animator = GetComponentInChildren<Animator>();
    }

    public void TakeDamage(int amount)
    {
        if (amount <= 0) return;

        currentHealth -= amount;

        // 🔹 DAMAGE POPUP
        if (DamagePopupSpawner.Instance != null)
            DamagePopupSpawner.Instance.Spawn(amount, transform.position);

        // 🔹 HIT ANIMATION
        if (playHitAnimation && animator != null)
            animator.SetTrigger("Hit");

        if (currentHealth <= 0)
        {
            currentHealth = 0;
            Die();
        }
    }

    private void Die()
    {
        // BASE ölürse oyun biter
        if (isBase && FlowUI.Instance != null)
        {
            FlowUI.Instance.OnGameOver();
            return;
        }

        // 🔹 ENEMY ÖLÜRSE COIN
        if (CompareTag("Enemy"))
        {
            ResourceManager.AddCoin(1);
        }

        if (!isBase && destroyOnDeath)
        {
            if (PoolManager.Instance != null && GetComponent<PooledObject>() != null)
                PoolManager.Instance.Despawn(gameObject);
            else
                Destroy(gameObject);
        }
    }

    public void OnSpawned()
    {
        currentHealth = maxHealth;
    }

    public void OnDespawned()
    {
    }
}
