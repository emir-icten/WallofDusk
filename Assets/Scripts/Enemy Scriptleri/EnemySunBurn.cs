using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Health))]
public class EnemySunBurn : MonoBehaviour
{
    [Header("Güneş Hasarı")]
    public int burnDamagePerTick = 2;      // Her tikte kaç HP gitsin
    public float tickInterval = 0.2f;      // Kaç saniyede bir vursun

    [Header("Davranış")]
    public bool disableAIWhileBurning = true;  // Yanarken saldırmasın mı?

    private Health health;
    private EnemyAI enemyAI;
    private bool isBurning = false;

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
        {
            enemyAI.enabled = false;  // Yanarken hareket/saldırı dursun
        }

        StartCoroutine(BurnRoutine());
    }

    private IEnumerator BurnRoutine()
    {
        // Canı bitene kadar 2 2 erit
        while (health != null && health.currentHealth > 0)
        {
            health.TakeDamage(burnDamagePerTick);
            yield return new WaitForSeconds(tickInterval);
        }

        // Canı 0'a düşünce düşmanı yok et
        if (gameObject != null)
        {
            Destroy(gameObject);
        }
    }
}
