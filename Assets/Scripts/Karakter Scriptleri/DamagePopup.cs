using TMPro;
using UnityEngine;

public class DamagePopup : MonoBehaviour
{
    [Header("Ayarlar")]
    public float floatSpeed = 1.5f;
    public float lifetime = 0.8f;

    TextMeshPro text;
    float timer;
    Transform cam;

    private void Awake()
    {
        // TextMeshPro otomatik bul
        text = GetComponent<TextMeshPro>();
        if (text == null)
            text = GetComponentInChildren<TextMeshPro>();

        // Kamera referansı
        if (Camera.main != null)
            cam = Camera.main.transform;
    }

    public void Setup(int damage)
    {
        if (text != null)
            text.text = "-" + damage + " HP";

        timer = 0f;
    }

    private void Update()
    {
        // Yukarı doğru süzül
        transform.position += Vector3.up * floatSpeed * Time.deltaTime;

        // Kameraya bak (billboard)
        if (cam != null)
        {
            Vector3 lookDir = transform.position - cam.position;
            lookDir.y = 0f;
            transform.rotation = Quaternion.LookRotation(lookDir);
        }

        timer += Time.deltaTime;
        if (timer >= lifetime)
            Destroy(gameObject);
    }
}
