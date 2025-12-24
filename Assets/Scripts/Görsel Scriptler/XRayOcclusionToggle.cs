using UnityEngine;

public class XRayOcclusionToggle : MonoBehaviour
{
    [Header("Hedef")]
    public Transform target;               // XRay gözükecek karakter/enemy
    public Renderer[] xrayRenderers;       // XRay mesh renderers (EnemyMesh_XRay içindeki renderer'lar)

    [Header("Occluder Mask")]
    public LayerMask occluderMask;         // Building/Base/Wall layerları

    [Header("Ray Ayarları")]
    public Vector3 targetOffset = new Vector3(0f, 1f, 0f);
    public float sphereRadius = 0.25f;

    [Header("Performans")]
    [Range(1, 6)] public int checkEveryNFrames = 2;

    int frame;

    private void Reset()
    {
        target = transform;
    }

    private void Awake()
    {
        if (target == null) target = transform;
        SetXRay(false); // başlangıçta kapalı
    }

    private void LateUpdate()
    {
        if (target == null || xrayRenderers == null || xrayRenderers.Length == 0)
            return;

        frame++;
        if (frame % checkEveryNFrames != 0)
            return;

        Camera cam = Camera.main;
        if (cam == null)
            return;

        Vector3 camPos = cam.transform.position;
        Vector3 aimPos = target.position + targetOffset;

        Vector3 dir = aimPos - camPos;
        float dist = dir.magnitude;
        if (dist < 0.01f)
        {
            SetXRay(false);
            return;
        }

        dir /= dist;

        // Kamera ile hedef arasına bina/base/duvar giriyor mu?
        bool blocked = Physics.SphereCast(
            camPos,
            sphereRadius,
            dir,
            out RaycastHit hit,
            dist,
            occluderMask,
            QueryTriggerInteraction.Ignore
        );

        SetXRay(blocked);
    }

    void SetXRay(bool on)
    {
        for (int i = 0; i < xrayRenderers.Length; i++)
        {
            if (xrayRenderers[i] != null)
                xrayRenderers[i].enabled = on;
        }
    }
}
