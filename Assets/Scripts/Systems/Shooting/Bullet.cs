using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Bullet : MonoBehaviour
{
    [Header("Bullet Stats")]
    [SerializeField] private float damage = 25f;
    [SerializeField] private GameObject _hitEffectPrefab;

    private Rigidbody _rb;
    private ObjectPool<Bullet> _pool;
    private readonly float _lifetime = 3f;
    private float _timer;

    private Vector3 _lastPosition;
    [SerializeField] private LayerMask _hitLayers = -1; // Default to Everything

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
    }

    private void OnEnable()
    {
        _lastPosition = transform.position;
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
    }

    void Update()
    {
        _timer -= Time.deltaTime;
        if (_timer <= 0)
        {
            Deactivate();
            return;
        }

        // Raycast movement to prevent tunneling (passing through objects)
        Vector3 currentPosition = transform.position;
        Vector3 direction = currentPosition - _lastPosition;
        float distance = direction.magnitude;

        if (distance > 0)
        {
            // SphereCast gives it some "thickness" so you don't have to be pixel perfect
            if (Physics.SphereCast(_lastPosition, 0.1f, direction.normalized, out RaycastHit hit, distance, _hitLayers))
            {
                 // Check if we hit ourselves (just in case) due to slight overlap
                 if (hit.collider.gameObject != gameObject)
                 {
                     HandleHit(hit.collider, hit.point, hit.normal);
                     // Position the visual bulet at the hit point
                     transform.position = hit.point; 
                     return; // HandleHit calls Deactivate
                 }
            }
        }

        _lastPosition = currentPosition;
    }

    // Keep these as backup if Raycast misses (e.g. spawns inside)
    private void OnCollisionEnter(Collision collision)
    {
        HandleHit(collision.collider, collision.contacts[0].point, collision.contacts[0].normal);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.isTrigger) return;
        HandleHit(other, transform.position, -transform.forward);
    }

    private void HandleHit(Collider other, Vector3 point, Vector3 normal)
    {
        Debug.Log($"Bullet HIT: {other.name}");

        EnemyManager enemy = other.GetComponentInParent<EnemyManager>();
        
        if (enemy != null)
        {
            enemy.TakeDamage(damage);
        }
        else
        {
            other.SendMessage("TakeDamage", damage, SendMessageOptions.DontRequireReceiver);
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
        _rb.linearVelocity = Vector3.zero;
        _rb.angularVelocity = Vector3.zero;
        
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
