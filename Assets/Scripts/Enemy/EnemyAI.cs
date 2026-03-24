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

    private enum State { Idle, Chasing, Attacking }
    private State _currentState;
    private NavMeshAgent _navAgent;
    
    private float _distanceToPlayer;
    private float _nextAttackTime;
    private float _fuseTimer;
    private bool _isFuseLit;
    private float _originalSpeed;
    private float _strafeTimer;
    
    // --- DEBUG ---
    private State _previousState;

    void Awake()
    {
        _navAgent = GetComponent<NavMeshAgent>();
        if (_navAgent != null) _originalSpeed = _navAgent.speed;
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if(playerObj != null) _playerTransform = playerObj.transform;
    }

    void Start()
    {
        _currentState = State.Idle;
        _previousState = State.Idle;
        
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
                if(_navAgent.isOnNavMesh) _navAgent.isStopped = true;
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
        }

        // --- BEGIN DEBUG ---
        if (_previousState != _currentState)
        {
            Debug.Log($"{gameObject.name} changed state from {_previousState} to {_currentState}", this);
        }
        // --- END DEBUG ---
    }

    private void HandleIdleState()
    {
        SetWalking(false);
        if (_distanceToPlayer <= _detectionRadius)
        {
            _currentState = State.Chasing;
            // Play detection alert sound
            if (!string.IsNullOrEmpty(_detectionSound))
                FMODHelper.PlayOneShot(_detectionSound, transform.position);
        }
    }

    private void HandleChasingState()
    {
        _navAgent.updateRotation = true;
        _navAgent.speed = _originalSpeed;
        SetWalking(true);
        _navAgent.isStopped = false;
        _navAgent.SetDestination(_playerTransform.position);
        
        // --- DEBUG ---
        if (_navAgent.pathPending)
        {
            Debug.Log($"{gameObject.name} is calculating a path...", this);
        }
        else if (_navAgent.hasPath)
        {
            Debug.Log($"{gameObject.name} is moving along its path. Velocity: {_navAgent.velocity.magnitude}", this);
        }
        else
        {
            Debug.LogWarning($"{gameObject.name} has no path. Is the destination reachable?", this);
        }
        // --- END DEBUG ---
        
        if (_distanceToPlayer <= _attackRange)
        {
            // Rotate to face player ONCE before switching to attack
            RotateTowardsPlayer();
            SetWalking(false);
            _currentState = State.Attacking;
        }
        else if (_distanceToPlayer > _detectionRadius)
        {
            _currentState = State.Idle;
            _navAgent.isStopped = true;
        }
    }

    private void HandleAttackingState()
    {
        if (_attackType == AttackType.Ranged)
        {
            RotateTowardsPlayer();
            
            if (_enableStrafing)
            {
                _navAgent.updateRotation = false; // Manually look at player while moving sideways
                _navAgent.isStopped = false;
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
                _navAgent.isStopped = true;
                SetWalking(false);
            }
        }
        else
        {
            _navAgent.isStopped = true;
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
        
        if (UnityEngine.AI.NavMesh.SamplePosition(targetPos, out UnityEngine.AI.NavMeshHit hit, 4f, UnityEngine.AI.NavMesh.AllAreas))
        {
            _navAgent.SetDestination(hit.position);
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
        StartCoroutine(MeleeHitCoroutine());
    }
    
    private System.Collections.IEnumerator MeleeHitCoroutine()
    {
        // Wait for animation to reach hit moment
        yield return new WaitForSeconds(_meleeHitDelay);
        
        // Check if still in range (player might have moved)
        if (_distanceToPlayer <= _attackRange && _playerTransform != null)
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
            _animator.SetBool(_isWalkingBool, isWalking);
            Debug.Log($"{gameObject.name} SetWalking: {isWalking}", this);
        }
        else
        {
            Debug.LogWarning($"{gameObject.name} SetWalking failed: Animator={_animator}, BoolName={_isWalkingBool}", this);
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
}
