using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovementCC : MonoBehaviour
{
    [Header("Hareket Ayarları")]
    public float moveSpeed = 8f;
    public float rotationSpeed = 10f;

    [Header("Joystick (isteğe bağlı)")]
    public Joystick moveJoystick;

    [Header("Yerçekimi / Zemine Yapışma")]
    public float gravity = -25f;
    public float groundStick = -2f;

    CharacterController cc;
    Transform cam;
    Animator animator;

    Vector3 moveDir;
    float verticalVel;

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

    void HandleInput()
    {
        float inputX, inputZ;

        bool joystickActive =
            moveJoystick != null &&
            (Mathf.Abs(moveJoystick.Horizontal) > 0.05f || Mathf.Abs(moveJoystick.Vertical) > 0.05f);

        if (joystickActive)
        {
            inputX = moveJoystick.Horizontal;
            inputZ = moveJoystick.Vertical;
        }
        else
        {
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
        else moveDir = inputDir;
    }

    void HandleRotation()
    {
        Vector3 flat = moveDir; flat.y = 0f;
        if (flat.sqrMagnitude > 0.001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(flat, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
        }
    }

    void ApplyGravityAndMove()
    {
        if (cc.isGrounded && verticalVel < 0f) verticalVel = groundStick;
        verticalVel += gravity * Time.deltaTime;

        Vector3 vel = moveDir * moveSpeed;
        vel.y = verticalVel;

        cc.Move(vel * Time.deltaTime);
    }

    void UpdateAnimator()
    {
        if (animator == null) return;
        animator.SetFloat("Speed", new Vector3(moveDir.x, 0f, moveDir.z).magnitude);
    }
}
