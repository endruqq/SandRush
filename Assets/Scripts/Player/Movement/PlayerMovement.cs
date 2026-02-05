using UnityEngine;

public class PlayerMovement
{
    // --- Settings ---
    private readonly float _moveSpeed;
    private readonly float _accelerationTime;

    // --- Dash Settings ---
    private readonly float _dashSpeed = 30f;
    private readonly float _dashDuration = 0.1f;
    private readonly float _dashCooldown = 1f;

    // --- References ---
    private readonly CharacterController _controller;
    private readonly Transform _cameraTransform;

    // --- State ---
    private Vector3 _currentMoveVelocity;
    private Vector3 _velocityDamper;
    private float _dashCooldownTimer;
    private float _dashTimer;
    private bool _isDashing;

    // --- Public State ---
    public bool JustDashed { get; private set; }
    public float DashCooldownTimer => _dashCooldownTimer;
    public Vector3 WorldMoveDirection { get; private set; }

    public PlayerMovement(CharacterController controller, Transform cameraTransform, float moveSpeed, float accelerationTime)
    {
        _controller = controller;
        _cameraTransform = cameraTransform;
        _moveSpeed = moveSpeed;
        _accelerationTime = accelerationTime;
    }

    public void Tick()
    {
        JustDashed = false; 
        float horizontalInput = Input.GetAxisRaw("Horizontal");
        float verticalInput = Input.GetAxisRaw("Vertical");

        Vector3 camForward = _cameraTransform.forward;
        camForward.y = 0;
        camForward.Normalize();

        Vector3 camRight = _cameraTransform.right;
        camRight.y = 0;
        camRight.Normalize();
        
        WorldMoveDirection = (camForward * verticalInput + camRight * horizontalInput).normalized;

        HandleDashState(WorldMoveDirection);
        ApplyGravity();

        if (_isDashing)
        {
            // Apply gravity even during dash
            Vector3 dashMove = WorldMoveDirection * _dashSpeed * Time.deltaTime;
            dashMove.y = _verticalVelocity * Time.deltaTime;
            _controller.Move(dashMove);
            return;
        }

        Vector3 targetVelocity = WorldMoveDirection * _moveSpeed;
        _currentMoveVelocity = Vector3.SmoothDamp(_currentMoveVelocity, targetVelocity, ref _velocityDamper, _accelerationTime);
        
        // Combine lateral movement with vertical gravity
        Vector3 finalVelocity = _currentMoveVelocity;
        finalVelocity.y = _verticalVelocity;
        
        _controller.Move(finalVelocity * Time.deltaTime);
    }

    // --- Gravity ---
    private float _verticalVelocity;
    private float _gravity = -20f; // Could be passed in constructor
    private float _groundedGravity = -2f;

    private void ApplyGravity()
    {
        if (_controller.isGrounded && _verticalVelocity < 0)
        {
            _verticalVelocity = _groundedGravity;
        }

        _verticalVelocity += _gravity * Time.deltaTime;
        
        // Optional: Terminal velocity cap could be added here
    }

    private void HandleDashState(Vector3 moveDirection)
    {
        if (_dashCooldownTimer > 0) _dashCooldownTimer -= Time.deltaTime;
        
        if (_dashTimer > 0)
        {
            _dashTimer -= Time.deltaTime;
            if (_dashTimer <= 0) _isDashing = false;
        }

        if (Input.GetKeyDown(KeyCode.Space) && _dashCooldownTimer <= 0 && moveDirection.sqrMagnitude > 0.1f)
        {
            JustDashed = true; 
            _isDashing = true;
            _dashCooldownTimer = _dashCooldown;
            _dashTimer = _dashDuration;
        }
    }
}
