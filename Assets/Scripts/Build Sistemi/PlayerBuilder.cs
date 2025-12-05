using UnityEngine;

public class PlayerBuilder : MonoBehaviour
{
    [Header("Hareket Ayarları")]
    public float moveSpeed = 5f;
    public float stopDistance = 1.5f;   // İnşa alanına bu kadar yaklaşınca dur

    [Header("İnşa Ayarları")]
    public float buildRange = 2f;       // Şimdilik sadece mesafe kontrolü için

    private ConstructionSite targetSite;
    private bool isMovingToBuild = false;

    // Senin var olan hareket scriptin
    private PlayerMovement playerMovement;
    private Rigidbody rb;

    private void Awake()
    {
        // Aynı objede bulunan PlayerMovement ve Rigidbody'yi yakala
        playerMovement = GetComponent<PlayerMovement>();

        if (playerMovement != null && playerMovement.rb != null)
        {
            rb = playerMovement.rb;
        }
        else
        {
            rb = GetComponent<Rigidbody>();
        }
    }

    private void Update()
    {
        if (!isMovingToBuild || targetSite == null)
            return;

        Vector3 targetPos = targetSite.transform.position;
        targetPos.y = transform.position.y;

        float dist = Vector3.Distance(transform.position, targetPos);

        if (dist > stopDistance)
        {
            // PlayerMovement'i biz kontrol edeceğimiz için kapat
            if (playerMovement != null && playerMovement.enabled)
                playerMovement.enabled = false;

            // Hedefe doğru yürü
            Vector3 dir = (targetPos - transform.position).normalized;

            if (rb != null)
            {
                rb.MovePosition(rb.position + dir * moveSpeed * Time.deltaTime);
            }
            else
            {
                transform.position += dir * moveSpeed * Time.deltaTime;
            }

            // Karakter yüzünü gittiği yöne çevir
            if (dir != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(dir);
            }
        }
        else
        {
            // Hedefe vardık
            isMovingToBuild = false;

            // Normal kontrolü geri ver
            if (playerMovement != null)
                playerMovement.enabled = true;

            // İnşayı başlat
            targetSite.BeginConstruction(this);
            targetSite = null;
        }
    }

    /// <summary>
    /// BuildSystem burayı çağırıyor: "Git şu inşa alanını yap".
    /// </summary>
    public void GoBuild(ConstructionSite site)
    {
        if (site == null) return;

        targetSite = site;
        isMovingToBuild = true;
    }
}
