using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public float moveSpeed = 5f; // Karakter hızı
    public Rigidbody rb; // Fizik motoru

    Vector3 movement;
    Vector3 camForward;
    Vector3 camRight;

    void Update()
    {
        // Klavyeden verileri al (WASD)
        float moveX = Input.GetAxisRaw("Horizontal");
        float moveZ = Input.GetAxisRaw("Vertical");

        // Hareket verisini bir vektörde tut
        movement = new Vector3(moveX, 0f, moveZ);
    }

    void FixedUpdate()
    {
        // Kameranın olduğu yerde olup olmadığını kontrol et (Hata almamak için)
        if (Camera.main != null)
        {
            // 1. Kameranın baktığı yönleri al
            camForward = Camera.main.transform.forward;
            camRight = Camera.main.transform.right;

            // 2. Kameranın yere eğimini (Y eksenini) sıfırla. 
            // Böylece karakter havaya uçmaya çalışmaz, yerde kalır.
            camForward.y = 0;
            camRight.y = 0;

            // 3. Vektörleri düzelt (Normalize et)
            camForward.Normalize();
            camRight.Normalize();

            // 4. Gideceğimiz asıl yönü hesapla:
            // (Kameranın ilerisi * W tuşu) + (Kameranın sağı * D tuşu)
            Vector3 desiredMoveDirection = (camForward * movement.z) + (camRight * movement.x);

            // 5. Karakteri hareket ettir
            rb.MovePosition(rb.position + desiredMoveDirection * moveSpeed * Time.fixedDeltaTime);

            // 6. Karakterin yüzünü gittiği yere döndür (Opsiyonel ama şık durur)
            if (desiredMoveDirection != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(desiredMoveDirection);
            }
        }
    }
}