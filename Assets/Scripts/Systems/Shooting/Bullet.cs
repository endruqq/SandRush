using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Bullet : MonoBehaviour
{
    [Header("Bullet Stats")]
    [SerializeField] private float damage = 25f;
    [SerializeField] private GameObject _hitEffectPrefab;
    [SerializeField] private TrailRenderer _trail;

    private Rigidbody _rb;
    private ObjectPool<Bullet> _pool;
    private readonly float _lifetime = 3f;
    private float _timer;

    private Vector3 _lastPosition;
    [SerializeField] private LayerMask _hitLayers = -1; // Default to Everything
    private bool _hasHit = false;
    private float _baseDamage;

    private static readonly RaycastHit[] _raycastHits = new RaycastHit[16];

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _baseDamage = damage;
    }

    private void OnEnable()
    {
        _lastPosition = transform.position;
        _hasHit = false;
        damage = _baseDamage;
    }

    private ObjectPool<Transform> _hitEffectPool;

    public GameObject HitEffectPrefab => _hitEffectPrefab;

    public void Init(ObjectPool<Bullet> pool, ObjectPool<Transform> hitEffectPool)
    {
        _pool = pool;
        _hitEffectPool = hitEffectPool;
    }

    public void Fire(Vector3 dir, float speed)
    {
        _timer = _lifetime;
        _lastPosition = transform.position;
        
        if(_rb != null) 
        {
            _rb.linearVelocity = dir * speed;
        }

        if (_trail != null)
        {
            _trail.Clear(); // Clear old trail from previous use
            _trail.emitting = true;
        }
    }
    
    /// <summary>
    /// Set damage dynamically (for pool reuse with different damage values)
    /// </summary>
    public void SetDamage(float newDamage)
    {
        damage = newDamage;
    }



    private string _ownerTag;
    private Transform _owner; // The actual shooter transform

    /// <summary>
    /// Set the owner of the bullet to prevent self-damage.
    /// Supports both Tag and Transform check for redundancy.
    /// </summary>
    public void SetOwner(Transform owner)
    {
        _owner = owner;
        if (owner != null)
        {
            _ownerTag = owner.tag;
            if (owner.CompareTag("Player") || owner.GetComponent<Player>() != null)
            {
                if (Player.Instance != null)
                {
                    damage = _baseDamage + Player.Instance.DamageBonus;
                }
            }
        }
    }
    
    // Legacy overload for compatibility if needed, but prefer Transform
    public void SetOwner(string tag)
    {
        _ownerTag = tag;
    }

    void Update()
    {
        // Skip if already hit something
        if (_hasHit) return;
        
        _timer -= Time.deltaTime;
        if (_timer <= 0)
        {
            Deactivate();
            return;
        }

        Vector3 currentPosition = transform.position;
        Vector3 direction = currentPosition - _lastPosition;
        float distance = direction.magnitude;

        if (distance > 0)
        {
            // Use SphereCastNonAlloc to avoid garbage collector allocations (GC pressure)
            int hitCount = Physics.SphereCastNonAlloc(_lastPosition, 0.1f, direction.normalized, _raycastHits, distance, _hitLayers);
            
            RaycastHit closestHit = default;
            float closestDistance = float.MaxValue;
            bool foundValidHit = false;

            for (int i = 0; i < hitCount; i++)
            {
                RaycastHit hit = _raycastHits[i];
                if (hit.collider.gameObject == gameObject) continue; // Ignore self

                // Check if we hit the owner
                if (IsOwner(hit.collider)) continue; // Ignore owner and KEEP GOING
                
                if (hit.distance < closestDistance)
                {
                    closestDistance = hit.distance;
                    closestHit = hit;
                    foundValidHit = true;
                }
            }

            if (foundValidHit)
            {
                _hasHit = true;
                HandleHit(closestHit.collider, closestHit.point, closestHit.normal);
                transform.position = closestHit.point;
                return; // Stop processing after finding the closest valid hit
            }
        }

        _lastPosition = currentPosition;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (_hasHit) return;
        if (IsOwner(collision.collider)) return; // Ignore owner collision

        HandleHit(collision.collider, collision.contacts[0].point, collision.contacts[0].normal);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_hasHit) return;
        if (other.isTrigger) return;
        if (IsOwner(other)) return; // Ignore owner trigger
        
        HandleHit(other, transform.position, -transform.forward);
    }
    
    private bool IsOwner(Collider other)
    {
        if (_owner != null)
        {
            // Check if it's the owner or part of the owner hierarchy
            if (other.transform == _owner || other.transform.IsChildOf(_owner)) return true;
        }
        
        // Fallback to tag check
        if (!string.IsNullOrEmpty(_ownerTag) && _ownerTag != "Untagged")
        {
            if (other.CompareTag(_ownerTag) || other.transform.root.CompareTag(_ownerTag)) return true;
        }
        
        return false;
    }

    private void HandleHit(Collider other, Vector3 point, Vector3 normal)
    {
        if (_hasHit && other != null) 
        {
            // Already handled in Update loop, but double check flag logic
             // If called from Trigger/Collision, we set flag now
        }
        _hasHit = true; 
        
        string hitTag = other.tag;
        string hitName = other.name;

        EnemyManager enemy = other.GetComponentInParent<EnemyManager>();
        bool isPlayer = other.CompareTag("Player") || other.GetComponentInParent<Player>() != null;

        if (enemy != null)
        {
            // Pass the bullet's current forward direction as the hit direction
            enemy.TakeDamage(damage, transform.forward);
        }
        else if (isPlayer)
        {
            Player.TakeDamage((int)damage);
        }
        else
        {
            // Generic damage call for objects like ExplosiveBarrel
            other.SendMessageUpwards("TakeDamage", damage, SendMessageOptions.DontRequireReceiver);
        }

        if (_hitEffectPool != null)
        {
            Transform hitFx = _hitEffectPool.GetObject();
            hitFx.SetPositionAndRotation(point, Quaternion.LookRotation(normal));
            
            var ps = hitFx.GetComponent<ParticleSystem>();
            if (ps != null) ps.Play(true);
            else
            {
                foreach (var childPs in hitFx.GetComponentsInChildren<ParticleSystem>())
                {
                    childPs.Play(true);
                }
            }

            PoolObjectCleanup cleanup = hitFx.GetComponent<PoolObjectCleanup>();
            if (cleanup == null)
            {
                cleanup = hitFx.gameObject.AddComponent<PoolObjectCleanup>();
                cleanup.Init(_hitEffectPool, 1f);
            }
            else
            {
                cleanup.ResetTimer();
            }
        }
        else if (_hitEffectPrefab != null)
        {
            Quaternion rot = Quaternion.LookRotation(normal);
            GameObject hitFx = Instantiate(_hitEffectPrefab, point, rot);
            Destroy(hitFx, 1f);
        }

        Deactivate();
    }

    private void Deactivate()
    {
        if (_trail != null)
        {
            _trail.emitting = false;
            _trail.Clear();
        }

        if (_rb != null)
        {
            _rb.linearVelocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
        }
        
        if (_pool != null)
        {
            _pool.ReturnObject(this);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}

public class PoolObjectCleanup : MonoBehaviour
{
    private ObjectPool<Transform> _pool;
    private float _lifetime;
    private float _timer;

    public void Init(ObjectPool<Transform> pool, float lifetime)
    {
        _pool = pool;
        _lifetime = lifetime;
        _timer = lifetime;
    }

    private void Update()
    {
        _timer -= Time.deltaTime;
        if (_timer <= 0)
        {
            if (_pool != null)
            {
                _pool.ReturnObject(transform);
            }
            else
            {
                Destroy(gameObject);
            }
            enabled = false;
        }
    }

    public void ResetTimer()
    {
        _timer = _lifetime;
        enabled = true;
    }
}
