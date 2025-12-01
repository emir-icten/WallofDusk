using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;

    [Header("Takip Ayarlarý")]
    public float smoothTime = 0.2f;

    [Header("Ölü Bölge (Dead Zone)")]
    public float deadZoneRadius = 2f; // Çemberi biraz büyüttüm daha net hisset diye

    [Header("Görüþ Ayarlarý")]
    public float lookAheadDst = 3f;
    public float lookSmooth = 3f;
    public float downLookMultiplier = 2f;

    private Vector3 offset;
    private Vector3 currentVelocity;
    private Vector3 currentLookAhead;
    private Vector3 focusPosition;

    void Start()
    {
        if (target != null)
        {
            offset = transform.position - target.position;
            focusPosition = target.position;
        }
    }

    void LateUpdate()
    {
        if (target != null)
        {
            // --- 1. ÖLÜ BÖLGE HESABI ---
            float distance = Vector3.Distance(target.position, focusPosition);

            // Karakter çemberin DIÞINA çýktý mý?
            bool isOutsideDeadZone = distance > deadZoneRadius;

            if (isOutsideDeadZone)
            {
                // Çýktýysa odaðý kaydýr (Takip et)
                Vector3 direction = (target.position - focusPosition).normalized;
                focusPosition = target.position - (direction * deadZoneRadius);
            }

            // --- 2. ÝLERÝ GÖRÜÞ (DÜZELTÝLEN KISIM) ---

            Vector3 targetLookAhead = Vector3.zero;

            // DÝKKAT: Artýk Ýleri Görüþ, sadece çemberin DIÞINDAYSAK çalýþacak.
            // Ýçindeyken 'targetLookAhead' sýfýr kalýr, yani kamera ekstra oynamaz.
            if (isOutsideDeadZone)
            {
                float x = Input.GetAxisRaw("Horizontal");
                float z = Input.GetAxisRaw("Vertical");
                bool isMoving = x != 0 || z != 0;

                if (isMoving)
                {
                    float currentDst = lookAheadDst;
                    if (z < -0.1f)
                    {
                        currentDst = lookAheadDst * downLookMultiplier;
                    }
                    targetLookAhead = target.forward * currentDst;
                }
            }

            // Bakýþ açýsýný yumuþat
            currentLookAhead = Vector3.Lerp(currentLookAhead, targetLookAhead, lookSmooth * Time.deltaTime);

            // --- 3. HEDEF VE HAREKET ---
            Vector3 targetPosition = focusPosition + offset + currentLookAhead;
            transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref currentVelocity, smoothTime);
        }
    }

    void OnDrawGizmos()
    {
        if (target != null)
        {
            Gizmos.color = new Color(1, 0, 0, 0.3f);
            Vector3 center = Application.isPlaying ? focusPosition : target.position;
            Gizmos.DrawSphere(center, deadZoneRadius);
        }
    }
}