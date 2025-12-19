using UnityEngine;

public class PlayerHarvestTool : MonoBehaviour
{
    [Header("Socket Altındaki Objeler")]
    public GameObject bow;      // Sol elde olabilir, fark etmez
    public GameObject pickaxe;  // Sağ elde
    public GameObject axe;      // Sağ elde

    [Header("Refs")]
    public Animator animator;           // Graphics_Ranger üzerindeki Animator
    public PlayerMovementCC movement;   // PlayerMovementCC (inputLocked olan)

    [Header("Animator Parametreleri (isimler birebir aynı olmalı)")]
    public string harvestingBool = "Harvesting";
    public string harvestHitTrigger = "HarvestHit";
    public string harvestToolInt = "HarvestTool"; // Wood=1, Stone=2

    [Header("Ayarlar")]
    public bool lockMovementWhileHarvesting = true;
    public float faceSpeedDeg = 1080f;          // hedefe hızlı dönme

    bool harvesting;
    Transform lookTarget;

    void Awake()
    {
        if (!animator) animator = GetComponentInChildren<Animator>();
        if (!movement) movement = GetComponent<PlayerMovementCC>();
        EquipBow();
    }

    void Update()
    {
        // Harvest sırasında kaynağa doğru dön
        if (!harvesting || lookTarget == null) return;

        Vector3 dir = lookTarget.position - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) return;

        Quaternion desired = Quaternion.LookRotation(dir);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, desired, faceSpeedDeg * Time.deltaTime);
    }

    public void OnHarvestStart(ResourceType type, Transform target)
    {
        harvesting = true;
        lookTarget = target;

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
        lookTarget = null;

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
