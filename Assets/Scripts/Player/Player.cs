using UnityEngine;
using Unity.Cinemachine;
using TMPro;

[RequireComponent(typeof(CharacterController))]
public class Player : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private Transform _firePoint;
    [SerializeField] private GameObject[] _firePointVFX;
    [SerializeField] private GameObject _bulletPrefab;
    [SerializeField] private Animator _animator;
    [SerializeField] private TextMeshProUGUI _dashCooldownText;
    [SerializeField] private DashUI _dashUI;
    
    [Header("Shooting Stats")]
    [SerializeField] private float _bulletSpeed = 25f;
    [SerializeField] private int _magazineSize = 25;
    [SerializeField] private float _reloadTime = 1.5f;
    [SerializeField] private AmmoUI _ammoUI;

    [Header("Health & Stats")]
    [SerializeField] private int _maxHealth = 100;


    [Header("Weapon Animation")]
    [SerializeField] private Animator _weaponAnimator;
    [SerializeField] private string _recoilAnimationTrigger = "Recoil";
    [Header("Shooting Layer Override")]
    [Tooltip("Name of the layer that plays shooting animations.")]
    [SerializeField] private string _shootingLayerName = "ShootingGunLayer";
    [Tooltip("Name of the upper body layer (idle aim).")]
    [SerializeField] private string _upperBodyLayerName = "Upper Body";
    [Tooltip("How fast the layer weights blend.")]
    [SerializeField] private float _layerBlendSpeed = 10f;

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
    private CursorCross _cursorCross;
    private DashGhostEffect _dashGhost;
    private PlayerDeathEffect _deathEffect;
    private HitFlash _hitFlash;
    private static bool _isDead = false;
    private static Vector3 _lastHitDirection = Vector3.forward;
    
    // Shooting layer indices
    private int _shootingLayerIndex = -1;
    private int _upperBodyLayerIndex = -1;
    private float _shootingLayerWeight = 0f;
    private float _lastShotTime = -10f;

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
    public PlayerShooting Shooting => _shooting;
    public PlayerAiming Aiming => _aiming;
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
        _shooting = new PlayerShooting(transform, _firePoint, _bulletPrefab, _bulletSpeed, _firePointVFX, _weaponAnimator, _recoilAnimationTrigger, _magazineSize, _reloadTime);
        
        // Subscribe to shooting events
        _shooting.OnShoot += OnShootHandler;

        // Wire up crosshair feedback
        _cursorCross = FindObjectOfType<CursorCross>();
        if (_cursorCross != null)
        {
            _shooting.OnShoot += _cursorCross.OnShoot;
            _shooting.OnReloadStateChanged += (isReloading) =>
                _cursorCross.OnReloadStateChanged(isReloading, _reloadTime);
        }

        // Initialize ammo UI
        if (_ammoUI != null)
        {
            _ammoUI.Initialize(_shooting);
        }
        
        // Get layer indices
        if (_animator != null)
        {
            _shootingLayerIndex = _animator.GetLayerIndex(_shootingLayerName);
            _upperBodyLayerIndex = _animator.GetLayerIndex(_upperBodyLayerName);
            
            if (_shootingLayerIndex == -1)
                Debug.LogWarning($"[Player] Layer '{_shootingLayerName}' not found in Animator.");
            if (_upperBodyLayerIndex == -1)
                Debug.LogWarning($"[Player] Layer '{_upperBodyLayerName}' not found in Animator.");
        }
        
        if (_dashCooldownText != null) _dashCooldownText.gameObject.SetActive(false);
        _lastRotation = transform.rotation;
        
        if (_worldCursorPrefab != null)
        {
            GameObject cursorInstance = Instantiate(_worldCursorPrefab);
            _worldCursor = cursorInstance.GetComponent<WorldSpaceCursor>();
        }
        Cursor.visible = false;

        // Get optional effects
        _dashGhost = GetComponent<DashGhostEffect>();
        _deathEffect = GetComponent<PlayerDeathEffect>();
        _hitFlash = GetComponent<HitFlash>();
        _isDead = false;
    }
    
    private void OnDestroy()
    {
        if (_shooting != null)
        {
            _shooting.OnShoot -= OnShootHandler;
            if (_cursorCross != null)
                _shooting.OnShoot -= _cursorCross.OnShoot;
        }
    }

    private void OnShootHandler()
    {
        if (_gunshotImpulseSource != null) 
            _gunshotImpulseSource.GenerateImpulse();
    }
    
    void Start()
    {
        // Initialize Health in Start to ensure UI is ready
        CurrentHealth = _maxHealth;
        HealthUI.Initialize(CurrentHealth, _maxHealth);
        Debug.Log($"[Player] Health Initialized: {CurrentHealth}/{_maxHealth}");
    }
    
    void Update()
    {
        if (_hasMask)
        {
            // ... (Mask logic unchanged) ...
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

        if (_movement.JustDashed)
        {
            if (_dashImpulseSource != null) _dashImpulseSource.GenerateImpulse();
            if (_dashGhost != null) _dashGhost.SpawnGhostTrail();
        }
        
        // REMOVED manual impulse check here - driven by _shooting.OnShoot

        // --- SHOOTING LOGIC MOVED TO UPDATE FOR RESPONSIVENESS ---
        // Calculate shoot direction
        Vector3 stableOrigin = transform.position;
        // Check for null just in case firepoint was destroyed or not assigned yet
        if (_firePoint != null) stableOrigin.y = _firePoint.position.y;
        
        Vector3 shootDirection = (_aiming.AimPosition - stableOrigin).normalized;
        
        bool firedThisFrame = _shooting.Tick(shootDirection);
        if (firedThisFrame) _lastShotTime = Time.time;
        
        UpdateShootingLayerWeights();
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

        // Shooting logic moved to Update()
        
        CalculateTurnSpeed();
    }

    public static void TakeDamage(int amount)
    {
        if (_instance == null || _isDead) return;
        
        Debug.Log($"[Player.TakeDamage] Amount: {amount}, HP Before: {_instance.CurrentHealth}");
        
        _instance.CurrentHealth = Mathf.Clamp(_instance.CurrentHealth - amount, 0, _instance._maxHealth);
        HealthUI.UpdateHealth(_instance.CurrentHealth);

        // Trigger hit flash
        if (_instance._hitFlash != null)
        {
            _instance._hitFlash.Flash();
        }

        if (_instance.CurrentHealth <= 0)
        {
            _isDead = true;
            if (_instance._deathEffect != null)
            {
                _instance._deathEffect.TriggerDeath(_lastHitDirection);
            }
        }
    }
    
    /// <summary>
    /// Call this to set the direction of the last hit (for ragdoll knockback).
    /// </summary>
    public static void SetLastHitDirection(Vector3 direction)
    {
        _lastHitDirection = direction;
    }
    
    public static void GetUltimate(int amount)
    {
        if (_instance == null) return;
        _instance.CurrentUltimate = Mathf.Clamp(_instance.CurrentUltimate + amount, 0, 10);
        UltimateUI.UpdateUltimate(_instance.CurrentUltimate);
    }
    
    private void UpdateShootingLayerWeights()
    {
        if (_animator == null || _shootingLayerIndex == -1 || _upperBodyLayerIndex == -1)
            return;
        
        // Check if player is currently holding fire button
        bool isShooting = Input.GetMouseButton(0);
        
        // Target weight: 1 when shooting, 0 when not
        float targetWeight = isShooting ? 1f : 0f;
        
        // Smoothly blend the weight
        _shootingLayerWeight = Mathf.MoveTowards(_shootingLayerWeight, targetWeight, _layerBlendSpeed * Time.deltaTime);
        
        // Apply weights: ShootingGunLayer gets the shooting weight, Upper Body gets the inverse
        _animator.SetLayerWeight(_shootingLayerIndex, _shootingLayerWeight);
        _animator.SetLayerWeight(_upperBodyLayerIndex, 1f - _shootingLayerWeight);
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
        // Update new visual UI
        if (_dashUI != null)
        {
            _dashUI.UpdateDashUI(_movement.CurrentDashCharges, _movement.DashRechargeProgress);
        }
        
        // Update legacy text if assigned (optional fallback)
        if (_dashCooldownText != null)
        {
             if (_movement.CurrentDashCharges > 0)
                _dashCooldownText.text = _movement.CurrentDashCharges.ToString();
             else
                _dashCooldownText.text = _movement.DashRechargeProgress.ToString("F1");
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
            


            yield return new WaitForSeconds(burstDelay);
        }
        
        Debug.Log("Mask Ability: Burst Fire Complete");
    }
}
