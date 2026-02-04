using UnityEngine;
using Unity.Cinemachine;
using TMPro;

[RequireComponent(typeof(CharacterController))]
public class Player : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private Transform _firePoint;
    [SerializeField] private ParticleSystem _firePointParticles;
    [SerializeField] private GameObject _bulletPrefab;
    [SerializeField] private Animator _animator;
    [SerializeField] private TextMeshProUGUI _dashCooldownText;

    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 3.5f;
    [SerializeField] private float accelerationTime = 0.1f;
    
    [Header("Animation Settings")]
    [SerializeField] private float animationSpeedMultiplier = 1f;
    [Tooltip("Lower values make turn animations smoother and less jerky.")]
    [SerializeField] private float animationTurnSpeedSmoothing = 15f;

    [Header("Aiming Settings")]
    [SerializeField] private LayerMask groundMask;
    [SerializeField] private GameObject _worldCursorPrefab;
    [Tooltip("Adjust this value until the character's model faces the exact direction of the shots.")]
    [SerializeField] private float _rotationOffset = 0f;
    [Tooltip("How quickly the character turns to face the aim direction.")]
    [SerializeField] private float _rotationSpeed = 25f;
    
    [Header("Impulse Sources")]
    [SerializeField] private CinemachineImpulseSource _dashImpulseSource;
    [SerializeField] private CinemachineImpulseSource _gunshotImpulseSource;
    
    [Header("Mask Ability")]
    [SerializeField] private float _maskAbilityCooldown = 5f;
    [SerializeField] private float _maskAbilityDamage = 50f;
    
    [Header("Procedural Animation (Synthetik Style)")]
    [Tooltip("Assign the Spine or Chest bone here to lock it to the aim direction.")]
    [SerializeField] private Transform _upperBodyBone;
    [Tooltip("Adjust rotation if the body is twisted. Try (0, 90, 0) or (0, -90, 0) if needed.")]
    [SerializeField] private Vector3 _upperBodyOffset = Vector3.zero;

    private PlayerMovement _movement;
    private PlayerAiming _aiming;
    private PlayerShooting _shooting;
    private CharacterController _controller;
    private Quaternion _lastRotation;
    private float _turnSpeed;
    private WorldSpaceCursor _worldCursor;
    private Camera _mainCamera;

    private bool _hasMask = false;
    private float _maskCooldownTimer = 0f;
    // Event for UI to listen to
    public System.Action<bool> OnMaskEquipped;
    public System.Action<float> OnMaskCooldownChanged;

    private readonly int _moveXHash = Animator.StringToHash("MoveX");
    private readonly int _moveYHash = Animator.StringToHash("MoveY");
    private readonly int _speedHash = Animator.StringToHash("Speed");
    private readonly int _turnSpeedHash = Animator.StringToHash("TurnSpeed");

    // Restored public access for UI scripts
    public int CurrentHealth { get; set; } = 100;
    public int CurrentUltimate { get; set; } = 0;
    private static Player _instance;

    void Awake()
    {
        _instance = this;
        _controller = GetComponent<CharacterController>();
        
        // --- SAFE INITIALIZATION ---
        if (_mainCamera == null) _mainCamera = Camera.main;
        if (_mainCamera == null) _mainCamera = FindObjectOfType<Camera>();
        
        if (_mainCamera == null)
        {
            Debug.LogError("[Player] Critical Error: No Camera found! Script functionalities will be limited.");
            enabled = false;
            return;
        }

        if (_firePoint == null)
        {
            Debug.LogWarning("[Player] FirePoint not assigned. Using Player Transform.");
            _firePoint = transform;
        }

        _aiming = new PlayerAiming(_mainCamera, _firePoint, groundMask);
        _movement = new PlayerMovement(_controller, _mainCamera.transform, moveSpeed, accelerationTime);
        _shooting = new PlayerShooting(transform, _firePoint, _bulletPrefab, _firePointParticles);
        
        if (_dashCooldownText != null) _dashCooldownText.gameObject.SetActive(false);
        _lastRotation = transform.rotation;
        
        if (_worldCursorPrefab != null)
        {
            GameObject cursorInstance = Instantiate(_worldCursorPrefab);
            _worldCursor = cursorInstance.GetComponent<WorldSpaceCursor>();
        }
        Cursor.visible = false;
    }
    
    void Update()
    {
        if (_hasMask)
        {
            if (_maskCooldownTimer > 0)
            {
                _maskCooldownTimer -= Time.deltaTime;
                OnMaskCooldownChanged?.Invoke(_maskCooldownTimer / _maskAbilityCooldown);
            }
            
            if (Input.GetMouseButtonDown(1) && _maskCooldownTimer <= 0)
            {
                UseMaskAbility();
            }
        }

        _aiming.Tick();
        _movement.Tick(); 

        if (_worldCursor != null)
        {
            _worldCursor.UpdatePosition(_aiming.GroundPosition);
        }

        UpdateAnimator();
        UpdateDashCooldownUI();

        if (_movement.JustDashed && _dashImpulseSource != null) 
            _dashImpulseSource.GenerateImpulse();
        if (Input.GetMouseButtonDown(0) && _gunshotImpulseSource != null) 
            _gunshotImpulseSource.GenerateImpulse();
    }

    void LateUpdate()
    {
        Vector3 lookDirection = _aiming.GroundPosition - transform.position;
        lookDirection.y = 0;

        if (lookDirection.sqrMagnitude > 0.01f)
        {
            Quaternion targetLookRotation = Quaternion.LookRotation(lookDirection);
            Quaternion finalRotation = targetLookRotation * Quaternion.Euler(0, _rotationOffset, 0);
            transform.rotation = Quaternion.Slerp(transform.rotation, finalRotation, Time.deltaTime * _rotationSpeed);
        }

        // FIX: Calculate shoot direction from player center (at weapon height) to aim target.
        // This decouples the aiming accuracy from the running animation sway (upper body movement).
        Vector3 stableOrigin = transform.position;
        stableOrigin.y = _firePoint.position.y;
        Vector3 shootDirection = (_aiming.AimPosition - stableOrigin).normalized;
        
        _shooting.Tick(shootDirection);

        CalculateTurnSpeed();
    }

    public static void TakeDamage(int amount)
    {
        if (_instance == null) return;
        _instance.CurrentHealth = Mathf.Clamp(_instance.CurrentHealth - amount, 0, 100);
        HealthUI.UpdateHealth(_instance.CurrentHealth);
    }
    
    public static void GetUltimate(int amount)
    {
        if (_instance == null) return;
        _instance.CurrentUltimate = Mathf.Clamp(_instance.CurrentUltimate + amount, 0, 10);
        UltimateUI.UpdateUltimate(_instance.CurrentUltimate);
    }
    
    private void CalculateTurnSpeed()
    {
        Quaternion currentRotation = transform.rotation;
        Quaternion deltaRotation = currentRotation * Quaternion.Inverse(_lastRotation);
        deltaRotation.ToAngleAxis(out float angle, out Vector3 axis);

        if (float.IsNaN(axis.x)) return;

        float turnDirection = Mathf.Sign(axis.y);
        float turnAnglePerSecond = (angle * turnDirection) / Time.deltaTime;
        
        _turnSpeed = Mathf.Lerp(_turnSpeed, Mathf.Clamp(turnAnglePerSecond / 360f, -1f, 1f), Time.deltaTime * animationTurnSpeedSmoothing);
        _lastRotation = currentRotation;
    }

    private void UpdateAnimator()
    {
        if (_animator == null) return;

        // --- SPEED ---
        // Use the character's actual velocity for the main speed parameter.
        // This ensures the character stops animating if it hits a wall.
        Vector3 worldVelocity = _controller.velocity;
        worldVelocity.y = 0;
        float currentSpeed = worldVelocity.magnitude;
        float normalizedSpeed = Mathf.Clamp01(currentSpeed / moveSpeed);
        
        _animator.SetFloat(_speedHash, normalizedSpeed * animationSpeedMultiplier);
        _animator.SetFloat(_turnSpeedHash, _turnSpeed);

        // --- DIRECTION ---
        // Calculate direction based on player input relative to the aim direction.
        // This is more reliable than using transform.InverseTransformDirection, which can be affected
        // by the timing of object rotation updates (Update vs. LateUpdate).

        // Get the intended movement direction from the input, in world space.
        Vector3 worldMoveDirection = _movement.WorldMoveDirection;

        if (worldMoveDirection.magnitude > 0.1f)
        {
            // Get the direction the player is aiming, in world space.
            Vector3 lookDirection = _aiming.GroundPosition - transform.position;
            lookDirection.y = 0;
            lookDirection.Normalize();

            // Create a rotation that represents looking in the aim direction.
            Quaternion lookRotation = Quaternion.LookRotation(lookDirection);

            // Transform the world movement direction into the 'local space' of the aim rotation.
            // This gives us a vector where X is sideways relative to aiming, and Z is forward/backward.
            Vector3 localMoveDirection = Quaternion.Inverse(lookRotation) * worldMoveDirection;

            // FIX: Map the circular normalized vector to a square to hit (1,1) on the Blend Tree.
            // This prevents blending issues (leg twisting) where the animator stucks at 70% between Forward/Side and Diagonal.
            float maxDir = Mathf.Max(Mathf.Abs(localMoveDirection.x), Mathf.Abs(localMoveDirection.z));
            if (maxDir > 0.01f)
            {
                localMoveDirection /= maxDir;
            }

            _animator.SetFloat(_moveXHash, localMoveDirection.x);
            _animator.SetFloat(_moveYHash, localMoveDirection.z);
        }
        else
        {
            // If there is no input, reset the direction parameters to idle.
            _animator.SetFloat(_moveXHash, 0f);
            _animator.SetFloat(_moveYHash, 0f);
        }
    }
    
    private void UpdateDashCooldownUI()
    {
        if (_dashCooldownText == null) return;

        float cooldown = _movement.DashCooldownTimer;
        if (cooldown > 0)
        {
            _dashCooldownText.gameObject.SetActive(true);
            _dashCooldownText.text = cooldown.ToString("F1");
        }
        else
        {
            _dashCooldownText.gameObject.SetActive(false);
        }
    }

    public void EquipWeapon(GameObject bulletPrefab)
    {
        _shooting.EquipWeapon(bulletPrefab);
    }

    public void EquipMask()
    {
        if (_hasMask) return;
        
        _hasMask = true;
        // Passive: +10% fire rate (dividing delay by 1.1)
        _shooting.ModifyFireRate(1.1f); 
        OnMaskEquipped?.Invoke(true);
        Debug.Log("Mask Equipped: +10% Fire Rate active.");
    }

    private void UseMaskAbility()
    {
        if (_maskCooldownTimer > 0) return;
        
        _maskCooldownTimer = _maskAbilityCooldown;
        OnMaskCooldownChanged?.Invoke(1f);
        
        StartCoroutine(BurstFireRoutine());
    }

    private System.Collections.IEnumerator BurstFireRoutine()
    {
        int shots = 5;
        float burstDelay = 0.08f; // Very fast burst
        
        for (int i = 0; i < shots; i++)
        {
            // Calculate direction same as LateUpdate
            Vector3 stableOrigin = transform.position;
            stableOrigin.y = _firePoint.position.y;
            Vector3 shootDirection = (_aiming.AimPosition - stableOrigin).normalized;

            _shooting.FireImmediate(shootDirection);
            
            // Generate visual impulse for feedback
            if (_gunshotImpulseSource != null) _gunshotImpulseSource.GenerateImpulse(0.5f);

            yield return new WaitForSeconds(burstDelay);
        }
        
        Debug.Log("Mask Ability: Burst Fire Complete");
    }
}
