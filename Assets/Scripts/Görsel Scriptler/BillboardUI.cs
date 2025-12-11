using UnityEngine;

public class BillboardUI : MonoBehaviour
{
    private Camera cam;

    private void LateUpdate()
    {
        if (cam == null)
            cam = Camera.main;
        if (cam == null) return;

        // Kameranın baktığı yönü al
        Vector3 camForward = cam.transform.forward;

        // Y ekseninde dik kalsın, öne/arkaya eğilmesin
        camForward.y = 0f;

        if (camForward.sqrMagnitude < 0.0001f)
            return;

        // Tüm healthbarlar kameraya paralel, dik duracak şekilde döner
        transform.rotation = Quaternion.LookRotation(camForward, Vector3.up);
    }
}
