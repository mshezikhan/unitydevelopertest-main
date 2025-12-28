using UnityEngine;

public class PlayerController : MonoBehaviour
{
    // References
    private Rigidbody _rb;
    private Animator _animator;
    private PlayerActions _actions;

    [Header("Player Config")]
    [SerializeField] float moveSpeed = 5f;
    [SerializeField] float jumpForce = 6f;
    [SerializeField] float customGravityStrength = 20f;
    [SerializeField] float rotationSpeed = 10f;

    [Header("Hologram Config")]
    [SerializeField] Transform hologramPivot;
    [SerializeField] float rotateAngle = 90f;

    // Input / State
    private Vector2 moveInput;
    private Vector2 rotateInput;
    private Vector2 lastRotateInput;

    private bool jumpPressed;
    private bool gravitySwitchPressed;
    private bool isGrounded;
    private float gravitySwitchCooldown = 0.2f;
    private float lastGravitySwitchTime;

    // Gravity
    private Vector3 gravityDir = Vector3.down;
    private Vector3 pendingGravityDir = Vector3.down;
    private Vector3 moveDirection;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _animator = GetComponent<Animator>();
        _actions = new PlayerActions();

        _rb.useGravity = false;
    }

    private void OnEnable()
    {
        _actions.Enable();
    }

    private void Update()
    {
        ReadInput();
        HandleJump();
        ReadRotationInput();
        SwitchGravity();
        UpdateAnimator();
    }

    private void FixedUpdate()
    {
        ApplyCustomGravity();
        Move();
        UpdateGroundedState();
        RotateTowardsMovement();
    }

    // ----------------------------------
    // INPUT (PLAYER-LOCAL DIRECTIONS)
    // ----------------------------------
    private void ReadInput()
    {
        moveInput = _actions.Movement.Move.ReadValue<Vector2>();
        rotateInput = _actions.Holo.Rot.ReadValue<Vector2>();
        jumpPressed = _actions.Jump.PlayerJump.triggered;
        gravitySwitchPressed = _actions.Gravity.GravitySwitch.triggered;

        Vector3 gravityUp = -gravityDir;

        // Player-facing forward projected onto surface
        Vector3 forward =
            Vector3.ProjectOnPlane(transform.forward, gravityDir).normalized;

        // SAFETY: fallback if forward collapses
        if (forward.sqrMagnitude < 0.001f)
        {
            Vector3 fallback = Vector3.forward;
            if (Mathf.Abs(Vector3.Dot(fallback, gravityUp)) > 0.9f)
                fallback = Vector3.right;

            forward = Vector3.ProjectOnPlane(fallback, gravityDir).normalized;
        }

        Vector3 right = Vector3.Cross(gravityUp, forward).normalized;

        moveDirection =
            forward * moveInput.y +
            right * moveInput.x;
    }



    // ----------------------------------
    // MOVEMENT
    // ----------------------------------
    private void Move()
    {
        if (moveDirection.sqrMagnitude < 0.01f) return;

        Vector3 newPos = _rb.position + moveDirection * moveSpeed * Time.fixedDeltaTime;
        _rb.MovePosition(newPos);
    }

    private void RotateTowardsMovement()
    {
        if (moveInput.y < 0f) return;

        if (moveDirection.sqrMagnitude < 0.01f) return;

        // Determine how much input is forward vs strafe
        float forwardAmount = Mathf.Abs(moveInput.y);
        float strafeAmount = Mathf.Abs(moveInput.x);

        float turnStrength =
            Mathf.Lerp(0.3f, 1f, forwardAmount);

        Quaternion targetRot =
            Quaternion.LookRotation(moveDirection, -gravityDir);

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRot,
            turnStrength * rotationSpeed * Time.fixedDeltaTime
        );
    }


    // ----------------------------------
    // GRAVITY
    // ----------------------------------
    private void ApplyCustomGravity()
    {
        _rb.AddForce(gravityDir * customGravityStrength, ForceMode.Acceleration);
    }

    private void SwitchGravity()
    {
        if (!gravitySwitchPressed || Time.time - lastGravitySwitchTime < gravitySwitchCooldown) return;

        Vector3 newGravity = pendingGravityDir.normalized;

        if (Vector3.Dot(gravityDir.normalized, newGravity) > 0.99f) return; // Same direction, skip switch

        // STEP 1: push player slightly away from the surface
        transform.position += newGravity * 1.8f;

        // STEP 2: switch gravity
        gravityDir = newGravity;

        // STEP 3: rotate player to align with new gravity
        transform.rotation = Quaternion.FromToRotation(transform.up, -gravityDir) * transform.rotation;

        /*        Vector3 forward =
                    Vector3.ProjectOnPlane(transform.forward, gravityDir).normalized;

                transform.rotation =
                    Quaternion.LookRotation(forward, -gravityDir);*/

        lastGravitySwitchTime = Time.time;
    }

    // ----------------------------------
    // JUMP
    // ----------------------------------
    private void HandleJump()
    {
        if (!jumpPressed || !isGrounded) return;

        Vector3 vel = _rb.velocity;
        vel -= Vector3.Project(vel, gravityDir);
        _rb.velocity = vel;

        _rb.AddForce(-gravityDir * jumpForce, ForceMode.Impulse);
    }

    private void UpdateGroundedState()
    {
        Vector3 origin = transform.position + (-gravityDir * 0.5f);

        isGrounded = Physics.Raycast(
            origin,
            gravityDir,
            out _,
            1.2f
        );
    }


    // ----------------------------------
    // HOLOGRAM (Arrow Keys)
    // ----------------------------------
    private void ReadRotationInput()
    {
        if (rotateInput == lastRotateInput) return;

        if (rotateInput == Vector2.zero)
        {
            lastRotateInput = Vector2.zero;
            return;
        }

        lastRotateInput = rotateInput;

        if (rotateInput.x > 0)
            RotatePreview(Vector3.forward);
        else if (rotateInput.x < 0)
            RotatePreview(Vector3.back);
        else if (rotateInput.y > 0)
            RotatePreview(Vector3.right);
        else if (rotateInput.y < 0)
            RotatePreview(Vector3.left);
    }

    private void RotatePreview(Vector3 localAxis)
    {
        Vector3 pivot = hologramPivot.position;
        Vector3 worldAxis = hologramPivot.TransformDirection(localAxis);

        hologramPivot.RotateAround(pivot, worldAxis, rotateAngle);

        Vector3 euler = hologramPivot.localEulerAngles;
        euler.y = 0f;
        euler.x = Mathf.Clamp(euler.x, 0f, 360f);
        euler.z = Mathf.Clamp(euler.z, 0f, 360f);
        hologramPivot.localEulerAngles = euler;

        // Feet direction = -up
        pendingGravityDir = -hologramPivot.up;
    }

    // ----------------------------------
    // ANIMATION
    // ----------------------------------
    private void UpdateAnimator()
    {
        if (!_animator) return;

        _animator.SetFloat("Speed", moveDirection.magnitude);
        _animator.SetBool("Grounded", isGrounded);
    }

    private void OnDisable()
    {
        _actions.Disable();
    }
}
