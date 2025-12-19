using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovementCC : MonoBehaviour
{
    [Header("Hareket Ayarları")]
    public float moveSpeed = 8f;
    public float rotationSpeed = 10f;

    [Header("Joystick (Fixed Joystick)")]
    public Joystick moveJoystick;

    [Tooltip("Joystick aktif sayılması için minimum eşik")]
    public float joystickDeadzone = 0.05f;

    [Header("Yerçekimi / Zemine Yapışma")]
    public float gravity = -25f;
    public float groundStick = -2f;

    [Header("Kontrol Kilidi")]
    public bool inputLocked = false;

    private CharacterController cc;
    private Transform cam;
    private Animator animator;

    private Vector3 moveDir;
    private float verticalVel;

    void Awake()
    {
        cc = GetComponent<CharacterController>();
        if (Camera.main != null) cam = Camera.main.transform;
        animator = GetComponentInChildren<Animator>();
    }

    void Update()
    {
        HandleInput();
        HandleRotation();
        ApplyGravityAndMove();
        UpdateAnimator();
    }

    // ---------------- INPUT ----------------

    void HandleInput()
    {
        if (inputLocked)
        {
            moveDir = Vector3.zero;
            return;
        }

        float inputX = 0f;
        float inputZ = 0f;

        // 1️⃣ Fixed Joystick varsa ve oynatılıyorsa
        if (moveJoystick != null &&
            (Mathf.Abs(moveJoystick.Horizontal) > joystickDeadzone ||
             Mathf.Abs(moveJoystick.Vertical) > joystickDeadzone))
        {
            inputX = moveJoystick.Horizontal;
            inputZ = moveJoystick.Vertical;
        }
        else
        {
            // 2️⃣ Klavye fallback (Editor / PC)
            inputX = Input.GetAxisRaw("Horizontal");
            inputZ = Input.GetAxisRaw("Vertical");
        }

        Vector3 inputDir = Vector3.ClampMagnitude(new Vector3(inputX, 0f, inputZ), 1f);

        if (cam != null)
        {
            Vector3 f = cam.forward; f.y = 0f; f.Normalize();
            Vector3 r = cam.right;   r.y = 0f; r.Normalize();
            moveDir = (f * inputDir.z + r * inputDir.x);
        }
        else
        {
            moveDir = inputDir;
        }
    }

    // ---------------- ROTATION ----------------

    void HandleRotation()
    {
        Vector3 flat = moveDir; 
        flat.y = 0f;

        if (flat.sqrMagnitude > 0.001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(flat, Vector3.up);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRot,
                rotationSpeed * Time.deltaTime
            );
        }
    }

    // ---------------- GRAVITY + MOVE ----------------

    void ApplyGravityAndMove()
    {
        if (cc.isGrounded && verticalVel < 0f)
            verticalVel = groundStick;

        verticalVel += gravity * Time.deltaTime;

        Vector3 velocity = moveDir * moveSpeed;
        velocity.y = verticalVel;

        cc.Move(velocity * Time.deltaTime);
    }

    // ---------------- ANIMATOR ----------------

    void UpdateAnimator()
    {
        if (animator == null) return;

        float speed = new Vector3(moveDir.x, 0f, moveDir.z).magnitude;
        animator.SetFloat("Speed", speed);
    }
}
