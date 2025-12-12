using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerBuilder : MonoBehaviour
{
    [Header("Hareket Ayarları")]
    public float moveSpeed = 5f;
    public float stopDistance = 1.5f;

    [Header("İnşa Ayarları")]
    public float buildRange = 2f; // şimdilik sadece mesafe kontrolü

    private ConstructionSite targetSite;
    private bool isMovingToBuild;

    private PlayerMovementCC playerMovement;     // senin yeni movement scriptin
    private CharacterController cc;
    private Animator animator;

    void Awake()
    {
        cc = GetComponent<CharacterController>();
        playerMovement = GetComponent<PlayerMovementCC>();   // ❗ eski PlayerMovement değil
        animator = GetComponentInChildren<Animator>();
    }

    void Update()
    {
        if (!isMovingToBuild || targetSite == null) return;

        // Y düzlemini sabitle (top-down gibi)
        Vector3 targetPos = targetSite.transform.position;
        targetPos.y = transform.position.y;

        float dist = Vector3.Distance(transform.position, targetPos);

        if (dist > stopDistance)
        {
            // Normal kontrol kapansın
            if (playerMovement != null && playerMovement.enabled)
                playerMovement.enabled = false;

            Vector3 dir = (targetPos - transform.position);
            dir.y = 0f;

            if (dir.sqrMagnitude > 0.0001f)
            {
                dir.Normalize();

                // CharacterController ile güvenli hareket
                cc.Move(dir * moveSpeed * Time.deltaTime);

                // Yöne dön
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    Quaternion.LookRotation(dir),
                    12f * Time.deltaTime
                );

                // Anim (opsiyonel)
                if (animator != null)
                    animator.SetFloat("Speed", 1f);
            }
        }
        else
        {
            // Vardık
            isMovingToBuild = false;

            // Anim (opsiyonel)
            if (animator != null)
                animator.SetFloat("Speed", 0f);

            // Kontrolü geri ver
            if (playerMovement != null)
                playerMovement.enabled = true;

            // İnşayı başlat
            targetSite.BeginConstruction();

            targetSite = null;
        }
    }

    public void GoBuild(ConstructionSite site)
    {
        if (site == null) return;
        targetSite = site;
        isMovingToBuild = true;
    }
}
