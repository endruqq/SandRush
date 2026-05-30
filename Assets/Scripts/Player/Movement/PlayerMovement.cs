using UnityEngine;

public class PlayerMovement
{
    // --- Settings ---
    private readonly float _moveSpeed;
    private readonly float _accelerationTime;

    // --- Dash Settings ---
    private readonly float _dashSpeed = 30f;
    private readonly float _dashDuration = 0.1f;
    
    // Charge System
    private readonly int _maxDashCharges = 2;
    private readonly float _dashRechargeTime = 3f;
    private int _currentDashCharges;
    private float _dashRechargeTimer; // counts DOWN from _dashRechargeTime

    // --- References ---
    private readonly CharacterController _controller;
    private readonly Transform _cameraTransform;

    // --- State ---
    private Vector3 _currentMoveVelocity;
    private Vector3 _velocityDamper;
    private float _dashDurationTimer; // Renamed from _dashTimer for clarity
    private bool _isDashing;

    // --- Public State ---
    public float SpeedMultiplier { get; set; } = 1f;
    public bool IsDashEnabled { get; set; } = true;
    public bool JustDashed { get; private set; }
    public int CurrentDashCharges => _currentDashCharges;
    public float DashRechargeProgress => GetActualDashRechargeTime() > 0.001f ? 1f - (_dashRechargeTimer / GetActualDashRechargeTime()) : 1f; // 0..1
    
    private float GetActualDashRechargeTime()
    {
        return _dashRechargeTime * (Player.Instance != null ? Player.Instance.MaskCooldownMultiplier : 1f);
    }
    public Vector3 WorldMoveDirection { get; private set; }

    public PlayerMovement(CharacterController controller, Transform cameraTransform, float moveSpeed, float accelerationTime)
    {
        _controller = controller;
        _cameraTransform = cameraTransform;
        _moveSpeed = moveSpeed;
        _accelerationTime = accelerationTime;
        
        // Initialize charges
        _currentDashCharges = _maxDashCharges;
        _dashRechargeTimer = 0f;
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

        HandleDashState(WorldMoveDirection); // This handles input AND recharging
        ApplyGravity();

        if (_isDashing)
        {
            // Apply gravity even during dash
            Vector3 dashMove = WorldMoveDirection * _dashSpeed * Time.deltaTime;
            dashMove.y = _verticalVelocity * Time.deltaTime;
            _controller.Move(dashMove);
            return;
        }

        Vector3 targetVelocity = WorldMoveDirection * (_moveSpeed * SpeedMultiplier * (Player.Instance != null ? Player.Instance.MovementSpeedMultiplier : 1f));
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
        // 1. Handle Duration (State Reset)
        if (_isDashing)
        {
            _dashDurationTimer -= Time.deltaTime;
            if (_dashDurationTimer <= 0) _isDashing = false;
        }

        // 2. Handle Recharging
        if (_currentDashCharges < _maxDashCharges)
        {
            _dashRechargeTimer -= Time.deltaTime;
            if (_dashRechargeTimer <= 0)
            {
                _currentDashCharges++;
                // If we still have room for more charges, start next timer
                if (_currentDashCharges < _maxDashCharges)
                {
                    _dashRechargeTimer = GetActualDashRechargeTime();
                }
                else
                {
                    _dashRechargeTimer = 0;
                }
            }
        }

        // 3. Handle Input
        // Must have charges, input direction, and not be currently dashing (optional, but prevents overlapping dashes)
        bool canDash = IsDashEnabled && (Player.Instance == null || !Player.Instance.IsDashBlocked);
        if (canDash && Input.GetKeyDown(KeyCode.Space) && !_isDashing && _currentDashCharges > 0 && moveDirection.sqrMagnitude > 0.1f)
        {
            JustDashed = true; 
            _isDashing = true;
            _dashDurationTimer = _dashDuration;
            
            // Consume charge
            _currentDashCharges--;
            
            // If we were at full charges, start the recharge timer now
            if (_currentDashCharges == _maxDashCharges - 1) 
            {
                _dashRechargeTimer = GetActualDashRechargeTime();
            }
        }
    }
}
