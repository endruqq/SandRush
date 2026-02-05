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
    [SerializeField] private Transform _firePoint;
    [SerializeField] private float _bulletSpeed = 20f;
    
    [Header("Melee Settings")]
    [SerializeField] private int _meleeDamage = 10;
    [SerializeField] private Transform _meleeVFXPoint;
    [SerializeField] private GameObject[] _meleeVFXPrefabs;
    
    [Header("Exploder Settings")]
    [SerializeField] private int _explosionDamage = 50;
    [SerializeField] private float _explosionRadius = 5f;
    [SerializeField] private float _explosionFuseTime = 1.5f;
    [SerializeField] private GameObject _explosionVFX;
    
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
    
    // --- DEBUG ---
    private State _previousState;

    void Awake()
    {
        _navAgent = GetComponent<NavMeshAgent>();
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if(playerObj != null) _playerTransform = playerObj.transform;
    }

    void Start()
    {
        _currentState = State.Idle;
        _previousState = State.Idle;
        if (!_navAgent.isOnNavMesh)
        {
            Debug.LogError($"{gameObject.name} is not on a NavMesh! Please check its position.", this);
        }
    }

    void Update()
    {
        if (_playerTransform == null)
        {
            if(_navAgent.isOnNavMesh) _navAgent.isStopped = true;
            return;
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
        if (_distanceToPlayer <= _detectionRadius) _currentState = State.Chasing;
    }

    private void HandleChasingState()
    {
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
        _navAgent.isStopped = true;
        Vector3 lookDirection = (_playerTransform.position - transform.position).normalized;
        lookDirection.y = 0;
        transform.rotation = Quaternion.LookRotation(lookDirection);

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

    private void PerformRangedAttack()
    {
        TriggerAnimation(_attackTrigger);
        if (_bulletPrefab == null || _firePoint == null) return;
        GameObject bulletGO = Instantiate(_bulletPrefab, _firePoint.position, _firePoint.rotation);
        if (bulletGO.TryGetComponent<Rigidbody>(out var rb))
        {
             Vector3 shootDirection = _playerTransform.position - _firePoint.position;
             shootDirection.y = 0;
             rb.linearVelocity = shootDirection.normalized * _bulletSpeed;
        }
    }

    private void PerformMeleeAttack()
    {
        TriggerAnimation(_attackTrigger);
        if (_distanceToPlayer <= _attackRange)
        {
            _playerTransform.SendMessage("TakeDamage", _meleeDamage, SendMessageOptions.DontRequireReceiver);
            SpawnMeleeVFX();
        }
    }

    private void SpawnMeleeVFX()
    {
        if (_meleeVFXPrefabs == null) return;
        
        Transform spawnPoint = _meleeVFXPoint != null ? _meleeVFXPoint : transform;
        
        foreach (var prefab in _meleeVFXPrefabs)
        {
            if (prefab == null) continue;
            
            // Rotate 180 degrees to face correct direction
            Quaternion correctedRotation = spawnPoint.rotation * Quaternion.Euler(0, 180, 0);
            GameObject vfxInstance = Instantiate(prefab, spawnPoint.position, correctedRotation, spawnPoint);
            
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
        if (_explosionVFX != null) Instantiate(_explosionVFX, transform.position, Quaternion.identity);
        
        if(_distanceToPlayer <= _explosionRadius)
        {
             _playerTransform.SendMessage("TakeDamage", _explosionDamage, SendMessageOptions.DontRequireReceiver);
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
        }
    }
}
