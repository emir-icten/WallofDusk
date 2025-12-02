using UnityEngine;

public class BillboardUI : MonoBehaviour
{
    private Camera cam;

    private void LateUpdate()
    {
        if (cam == null)
            cam = Camera.main;
        if (cam == null) return;

        // Kameraya bakacak şekilde döndür
        Vector3 dir = transform.position - cam.transform.position;
        dir.y = 0f;  // Dilersen dik kalması için Y'yi sabitlersin, istemezsen bu satırı sil

        if (dir.sqrMagnitude > 0.0001f)
        {
            transform.rotation = Quaternion.LookRotation(dir);
        }
    }
}
