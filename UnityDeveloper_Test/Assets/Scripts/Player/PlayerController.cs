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
    [SerializeField] Transform cameraTransform;

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

    // Custom gravity
    private Vector3 gravityDir = Vector3.down;
    private Vector3 pendingGravityDir = Vector3.down;

    private Vector3 moveDirection;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _animator = GetComponent<Animator>();
        _actions = new PlayerActions();

        // IMPORTANT: disable Unity gravity
        _rb.useGravity = false;
    }

    private void OnEnable()
    {
        _actions.Enable();
    }

    private void Update()
    {
        ReadInput();
        RotateTowardsMovement();
        HandleJump();
        UpdateAnimator();
        ReadRotationInput();
        SwitchGravity();
    }

    private void FixedUpdate()
    {
        ApplyCustomGravity();
        Move();
        UpdateGroundedState();
    }

    private void ReadInput()
    {
        moveInput = _actions.Movement.Move.ReadValue<Vector2>();
        rotateInput = _actions.Holo.Rot.ReadValue<Vector2>();
        jumpPressed = _actions.Jump.PlayerJump.triggered;
        gravitySwitchPressed = _actions.Gravity.GravitySwitch.triggered;

        Vector3 camForward = cameraTransform.forward;
        Vector3 camRight = cameraTransform.right;

        camForward.y = 0f;
        camRight.y = 0f;

        camForward.Normalize();
        camRight.Normalize();

        moveDirection = camForward * moveInput.y + camRight * moveInput.x;
    }

    private void Move()
    {
        if (moveDirection.sqrMagnitude < 0.01f) return;

        Vector3 newPos =
            _rb.position + moveDirection.normalized * moveSpeed * Time.fixedDeltaTime;

        _rb.MovePosition(newPos);
    }

    private void RotateTowardsMovement()
    {
        if (moveDirection.sqrMagnitude < 0.01f) return;

        Quaternion targetRot =
            Quaternion.LookRotation(moveDirection, -gravityDir);

        transform.rotation = targetRot;
    }

    private void ApplyCustomGravity()
    {
        _rb.AddForce(gravityDir * customGravityStrength, ForceMode.Acceleration);
    }

    private void HandleJump()
    {
        if (!jumpPressed) return;
        if (!isGrounded) return;

        Vector3 vel = _rb.velocity;
        vel -= Vector3.Project(vel, gravityDir);
        _rb.velocity = vel;

        _rb.AddForce(-gravityDir * jumpForce, ForceMode.Impulse);
    }

    private void UpdateGroundedState()
    {
        isGrounded = Physics.Raycast(
            transform.position,
            gravityDir,
            out _,
            1.1f
        );
    }

    private void UpdateAnimator()
    {
        _animator.SetFloat("Speed", moveDirection.magnitude);
        _animator.SetBool("Grounded", isGrounded);
    }

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
        hologramPivot.localEulerAngles = euler;

        // feet direction = -up
        pendingGravityDir = -hologramPivot.up;
    }

    private void SwitchGravity()
    {
        if (!gravitySwitchPressed) return;

        gravityDir = pendingGravityDir.normalized;

        // align player instantly to new gravity
        transform.rotation =
            Quaternion.FromToRotation(transform.up, -gravityDir) * transform.rotation;
    }

    private void OnDisable()
    {
        _actions.Disable();
    }
}
