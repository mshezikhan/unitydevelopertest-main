using System;
using UnityEngine;
using UnityEngine.EventSystems;


public class PlayerController : MonoBehaviour
{
    // Reference
    private Rigidbody _rb;
    private Animator _animator;
    private PlayerActions _actions;

    [Header("Config")]
    [SerializeField] float moveSpeed = 5f;
    [SerializeField] float jumpForce = 5f;

    // States
    private Vector2 moveInput;
    private Vector3 moveDirection;
    private bool jumpPressed;
    private bool isGrounded;


    // Pick all components from Player object using GetComponent<>();
    private void Awake()
    {
        // Make sure all of these attached to Player object via inspector.
        _rb = GetComponent<Rigidbody>();
        _animator = GetComponent<Animator>();
        _actions = new PlayerActions(); // Create a new instance of PlayerActions and use that
    }

    // Enable Inputs when player object loads.
    private void OnEnable()
    {
        _actions.Enable();
    }

    // Update is called once per frame
    void Update()
    {
        ReadMovement();
        RotateTowardsMovement();
        UpdateGroundedState();
        HandleJump();
        UpdateAnimator();
    }


    private void FixedUpdate()
    {
        Move();
    }

    private void ReadMovement()
    {
        // Read WASD input and feed in Vector 2
        moveInput = _actions.Movement.Move.ReadValue<Vector2>();

        // Get move direction from move input
        moveDirection = new Vector3(moveInput.x, 0f, moveInput.y);

        // Check for Jump button (SAPCEBAR)
        jumpPressed = _actions.Jump.PlayerJump.triggered;

    }

    private bool IsPlayerMoving()
    {
        if (moveDirection.sqrMagnitude < 0.01f) // Check if player is not moving
            return true;
        return false;
    }


    private void Move() 
    {
        if (IsPlayerMoving()) return;

        moveDirection = moveDirection * -1; // Fixes reverse movements

        Vector3 newPosition = // Prepare new pos with provided Speed
            _rb.position + moveDirection.normalized * moveSpeed * Time.fixedDeltaTime;

        _rb.MovePosition(newPosition); // Apply new pos on RigidBody (it finally moves player to a position)
    }

    private void RotateTowardsMovement()
    {   // face the Move Direction

        if (IsPlayerMoving()) return;

        Quaternion targetRotation = Quaternion.LookRotation(moveDirection *-1 , Vector3.up);
        transform.rotation = targetRotation;
    }

    private void UpdateGroundedState()
    {
        // TEMP grounded check based on _rb velocity
        isGrounded = Mathf.Abs(_rb.velocity.y) < 0.05f;
    }

    private void HandleJump()
    {
        if (!jumpPressed) return;
        if (!isGrounded) return;

        Vector3 velocity = _rb.velocity; // Fetch _rb velocity
        velocity.y = 0f;              // Reset falling speed
        _rb.velocity = velocity;  // Feed new velocity in _rb

        _rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse); // Jump

        isGrounded = false;   // lock jumping


    }


    private void UpdateAnimator()
    {
        // Feed values to the animator for aniamtions

        // Feed speed value
        float speed = moveDirection.magnitude;
        _animator.SetFloat("Speed", speed);

        // Feed jump bool
        _animator.SetBool("Grounded", isGrounded);
    }


    // Disable Inputs when player disables. (It prevents ghost inputs)
    private void OnDisable()
    {
        _actions.Disable();
    }
}
