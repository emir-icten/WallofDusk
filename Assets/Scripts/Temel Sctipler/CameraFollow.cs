using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Takip Edilecek Hedef")]
    public Transform target;          // Takip edeceğimiz player

    [Header("Konum Ayarları")]
    public Vector3 offset = new Vector3(0f, 15f, -15f); // Hedefe göre ofset
    public float smoothTime = 0.2f;                    // Yumuşatma süresi

    [Header("Dead Zone Ayarları")]
    [Tooltip("Player bu yarıçapın içinde kaldıkça kamera hareket etmez.")]
    public float deadZoneRadius = 2f;

    private Vector3 focusPosition;    // Kameranın "takip ettiği merkez"
    private Vector3 currentVelocity;  // SmoothDamp için

    /// <summary>
    /// Oyun içinde istediğimiz zaman hedefi değiştirmek için kullanacağız.
    /// (Karakter seçildiğinde PlayerSelectionManager burayı çağıracak.)
    /// </summary>
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;

        if (target != null)
        {
            // Hedef değiştiğinde kameranın odağını direkt oraya taşı
            focusPosition = target.position;
        }
    }

    private void Start()
    {
        if (target != null)
        {
            focusPosition = target.position;
            transform.position = focusPosition + offset;
        }
    }

    private void LateUpdate()
    {
        if (target == null)
            return;

        Vector3 targetPos = target.position;

        // Hedef ile odak arasındaki fark
        Vector3 delta = targetPos - focusPosition;

        // Dead zone'u XZ (yatay) düzleminde hesaplayalım
        Vector3 deltaHorizontal = new Vector3(delta.x, 0f, delta.z);
        float dist = deltaHorizontal.magnitude;

        // Player dead zone'dan çıkarsa, focusPosition'ı onu takip ettirelim
        if (dist > deadZoneRadius)
        {
            float moveAmount = dist - deadZoneRadius;
            Vector3 moveDir = deltaHorizontal.normalized;
            focusPosition += moveDir * moveAmount;
        }

        // Y eksenini de yavaşça hedefe yaklaştıralım (istersen sabit de bırakabilirsin)
        focusPosition.y = Mathf.Lerp(focusPosition.y, targetPos.y, Time.deltaTime * 5f);

        // Kameranın gitmesi gereken hedef pozisyon
        Vector3 desiredPos = focusPosition + offset;

        // SmoothDamp ile yumuşak takip
        transform.position = Vector3.SmoothDamp(
            transform.position,
            desiredPos,
            ref currentVelocity,
            smoothTime
        );
    }

    private void OnDrawGizmosSelected()
    {
        if (target == null)
            return;

        Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
        Gizmos.DrawWireSphere(
            Application.isPlaying ? focusPosition : target.position,
            deadZoneRadius
        );
    }
}
