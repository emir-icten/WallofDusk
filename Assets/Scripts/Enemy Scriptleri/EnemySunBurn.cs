using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Health))]
public class EnemySunBurn : MonoBehaviour
{
    [Header("Güneş Hasarı")]
    public int burnDamagePerTick = 2;
    public float tickInterval = 0.2f;

    [Header("Davranış")]
    public bool disableAIWhileBurning = true;

    private Health health;
    private EnemyAI enemyAI;
    private bool isBurning = false;
    private Coroutine burnCo;

    private void Awake()
    {
        health = GetComponent<Health>();
        enemyAI = GetComponent<EnemyAI>();
    }

    public void StartBurning()
    {
        if (isBurning) return;
        isBurning = true;

        if (disableAIWhileBurning && enemyAI != null)
            enemyAI.enabled = false;

        if (burnCo != null) StopCoroutine(burnCo);
        burnCo = StartCoroutine(BurnRoutine());
    }

    private IEnumerator BurnRoutine()
    {
        while (health != null && health.currentHealth > 0)
        {
            health.TakeDamage(burnDamagePerTick);
            yield return new WaitForSeconds(tickInterval);
        }

        // Burada Destroy/Despawn yapmıyoruz.
        // Health.TakeDamage -> Die() zaten pooled ise Despawn ediyor.
        burnCo = null;
        isBurning = false;
    }
}
