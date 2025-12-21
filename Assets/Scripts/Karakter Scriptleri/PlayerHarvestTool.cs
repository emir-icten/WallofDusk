using UnityEngine;

public class PlayerHarvestTool : MonoBehaviour
{
    [Header("Socket Altındaki Objeler")]
    public GameObject bow;      // Sol elde olabilir, fark etmez
    public GameObject pickaxe;  // Sağ elde
    public GameObject axe;      // Sağ elde

    [Header("Refs")]
    public Animator animator;           // Character üzerindeki Animator
    public PlayerMovementCC movement;   // inputLocked olan

    [Header("Animator Parametreleri (isimler birebir aynı olmalı)")]
    public string harvestingBool = "Harvesting";
    public string harvestHitTrigger = "HarvestHit";
    public string harvestToolInt = "HarvestTool"; // Wood=1, Stone=2

    [Header("Ayarlar")]
    public bool lockMovementWhileHarvesting = true;

    [Tooltip("Harvest sırasında hedefe otomatik dönsün mü? Dönme sorunu yaşarsan kapatabilirsin.")]
    public bool rotateToTargetWhileHarvesting = true;

    [Tooltip("Hedefe dönerken dönüş hızı (deg/s). Çok yüksek olursa jitter/spin hissi verebilir.")]
    public float faceSpeedDeg = 540f;

    [Tooltip("Hedef çok yakınsa dönmeyi durdur (spin önler).")]
    public float minTurnDistance = 0.35f;

    bool harvesting;

    // Transform yerine sabit world noktası: child collider/mesh değişse bile hedef sabit kalır
    Vector3 lookPointWorld;
    bool hasLookPoint;

    public bool IsHarvesting => harvesting;

    void Awake()
    {
        if (!animator) animator = GetComponentInChildren<Animator>();
        if (!movement) movement = GetComponent<PlayerMovementCC>();
        EquipBow();
    }

    void Update()
    {
        if (!harvesting) return;
        if (!rotateToTargetWhileHarvesting) return;
        if (!hasLookPoint) return;

        Vector3 dir = lookPointWorld - transform.position;
        dir.y = 0f;

        // Hedef aşırı yakınsa dönme (spin önler)
        if (dir.magnitude < minTurnDistance) return;
        if (dir.sqrMagnitude < 0.0001f) return;

        Quaternion desired = Quaternion.LookRotation(dir.normalized, Vector3.up);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, desired, faceSpeedDeg * Time.deltaTime);
    }

    public void OnHarvestStart(ResourceType type, Transform target)
    {
        harvesting = true;

        // Hedefi sabitle: Transform yerine world point
        hasLookPoint = false;
        if (target != null)
        {
            // Eğer collider varsa merkezini al (daha stabil)
            Collider c = target.GetComponentInChildren<Collider>();
            if (c != null)
                lookPointWorld = c.bounds.center;
            else
                lookPointWorld = target.position;

            hasLookPoint = true;
        }

        if (movement)
            movement.inputLocked = lockMovementWhileHarvesting;

        if (animator)
        {
            animator.SetBool(harvestingBool, true);

            // Wood=1, Stone=2
            if (!string.IsNullOrEmpty(harvestToolInt))
                animator.SetInteger(harvestToolInt, type == ResourceType.Stone ? 2 : 1);
        }

        if (type == ResourceType.Stone) EquipPickaxe();
        else EquipAxe();
    }

    // ResourceNode her loot verdiğinde çağıracak: Trigger ile tek vuruş
    public void OnHarvestTick()
    {
        if (!harvesting) return;
        if (!animator) return;

        animator.ResetTrigger(harvestHitTrigger);
        animator.SetTrigger(harvestHitTrigger);
    }

    public void OnHarvestStop()
    {
        harvesting = false;
        hasLookPoint = false;

        if (movement)
            movement.inputLocked = false;

        if (animator)
            animator.SetBool(harvestingBool, false);

        EquipBow();
    }

    void EquipBow()
    {
        if (bow) bow.SetActive(true);
        if (pickaxe) pickaxe.SetActive(false);
        if (axe) axe.SetActive(false);
    }

    void EquipPickaxe()
    {
        if (bow) bow.SetActive(false);
        if (pickaxe) pickaxe.SetActive(true);
        if (axe) axe.SetActive(false);
    }

    void EquipAxe()
    {
        if (bow) bow.SetActive(false);
        if (pickaxe) pickaxe.SetActive(false);
        if (axe) axe.SetActive(true);
    }
}
