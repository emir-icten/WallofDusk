using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Hareket")]
    public float moveSpeed = 5f;
    public Rigidbody rb;

    [Header("Giriş Kaynağı")]
    public Joystick moveJoystick;   // 🎮 Mobil joystick (Canvas’taki joystiği buraya sürükle)

    private Transform cam;
    private Vector3 inputDir;
    private Vector3 moveDir;

    private void Awake()
    {
        if (rb == null)
            rb = GetComponent<Rigidbody>();

        if (Camera.main != null)
            cam = Camera.main.transform;
    }

    private void Update()
    {
        float moveX = 0f;
        float moveZ = 0f;

        // 1) Önce joystick’i dene (mobil)
        if (moveJoystick != null && 
            (Mathf.Abs(moveJoystick.Horizontal) > 0.01f || Mathf.Abs(moveJoystick.Vertical) > 0.01f))
        {
            moveX = moveJoystick.Horizontal;
            moveZ = moveJoystick.Vertical;
        }
        else
        {
            // 2) Joystick yoksa / kullanılmıyorsa klavye (PC test için)
            moveX = Input.GetAxisRaw("Horizontal");
            moveZ = Input.GetAxisRaw("Vertical");
        }

        inputDir = new Vector3(moveX, 0f, moveZ);
        inputDir = Vector3.ClampMagnitude(inputDir, 1f);

        // Kamera yönüne göre hareket (aynı eski mantığın)
        if (cam != null)
        {
            Vector3 camForward = cam.forward;
            camForward.y = 0f;
            camForward.Normalize();

            Vector3 camRight = cam.right;
            camRight.y = 0f;
            camRight.Normalize();

            moveDir = camForward * inputDir.z + camRight * inputDir.x;
        }
        else
        {
            moveDir = inputDir;
        }

        // Yürürken oyuncuyu gittiği yöne döndür
        if (moveDir.sqrMagnitude > 0.001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(moveDir, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, 10f * Time.deltaTime);
        }
    }

    private void FixedUpdate()
    {
        if (rb == null) return;

        rb.MovePosition(rb.position + moveDir * moveSpeed * Time.fixedDeltaTime);
    }
}
