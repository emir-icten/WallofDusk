using UnityEngine;

public class LootProjectile : MonoBehaviour
{
    private Vector3 targetPosition; // Gideceði yer (Sol üstteki kutu)
    private int amountToAdd;        // Kaç puan taþýyor?
    private string type;            // Odun mu Taþ mý?
    private bool isMoving = false;

    // Hýz ayarý (Canvas üzerinde olduðu için yüksek hýz gerekir)
    public float moveSpeed = 2000f;

    public void Setup(Vector3 target, int amount, string resourceType)
    {
        targetPosition = target;
        amountToAdd = amount;
        type = resourceType;
        isMoving = true;
    }

    void Update()
    {
        if (!isMoving) return;

        // Hedefe doðru uç (Doðrusal Hareket)
        transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);

        // Hedefe vardýk mý? (Mesafe çok azaldýysa)
        if (Vector3.Distance(transform.position, targetPosition) < 50f)
        {
            DepositReward(); // Puaný kasaya koy
            Destroy(gameObject); // Kendini yok et
        }
    }

    void DepositReward()
    {
        // Kasa dolumu BURADA yapýlýyor (Vardýðý an)
        if (type == "Wood")
        {
            if (ResourceManager.instance != null) ResourceManager.AddWood(amountToAdd);
        }
        else if (type == "Stone")
        {
            if (ResourceManager.instance != null) ResourceManager.AddStone(amountToAdd);
        }
    }
}