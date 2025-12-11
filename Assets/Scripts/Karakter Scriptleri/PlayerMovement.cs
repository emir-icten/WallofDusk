using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Hareket Ayarları")]
    public float moveSpeed = 5f;
    public float rotationSpeed = 10f;

    [Header("Joystick (isteğe bağlı)")]
    public Joystick moveJoystick;          // Canvas'taki joystick'i buraya sürükle

    [Header("Referanslar")]
    public Rigidbody rb;                   // Root objenin Rigidbody'si

    private Transform cam;                 // Ana kamera
    private Animator animator;             // Karakterin Animator'u

    private Vector3 moveDir;               // Dünya uzayında hareket yönü
    private float baseY;                   // Karakterin sabit yükseklik değeri

    private void Awake()
    {
        if (rb == null)
            rb = GetComponent<Rigidbody>();

        if (Camera.main != null)
            cam = Camera.main.transform;

        animator = GetComponentInChildren<Animator>();

        // Başlangıçtaki yüksekliği kaydet (zemine göre bir kere ayarlanmış olsun)
        baseY = transform.position.y;

        // Fiziksel gömülmeyi önlemek için Rigidbody ayarlarını güvene al
        rb.useGravity = false; // Yerçekimi yok, yüksekliği biz kontrol edeceğiz
        rb.constraints = RigidbodyConstraints.FreezeRotationX |
                         RigidbodyConstraints.FreezeRotationZ;
        // Yüksekliği ayrıca kodda sabitlediğimiz için PositionY'yi burada dondurmak şart değil
        // ama istersen şu satırı da ekleyebilirsin:
        // rb.constraints |= RigidbodyConstraints.FreezePositionY;
    }

    private void Update()
    {
        HandleInput();
        HandleRotation();
        UpdateAnimator();
    }

    private void FixedUpdate()
    {
        if (rb == null) return;

        // Sadece XZ düzleminde hareket et
        Vector3 newPos = rb.position + moveDir * moveSpeed * Time.fixedDeltaTime;

        // Yüksekliği her frame sabitle: asla zemine gömülmesin
        newPos.y = baseY;

        rb.MovePosition(newPos);
    }

    /// <summary>
    /// Joystick + klavye girişlerinden hareket vektörünü hesaplar.
    /// </summary>
    private void HandleInput()
    {
        float inputX = 0f;
        float inputZ = 0f;

        // Joystick aktif mi? (mobil)
        bool joystickActive =
            moveJoystick != null &&
            (Mathf.Abs(moveJoystick.Horizontal) > 0.05f ||
             Mathf.Abs(moveJoystick.Vertical) > 0.05f);

        if (joystickActive)
        {
            inputX = moveJoystick.Horizontal;
            inputZ = moveJoystick.Vertical;
        }
        else
        {
            // PC testleri için WASD / ok tuşları
            inputX = Input.GetAxisRaw("Horizontal");
            inputZ = Input.GetAxisRaw("Vertical");
        }

        // Yerel giriş vektörü
        Vector3 inputDir = new Vector3(inputX, 0f, inputZ);
        inputDir = Vector3.ClampMagnitude(inputDir, 1f);

        // Kamera yönüne göre dünya uzayına çevir
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
    }

    /// <summary>
    /// Karakteri yürüdüğü yöne doğru döndürür.
    /// </summary>
    private void HandleRotation()
    {
        Vector3 flatDir = moveDir;
        flatDir.y = 0f;

        if (flatDir.sqrMagnitude > 0.001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(flatDir, Vector3.up);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRot,
                rotationSpeed * Time.deltaTime
            );
        }
    }

    /// <summary>
    /// Animator'daki "Speed" parametresini günceller (Idle / Run geçişleri için).
    /// </summary>
    private void UpdateAnimator()
    {
        if (animator == null) return;

        float speed = new Vector3(moveDir.x, 0f, moveDir.z).magnitude;
        animator.SetFloat("Speed", speed);
    }
}
