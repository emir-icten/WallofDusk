using UnityEngine;

public class LootProjectile : MonoBehaviour, IPoolable
{
    private Vector3 targetPosition; // Gideceği yer (Sol üstteki kutu)
    private int amountToAdd;        // Kaç puan taşıyor?
    private string type;            // Odun mu Taş mı?
    private bool isMoving = false;

    public float moveSpeed = 2000f;

    public void Setup(Vector3 target, int amount, string resourceType)
    {
        targetPosition = target;
        amountToAdd = amount;
        type = resourceType;
        isMoving = true;
    }

    public void OnSpawned()
    {
        // Setup çağrılınca hareket başlasın
        isMoving = false;
    }

    public void OnDespawned()
    {
        isMoving = false;
        amountToAdd = 0;
        type = null;
        targetPosition = Vector3.zero;
    }

    void Update()
    {
        if (!isMoving) return;

        transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);

        if (Vector3.Distance(transform.position, targetPosition) < 50f)
        {
            DepositReward();
            DespawnSelf();
        }
    }

    void DepositReward()
    {
        if (type == "Wood")
        {
            if (ResourceManager.instance != null) ResourceManager.AddWood(amountToAdd);
        }
        else if (type == "Stone")
        {
            if (ResourceManager.instance != null) ResourceManager.AddStone(amountToAdd);
        }
    }

    private void DespawnSelf()
    {
        if (PoolManager.Instance != null && GetComponent<PooledObject>() != null)
            PoolManager.Instance.Despawn(gameObject);
        else
            Destroy(gameObject);
    }
}
