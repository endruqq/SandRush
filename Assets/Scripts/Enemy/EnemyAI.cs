using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyAI : MonoBehaviour
{
    public enum AttackType { Ranged, Melee, Exploder }
    
    [Header("AI Behavior")]
    [SerializeField] private AttackType _attackType = AttackType.Ranged;
    
    [Header("Dependencies")]
    [SerializeField] private Transform _playerTransform;

    [Header("AI Stats")]
    [SerializeField] private float _detectionRadius = 15f;
    [SerializeField] private float _attackRange = 10f;
    [SerializeField] private float _attackRate = 1f;

    [Header("Ranged Weapon Settings")]
    [SerializeField] private GameObject _bulletPrefab;
    [SerializeField] private Transform[] _firePoints;
    [SerializeField] private float _bulletSpeed = 20f;
    [SerializeField] private float _rangedDamage = 25f;
    [SerializeField] private GameObject[] _rangedVFXPrefabs;
    [SerializeField] private bool _enableStrafing = true;
    [SerializeField] private float _strafeSpeed = 2f;
    [SerializeField] private float _strafeChangeInterval = 2f;
    
    [Header("Melee Settings")]
    [SerializeField] private int _meleeDamage = 10;
    [SerializeField] private float _meleeHitDelay = 0.3f; // Delay before damage/VFX (sync with animation)
    [SerializeField] private Transform _meleeVFXPoint;
    [SerializeField] private GameObject[] _meleeVFXPrefabs;
    
    [Header("Exploder Settings")]
    [SerializeField] private int _explosionDamage = 50;
    [SerializeField] private float _explosionRadius = 5f;
    [SerializeField] private float _explosionFuseTime = 1.5f;
    [SerializeField] private GameObject _explosionVFX;
    
    [Header("FMOD Sounds")]
    [SerializeField] private string _attackSound = "event:/Scarab_Attack";
    [SerializeField] private string _detectionSound = "event:/Detection_Alert";
    [SerializeField] private string _explosionSound = "event:/Explosions";

    [Header("Animation")]
    [SerializeField] private Animator _animator;
    [SerializeField] private string _isWalkingBool = "IsWalking";
    [SerializeField] private string _attackTrigger = "Attack";

    [Header("Search & Line of Sight")]
    [SerializeField] private float _losHeightOffset = 1.2f;
    [SerializeField] private float _searchDuration = 2.0f;

    private enum State { Idle, Chasing, Attacking, Searching, ReturningToOrigin }
    private State _currentState;
    private NavMeshAgent _navAgent;
    
    private float _distanceToPlayer;
    private float _nextAttackTime;
    private float _fuseTimer;
    private bool _isFuseLit;
    private float _originalSpeed;
    private float _strafeTimer;
    
    // AI enhancements
    private float _randomAngleOffset;
    private float _speedVariance;

    // Search/Return behaviors
    private Vector3 _originalPosition;
    private Vector3 _lastSeenPosition;
    private float _searchTimer;
    
    // --- DEBUG ---
    private State _previousState;

    void Awake()
    {
        _navAgent = GetComponent<NavMeshAgent>();
        if (_navAgent != null) _originalSpeed = _navAgent.speed;
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if(playerObj != null) _playerTransform = playerObj.transform;
        
        _randomAngleOffset = Random.Range(0f, 360f);
        _speedVariance = Random.Range(0.85f, 1.25f); // 15-25% variation so they don't form identical lines!

        if (_animator == null)
        {
            _animator = GetComponentInChildren<Animator>();
        }
    }

    void Start()
    {
        _currentState = State.Idle;
        _previousState = State.Idle;
        _originalPosition = transform.position;
        
        // Check if on NavMesh, try to warp if not
        if (!_navAgent.isOnNavMesh)
        {
            Debug.LogWarning($"{gameObject.name} is not on a NavMesh! Trying to find nearest NavMesh point...", this);
            
            // Try to find nearest point on NavMesh
            if (UnityEngine.AI.NavMesh.SamplePosition(transform.position, out UnityEngine.AI.NavMeshHit hit, 5f, UnityEngine.AI.NavMesh.AllAreas))
            {
                _navAgent.Warp(hit.position);
                Debug.Log($"{gameObject.name} warped to NavMesh at {hit.position}", this);
            }
            else
            {
                Debug.LogError($"{gameObject.name} could not find nearby NavMesh! Enemy will not move.", this);
            }
        }
        else
        {
            Debug.Log($"{gameObject.name} is on NavMesh and ready.", this);
        }
    }

    void Update()
    {
        if (_playerTransform == null)
        {
            // Try to find player again (might have spawned after enemy)
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                _playerTransform = playerObj.transform;
            }
            else
            {
                SetAgentStopped(true);
                return;
            }
        }
        _distanceToPlayer = Vector3.Distance(transform.position, _playerTransform.position);

        // --- BEGIN DEBUG ---
        _previousState = _currentState;
        // --- END DEBUG ---

        switch (_currentState)
        {
            case State.Idle: HandleIdleState(); break;
            case State.Chasing: HandleChasingState(); break;
            case State.Attacking: HandleAttackingState(); break;
            case State.Searching: HandleSearchingState(); break;
            case State.ReturningToOrigin: HandleReturningToOriginState(); break;
        }

        // --- BEGIN DEBUG ---
        if (_previousState != _currentState)
        {
            Debug.Log($"{gameObject.name} changed state from {_previousState} to {_currentState}", this);
        }
        // --- END DEBUG ---
    }

    private bool CheckLineOfSight()
    {
        if (_playerTransform == null) return false;
        
        Vector3 eyePos = transform.position + Vector3.up * _losHeightOffset;
        Vector3 targetPos = _playerTransform.position + Vector3.up * 1.0f;
        Vector3 direction = targetPos - eyePos;
        float distance = direction.magnitude;
        
        if (Physics.Raycast(eyePos, direction, out RaycastHit hit, distance))
        {
            if (hit.transform == _playerTransform || 
                hit.transform.CompareTag("Player") || 
                hit.transform.GetComponentInParent<Player>() != null)
            {
                return true;
            }
            return false;
        }
        
        return true;
    }

    private void HandleIdleState()
    {
        SetWalking(false);
        if (_distanceToPlayer <= _detectionRadius && CheckLineOfSight())
        {
            _currentState = State.Chasing;
            _lastSeenPosition = _playerTransform.position;
            // Play detection alert sound
            if (!string.IsNullOrEmpty(_detectionSound))
                FMODHelper.PlayOneShot(_detectionSound, transform.position);
        }
    }

    private void HandleChasingState()
    {
        _navAgent.updateRotation = true;
        
        // Melee enemies chase faster, and all enemies use speed variance to avoid walking in sync
        float chaseSpeed = _attackType == AttackType.Melee ? (_originalSpeed * 1.5f) * _speedVariance : _originalSpeed * _speedVariance;
        _navAgent.speed = chaseSpeed;
        
        if (CheckLineOfSight())
        {
            _lastSeenPosition = _playerTransform.position;
            _navAgent.SetDestination(_playerTransform.position);
            SetWalking(true);
            SetAgentStopped(false);
        }
        else
        {
            _currentState = State.Searching;
            _searchTimer = _searchDuration;
            _navAgent.SetDestination(_lastSeenPosition);
            SetWalking(true);
            SetAgentStopped(false);
            return;
        }
        
        if (_attackType == AttackType.Melee)
        {
            // Minor push to avoid walking exactly inside each other
            // Also handles encircling the player when close
            SeparateFromPeers();
        }
        
        // --- DEBUG REMOVED TO PREVENT RAM LEAKS FROM LOG SPAM ---
        
        if (_distanceToPlayer <= _attackRange)
        {
            // Rotate to face player ONCE before switching to attack
            RotateTowardsPlayer();
            SetWalking(false);
            _currentState = State.Attacking;
        }
        else if (_distanceToPlayer > _detectionRadius * 1.5f)
        {
            _currentState = State.ReturningToOrigin;
            _navAgent.SetDestination(_originalPosition);
        }
    }
    
    private void SeparateFromPeers()
    {
        if (!_navAgent.isOnNavMesh) return;
        
        // Detect if the enemy is on stairs or a slope to avoid pushing them off/under
        bool isOnSlope = false;
        if (UnityEngine.AI.NavMesh.SamplePosition(transform.position, out UnityEngine.AI.NavMeshHit navHit, 1.0f, UnityEngine.AI.NavMesh.AllAreas))
        {
            if (navHit.normal.y < 0.95f) // Slope angle is greater than ~18 degrees
            {
                isOnSlope = true;
            }
        }
        
        // Find nearby enemies to push away from
        Collider[] nearby = Physics.OverlapSphere(transform.position, 2.0f);
        Vector3 separationForce = Vector3.zero;
        int count = 0;
        
        foreach (var col in nearby)
        {
            // Use TryGetComponent instead of CompareTag to avoid "Tag not defined" errors
            if (col.gameObject != gameObject && col.TryGetComponent<EnemyAI>(out _))
            {
                Vector3 away = transform.position - col.transform.position;
                away.y = 0; // Keep push horizontal
                
                float dist = away.magnitude;
                if (dist < 1.5f && dist > 0.01f) // They are crowding
                {
                    // Stronger push the closer they are
                    separationForce += away.normalized * (1.5f - dist);
                    count++;
                }
            }
        }
        
        if (count > 0 && !isOnSlope)
        {
            // Manually shove the agent so they slide apart while chasing.
            // Reduced by ~80% for a much more subtle, natural shift rather than an aggressive slide.
            _navAgent.Move(separationForce * Time.deltaTime * 1.2f);
        }
        
        // Close-range Encirclement
        // Only start wrapping around when they get close to the player (e.g., within 6 units)
        // This ensures they always charge forward from far away, but fan out wide when closing in for the kill
        if (!isOnSlope && _distanceToPlayer < 6.0f && _distanceToPlayer > _attackRange * 0.5f)
        {
            Vector3 dirFromPlayer = (transform.position - _playerTransform.position).normalized;
            dirFromPlayer.y = 0;
            
            // Tangent vector for lateral movement (sidestepping around player)
            Vector3 tangent = Vector3.Cross(dirFromPlayer, Vector3.up).normalized;
            
            // RandomAngleOffset determines if they prefer flanking left or right forever
            float strafeDir = Mathf.Sin(_randomAngleOffset) > 0 ? 1f : -1f;
            
            // Move sideways while pathing forward. This balloons out the crowd into a wide half-circle front.
            // Reduced by 80% to be very subtle and look organic.
            _navAgent.Move(tangent * strafeDir * Time.deltaTime * 0.9f);
        }
    }

    private void HandleAttackingState()
    {
        // Continuously face the player while attacking (winds up)
        RotateTowardsPlayer();

        bool canSee = CheckLineOfSight();
        if (!canSee)
        {
            _currentState = State.Searching;
            _searchTimer = _searchDuration;
            _navAgent.SetDestination(_lastSeenPosition);
            SetAgentStopped(false);
            SetWalking(true);
            if (_attackType == AttackType.Exploder)
            {
                _isFuseLit = false;
            }
            return;
        }

        _lastSeenPosition = _playerTransform.position;

        if (_attackType == AttackType.Ranged)
        {
            // Only strafe if the player is at a reasonable distance (more than 4 units away)
            // If they are too close, strafing laterally might cause them to path off stairs or run away weirdly
            if (_enableStrafing && _distanceToPlayer > 4f)
            {
                _navAgent.updateRotation = false; // Manually look at player while moving sideways
                SetAgentStopped(false);
                _navAgent.speed = _strafeSpeed;
                SetWalking(true);
                
                _strafeTimer -= Time.deltaTime;
                if (_strafeTimer <= 0f)
                {
                    ChangeStrafeDirection();
                }
            }
            else
            {
                // Stand ground and shoot when close or when strafing is disabled
                SetAgentStopped(true);
                _navAgent.updateRotation = true;
                SetWalking(false);
            }
        }
        else
        {
            SetAgentStopped(true);
            SetWalking(false);
        }

        // For all attack types, if player is out of range, go back to chasing.
        if (_distanceToPlayer > _attackRange) {
            _currentState = State.Chasing;
            if(_attackType == AttackType.Exploder)
            {
                _isFuseLit = false; // Reset the fuse if the player runs away
            }
            return;
        }
        
        if (_attackType == AttackType.Exploder)
        {
            if (!_isFuseLit)
            {
                _fuseTimer = _explosionFuseTime;
                _isFuseLit = true;
            }
            
            _fuseTimer -= Time.deltaTime;
            if (_fuseTimer <= 0f) Explode();
        }
        else
        {
            if (Time.time >= _nextAttackTime)
            {
                if (_attackType == AttackType.Ranged) PerformRangedAttack();
                else PerformMeleeAttack();
                _nextAttackTime = Time.time + 1f / _attackRate;
            }
        }
    }

    private void ChangeStrafeDirection()
    {
        _strafeTimer = _strafeChangeInterval + Random.Range(-0.5f, 0.5f);
        
        if (_playerTransform == null) return;

        // Choose random direction: 1 (right) or -1 (left)
        int dir = Random.value > 0.5f ? 1 : -1;
        
        // Direction from player to enemy
        Vector3 dirFromPlayer = (transform.position - _playerTransform.position).normalized;
        dirFromPlayer.y = 0; // Keep movement on flat plane
        
        // Find tangent vector for left/right strafe
        Vector3 strafeDir = Vector3.Cross(dirFromPlayer, Vector3.up).normalized * dir;
        
        // Attempt to find a valid navmesh point 4 units in the strafe direction
        Vector3 targetPos = transform.position + strafeDir * 4f;
        
        // Restrict sampling range to 1.5 units and verify elevation is similar to avoid snapping to a floor under the stairs
        if (UnityEngine.AI.NavMesh.SamplePosition(targetPos, out UnityEngine.AI.NavMeshHit hit, 1.5f, UnityEngine.AI.NavMesh.AllAreas))
        {
            if (Mathf.Abs(hit.position.y - transform.position.y) < 1.5f)
            {
                _navAgent.SetDestination(hit.position);
            }
        }
    }

    private void PerformRangedAttack()
    {
        TriggerAnimation(_attackTrigger);
        // Play ranged attack sound
        if (!string.IsNullOrEmpty(_attackSound))
            FMODHelper.PlayOneShot(_attackSound, transform.position);
        if (_firePoints == null || _firePoints.Length == 0) return;
        
        // Fire from all fire points
        foreach (var firePoint in _firePoints)
        {
            if (firePoint == null) continue;
            
            // Calculate direction to player
            Vector3 shootDirection = _playerTransform.position - firePoint.position;
            shootDirection.y = 0;
            shootDirection.Normalize();
            
            // Use shared enemy bullet pool
            if (EnemyBulletPool.Instance != null)
            {
                EnemyBulletPool.Instance.FireBullet(firePoint.position, shootDirection, _bulletSpeed, _rangedDamage, transform);
            }
            else
            {
                // Fallback: instantiate directly if no pool exists
                if (_bulletPrefab != null)
                {
                    GameObject bulletGO = Instantiate(_bulletPrefab, firePoint.position, Quaternion.LookRotation(shootDirection));
                    if (bulletGO.TryGetComponent<Bullet>(out var bullet))
                    {
                        bullet.SetDamage(_rangedDamage);
                        bullet.SetOwner(transform);
                        bullet.Fire(shootDirection, _bulletSpeed);
                    }
                }
            }
            
            // Spawn VFX at this fire point
            SpawnRangedVFX(firePoint);
        }
    }
    
    private void SpawnRangedVFX(Transform firePoint)
    {
        if (_rangedVFXPrefabs == null || firePoint == null) return;
        
        foreach (var prefab in _rangedVFXPrefabs)
        {
            if (prefab == null) continue;
            
            Quaternion correctedRotation = firePoint.rotation * Quaternion.Euler(0, 180, 0);
            GameObject vfxInstance = Instantiate(prefab, firePoint.position, correctedRotation, firePoint);
            
            ParticleSystem rootPS = vfxInstance.GetComponent<ParticleSystem>();
            if (rootPS != null)
            {
                rootPS.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                rootPS.Play(true);
            }
            else
            {
                foreach (var ps in vfxInstance.GetComponentsInChildren<ParticleSystem>())
                {
                    ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                    ps.Play(true);
                }
            }
            
            float maxDuration = 0f;
            foreach (var ps in vfxInstance.GetComponentsInChildren<ParticleSystem>())
            {
                float duration = ps.main.duration + ps.main.startLifetime.constantMax;
                if (duration > maxDuration) maxDuration = duration;
            }
            
            Destroy(vfxInstance, maxDuration + 0.5f);
        }
    }

    private void PerformMeleeAttack()
    {
        TriggerAnimation(_attackTrigger);
        // Play melee attack sound
        if (!string.IsNullOrEmpty(_attackSound))
            FMODHelper.PlayOneShot(_attackSound, transform.position);
            
        // Trigger a short dash/lunge towards the player
        StartCoroutine(MeleeLungeCoroutine());
        StartCoroutine(MeleeHitCoroutine());
    }
    
    private System.Collections.IEnumerator MeleeLungeCoroutine()
    {
        if (_playerTransform == null || !_navAgent.isOnNavMesh) yield break;
        
        // Save original agent settings
        float originalAccel = _navAgent.acceleration;
        float originalSpeed = _navAgent.speed;
        
        float lungeTime = 0.25f; // Slightly longer for a smoother feel
        float maxLungeSpeed = 12f;
        
        SetAgentStopped(false);
        _navAgent.acceleration = 100f; // High acceleration for instant response
        
        float timer = 0f;
        while (timer < lungeTime)
        {
            RotateTowardsPlayer();
            
            // Only lunge if not practically touching the player, preventing clipping
            if (_playerTransform != null && _distanceToPlayer > 1.2f)
            {
                Vector3 dir = (_playerTransform.position - transform.position).normalized;
                
                // Ease-out the speed so it smoothly halts instead of stopping abruptly
                float easeOut = 1f - (timer / lungeTime); 
                if (_navAgent != null && _navAgent.isActiveAndEnabled && _navAgent.isOnNavMesh)
                {
                    _navAgent.velocity = dir * (maxLungeSpeed * easeOut);
                }
            }
            else
            {
                if (_navAgent != null && _navAgent.isActiveAndEnabled && _navAgent.isOnNavMesh)
                {
                    _navAgent.velocity = Vector3.zero;
                }
            }
            
            timer += Time.deltaTime;
            yield return null;
        }
        
        // Restore settings
        if (_navAgent != null && _navAgent.isActiveAndEnabled && _navAgent.isOnNavMesh)
        {
            _navAgent.velocity = Vector3.zero;
            _navAgent.speed = originalSpeed;
            _navAgent.acceleration = originalAccel;
        }
        SetAgentStopped(true);
    }
    
    private System.Collections.IEnumerator MeleeHitCoroutine()
    {
        // Wait for animation to reach hit moment
        yield return new WaitForSeconds(_meleeHitDelay);
        
        // Check if still in range (player might have moved)
        // More forgiving check: 1.5x interaction range so it's harder to step out during 0.3s delay.
        if (_distanceToPlayer <= _attackRange * 1.5f && _playerTransform != null)
        {
            // Set hit direction for ragdoll knockback
            Vector3 hitDir = (_playerTransform.position - transform.position).normalized;
            Player.SetLastHitDirection(hitDir);
            Player.TakeDamage(_meleeDamage);
            SpawnMeleeVFX();
        }
    }

    private void SpawnMeleeVFX()
    {
        if (_meleeVFXPrefabs == null) return;
        
        // Use assigned point, or try player position (where hit lands), or fallback to self
        Transform spawnPoint = _meleeVFXPoint;
        if (spawnPoint == null && _playerTransform != null)
        {
            spawnPoint = _playerTransform; // Spawn VFX at player (hit target)
        }
        if (spawnPoint == null)
        {
            // Last resort: find by tag
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            spawnPoint = playerObj != null ? playerObj.transform : transform;
        }
        
        foreach (var prefab in _meleeVFXPrefabs)
        {
            if (prefab == null) continue;
            
            // Rotate 180 degrees to face correct direction
            Quaternion correctedRotation = spawnPoint.rotation * Quaternion.Euler(0, 180, 0);
            // Spawn in world space (null parent) to avoid scaling issues if enemy is scaled weirdly
            GameObject vfxInstance = Instantiate(prefab, spawnPoint.position, correctedRotation, null);
            
            ParticleSystem rootPS = vfxInstance.GetComponent<ParticleSystem>();
            if (rootPS != null)
            {
                rootPS.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                rootPS.Play(true);
            }
            else
            {
                foreach (var ps in vfxInstance.GetComponentsInChildren<ParticleSystem>())
                {
                    ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                    ps.Play(true);
                }
            }
            
            float maxDuration = 0f;
            foreach (var ps in vfxInstance.GetComponentsInChildren<ParticleSystem>())
            {
                float duration = ps.main.duration + ps.main.startLifetime.constantMax;
                if (duration > maxDuration) maxDuration = duration;
            }
            
            Destroy(vfxInstance, maxDuration + 0.5f);
        }
    }

    private void Explode()
    {
        // Play explosion sound
        if (!string.IsNullOrEmpty(_explosionSound))
            FMODHelper.PlayOneShot(_explosionSound, transform.position);
        if (_explosionVFX != null) Instantiate(_explosionVFX, transform.position, Quaternion.identity);
        
        if(_distanceToPlayer <= _explosionRadius)
        {
             Vector3 hitDir = (_playerTransform.position - transform.position).normalized;
             Player.SetLastHitDirection(hitDir);
             Player.TakeDamage(_explosionDamage);
        }
        
        if(TryGetComponent<EnemyManager>(out var manager))
        {
            manager.TakeDamage(9999);
        }
        else
        {
             Destroy(gameObject);
        }
    }

    private void OnDrawGizmosSelected()
    {
        // Draw the detection radius
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, _detectionRadius);

        // Draw the attack range
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, _attackRange);
    }
    private void HandleSearchingState()
    {
        _navAgent.updateRotation = true;
        _navAgent.speed = _originalSpeed * _speedVariance;

        if (CheckLineOfSight())
        {
            _currentState = State.Chasing;
            return;
        }

        if (!_navAgent.pathPending && _navAgent.remainingDistance <= 1.2f)
        {
            SetAgentStopped(true);
            SetWalking(false);

            _searchTimer -= Time.deltaTime;
            if (_searchTimer <= 0f)
            {
                _currentState = State.ReturningToOrigin;
                SetAgentStopped(false);
                _navAgent.SetDestination(_originalPosition);
            }
        }
        else
        {
            SetAgentStopped(false);
            SetWalking(true);
            if (_navAgent.destination != _lastSeenPosition)
            {
                _navAgent.SetDestination(_lastSeenPosition);
            }
        }
    }

    private void HandleReturningToOriginState()
    {
        _navAgent.updateRotation = true;
        _navAgent.speed = _originalSpeed * 0.8f * _speedVariance;

        if (_distanceToPlayer <= _detectionRadius && CheckLineOfSight())
        {
            _currentState = State.Chasing;
            if (!string.IsNullOrEmpty(_detectionSound))
                FMODHelper.PlayOneShot(_detectionSound, transform.position);
            return;
        }

        if (!_navAgent.pathPending && _navAgent.remainingDistance <= 1.2f)
        {
            SetAgentStopped(true);
            SetWalking(false);
            _currentState = State.Idle;
        }
        else
        {
            SetAgentStopped(false);
            SetWalking(true);
            if (_navAgent.destination != _originalPosition)
            {
                _navAgent.SetDestination(_originalPosition);
            }
        }
    }

    private void TriggerAnimation(string triggerName)
    {
        if (_animator != null && !string.IsNullOrEmpty(triggerName))
        {
            _animator.SetTrigger(triggerName);
        }
    }
    
    private void SetWalking(bool isWalking)
    {
        if (_animator != null && !string.IsNullOrEmpty(_isWalkingBool))
        {
            // Prevent spamming the animator and avoid Unity console logging every frame
            if (_animator.GetBool(_isWalkingBool) != isWalking)
            {
                _animator.SetBool(_isWalkingBool, isWalking);
            }
        }
    }
    
    private void RotateTowardsPlayer()
    {
        if (_playerTransform == null) return;
        Vector3 lookDirection = (_playerTransform.position - transform.position).normalized;
        lookDirection.y = 0;
        if (lookDirection != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(lookDirection);
        }
    }

    private void OnDisable()
    {
        StopAllCoroutines();
    }

    private void SetAgentStopped(bool stopped)
    {
        if (_navAgent != null && _navAgent.isActiveAndEnabled && _navAgent.isOnNavMesh)
        {
            _navAgent.isStopped = stopped;
        }
    }
}
