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

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
    }

    private void OnEnable()
    {
        _lastPosition = transform.position;
        _hasHit = false;
    }

    public void Init(ObjectPool<Bullet> pool)
    {
        _pool = pool;
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
        if (owner != null) _ownerTag = owner.tag;
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
            // Use SphereCastAll to get ALL hits, then pick the first valid one
            // This prevents the bullet from stopping on the Shooter's collider
            RaycastHit[] hits = Physics.SphereCastAll(_lastPosition, 0.1f, direction.normalized, distance, _hitLayers);
            
            // Sort by distance to process closest hits first
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            
            foreach (RaycastHit hit in hits)
            {
                if (hit.collider.gameObject == gameObject) continue; // Ignore self

                // Check if we hit the owner
                if (IsOwner(hit.collider)) continue; // Ignore owner and KEEO GOING
                
                // If we got here, it's a valid hit (wall, enemy, another player)
                _hasHit = true;
                HandleHit(hit.collider, hit.point, hit.normal);
                transform.position = hit.point;
                return; // Stop processing after first valid hit
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
        
        // Log valid hit
        Debug.Log($"Bullet HIT VALID: {hitName} (Tag: {hitTag})");

        EnemyManager enemy = other.GetComponentInParent<EnemyManager>();
        bool isPlayer = other.CompareTag("Player") || other.GetComponentInParent<Player>() != null;

        if (enemy != null)
        {
            Debug.Log($"Dealing {damage} damage to ENEMY: {enemy.name}");
            enemy.TakeDamage(damage);
        }
        else if (isPlayer)
        {
            Debug.Log($"Dealing {damage} damage to PLAYER");
            Player.TakeDamage((int)damage);
        }

        if (_hitEffectPrefab != null)
        {
            Quaternion rot = Quaternion.LookRotation(normal);
            GameObject hitFx = Instantiate(_hitEffectPrefab, point, rot);
            Destroy(hitFx, 1f);
        }

        Deactivate();
    }

    private void Deactivate()
    {
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
