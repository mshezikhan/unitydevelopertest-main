using UnityEngine;

public class PlayerController : MonoBehaviour
{
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

    [Header("Fall Detection")]
    [SerializeField] float fallGraceTime = 4f; // it separates jump and fall logics

    private float fallTimer; // increases while player is falling

    // Input
    private Vector2 moveInput;
    private Vector2 rotateInput;
    private Vector2 lastRotateInput;

    // States
    private bool jumpPressed;
    private bool gravitySwitchPressed;
    private bool isGrounded;
    private float gravitySwitchCooldown = 0.2f; // prevents unneccary buttun press
    private float lastGravitySwitchTime;

    // gravity
    private Vector3 gravityDir = Vector3.down;
    private Vector3 pendingGravityDir = Vector3.down;
    private Vector3 moveDirection;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _animator = GetComponent<Animator>();
        _actions = new PlayerActions();

        _rb.useGravity = false; // we make force pretend like gravity
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
        CheckFalling();
    }

    private void FixedUpdate()
    {
        ApplyCustomGravity();
        Move();
        UpdateGroundedState();
        RotateTowardsMovement();
    }


    // read player input
    private void ReadInput()
    {
        moveInput = _actions.Movement.Move.ReadValue<Vector2>();
        rotateInput = _actions.Holo.Rot.ReadValue<Vector2>();
        jumpPressed = _actions.Jump.PlayerJump.triggered;
        gravitySwitchPressed = _actions.Gravity.GravitySwitch.triggered;

        Vector3 gravityUp = -gravityDir;

        // it make player able to walk on walls 
        Vector3 forward =
            Vector3.ProjectOnPlane(transform.forward, gravityDir).normalized;

/*        // SAFETY: fallback if forward collapses
        if (forward.sqrMagnitude < 0.001f) 
        {
            Vector3 fallback = Vector3.forward;
            if (Mathf.Abs(Vector3.Dot(fallback, gravityUp)) > 0.9f)
                fallback = Vector3.right;

            forward = Vector3.ProjectOnPlane(fallback, gravityDir).normalized;
        }*/

        Vector3 right = Vector3.Cross(gravityUp, forward).normalized;

        moveDirection =
            forward * moveInput.y +
            right * moveInput.x;
    }



    // player movement
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


    // gravity
    private void ApplyCustomGravity()
    {
        _rb.AddForce(gravityDir * customGravityStrength, ForceMode.Acceleration);
    }

    private void SwitchGravity()
    {
        if (!gravitySwitchPressed || Time.time - lastGravitySwitchTime < gravitySwitchCooldown) return;

        Vector3 newGravity = pendingGravityDir.normalized;

        if (Vector3.Dot(gravityDir.normalized, newGravity) > 0.99f) return; // Same direction, skip switch

        // 1 push player to prevent going under plane
        transform.position += newGravity * 1.8f;

        // 2 switch gravity
        gravityDir = newGravity;

        // 3 rotate player to align with new gravity
        transform.rotation = Quaternion.FromToRotation(transform.up, -gravityDir) * transform.rotation;

        lastGravitySwitchTime = Time.time;
        hologramPivot.gameObject.SetActive(false);
    }

    // player jump
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


    // holographic preview for gravity
    private void ReadRotationInput()
    {
        if (rotateInput == lastRotateInput) return;

        if (rotateInput == Vector2.zero)
        {
            lastRotateInput = Vector2.zero;
            // hologramPivot.gameObject.SetActive(false); (it auto hides it after few seconds)
            return;
        }

        hologramPivot.gameObject.SetActive(true);
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

        pendingGravityDir = -hologramPivot.up;
    }

    // player animation
    private void UpdateAnimator()
    {
        if (!_animator) return;

        _animator.SetFloat("Speed", moveDirection.magnitude);
        _animator.SetBool("Grounded", isGrounded);
    }

    // check if player is falling
    private void CheckFalling()
    {
        // velocity in gravity direction
        float gravityVelocity = Vector3.Dot(_rb.velocity, gravityDir);

        bool isActuallyFalling =
            !isGrounded &&
            gravityVelocity > 0.5f; // moving with gravity

        if (isActuallyFalling)
        {
            fallTimer += Time.deltaTime; // increase timer

            if (fallTimer >= fallGraceTime) // detect if its jump or fall
            {
                GameManager.Instance.GameOver();
            }
        }
        else
        {
            fallTimer = 0f; // reset falling timer
        }
    }

    // cube collection
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Cube"))
        {
            GameManager.Instance.CollectCube();
            Destroy(collision.gameObject);
        }
    }

    private void OnDisable()
    {
        _actions.Disable();
    }
}
