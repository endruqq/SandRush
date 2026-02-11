using UnityEngine;

public class EnemyManager : MonoBehaviour
{
    [SerializeField] private float _maxHealth = 100f;
    
    [Header("Animation")]
    [SerializeField] private Animator _animator;
    [SerializeField] private string _deathTrigger = "Death";
    [SerializeField] private float _deathAnimationDuration = 1f;

    [Header("Effects")]
    [SerializeField] private GameObject _bloodSplatPrefab;
    [Tooltip("Optional: Handles dismemberment on death.")]
    [SerializeField] private BodyPartExploder _bodyPartExploder;
    [SerializeField] private Renderer[] _modelRenderers; // Assign all meshes here or let it auto-find

    [Header("FMOD Sounds")]
    [SerializeField] private string _deathSound = "event:/Scarab_Death";

    private float _currentHealth;
    private bool _isDead;
    private EnemySpawner _mySpawner;
    private HitFlash _hitFlash;
    private Vector3 _lastHitDirection;

    void Awake()
    {
        _currentHealth = _maxHealth;
        _hitFlash = GetComponent<HitFlash>();
        if (_bodyPartExploder == null) _bodyPartExploder = GetComponent<BodyPartExploder>();

        if (_modelRenderers == null || _modelRenderers.Length == 0)
        {
            _modelRenderers = GetComponentsInChildren<Renderer>();
        }
    }

    public void Initialize(EnemySpawner spawner)
    {
        _mySpawner = spawner;
    }

    public void TakeDamage(float amount)
    {
        TakeDamage(amount, Vector3.zero);
    }

    public void TakeDamage(float amount, Vector3 hitDirection)
    {
        if (_isDead) return;

        _currentHealth -= amount;
        _lastHitDirection = hitDirection;

        // Visual Feedback
        if (_hitFlash != null)
        {
            _hitFlash.Flash();
        }

        Debug.Log($"{gameObject.name} taking {amount} damage. HP = {_currentHealth}");

        if (_currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        Player.GetUltimate(1);

        _isDead = true;
        
        // Play death sound
        if (!string.IsNullOrEmpty(_deathSound))
            FMODHelper.PlayOneShot(_deathSound, transform.position);
        
        // Disable AI and movement
        if (TryGetComponent<EnemyAI>(out var ai)) ai.enabled = false;
        if (TryGetComponent<UnityEngine.AI.NavMeshAgent>(out var nav)) nav.enabled = false;
        if (TryGetComponent<Collider>(out var col)) col.enabled = false; // Disable collider so we can't hit dead body
        
        // Spawn Blood
        if (_bloodSplatPrefab != null)
        {
            Vector3 spawnPos = transform.position;
            
            // Calculate position "behind" the enemy based on hit direction
            // If direction is zero (unknown), just use center
            if (_lastHitDirection != Vector3.zero)
            {
                // Push blood 0.8f units behind the enemy relative to shot
                spawnPos += _lastHitDirection.normalized * 0.8f;
            }

            // Raycast down to find ground for perfect placement
            if (Physics.Raycast(spawnPos + Vector3.up * 2f, Vector3.down, out RaycastHit hit, 5f))
            {
                spawnPos = hit.point + Vector3.up * 0.01f; // Slightly above ground
            }
            else
            {
                 // Fallback if raycast fails (e.g. over void), just reset to transform y
                 spawnPos.y = transform.position.y + 0.01f;
            }
            // Random rotation for variety ? NO, we want directional.
            // Texture moves "UP" (Y+). We want UP to point in HitDirection on the ground.
            // Quad flat on ground: forward is Z. We need to align Quad's "Up" (texture Y) with hitDirection.
            // If Quad has X-90 rotation: Local Y is World "Forward" (Z-like), Local Z is World Up.
            // Wait, standard Quad: Z is Normal facing camera.
            // If use SpriteRenderer on X-90 object: Local Up is World Forward.
            // Let's assume the user followed instructions and rotated prefab X-90.
            // Then Local Y is World Forward (on ground plane).
            // So we just need to LookRotation(hitDirection) but that aligns Z...
            // Actually simpler:
            // LookRotation(forward, up).
            // If we want the object's 'Up' vector (Texture Y) to point in 'hitDirection':
            // Quaternion.LookRotation(Vector3.up, hitDirection) ? No.
            
            // Let's use standard LookRotation to point the object's Z axis.
            // Then we rotate 90 degrees if needed.
            // If prefab is SpriteRenderer rotated 90 on X:
            // Its "Up" (Texture Y) becomes World Forward (Z).
            // So Quaternion.LookRotation(hitDirection) should align Z (Forward) with HitDirection.
            // And since Sprite Up = World Forward in that setup, it should work perfect.
            
            // First calculate the flat direction rotation (Y-axis only)
            Quaternion lookRotation = Quaternion.LookRotation(_lastHitDirection.normalized);
            
            // Add slight randomness to the angle around the Y axis
            float randomAngle = Random.Range(-15f, 15f);
            lookRotation *= Quaternion.Euler(0, randomAngle, 0); 
            
            // NOW apply the 90-degree tilt to lay it flat on the ground (X-axis)
            Quaternion finalRotation = lookRotation * Quaternion.Euler(90, 0, 0);
            
            // Instantiate and Auto-Destroy
            GameObject blood = Instantiate(_bloodSplatPrefab, spawnPos, finalRotation);
            Destroy(blood, 7f);
        }

        if (_mySpawner != null)
        {
            _mySpawner.OnEnemyDied(this);
        }

        Debug.Log($"{gameObject.name} is dead!");

        // Handle Visual Death: Either Explode parts or just Hide
        if (_bodyPartExploder != null)
        {
            // Ensure we aren't flashing white when we explode
            if (_hitFlash != null) _hitFlash.RestoreMaterials();
            
            _bodyPartExploder.Explode(_lastHitDirection);
            // Don't disable renderers manually, exploded parts need them!
            // But we do destroy the main object eventually to clean up the empty shell.
            StartCoroutine(DisableAfterDelay(0.1f)); 
        }
        else
        {
            // Fallback: simple hide
            if (_modelRenderers != null)
            {
                foreach (var r in _modelRenderers)
                {
                    if (r != null) r.enabled = false;
                }
            }
            StartCoroutine(DisableAfterDelay(0.1f));
        }
    }
    
    private System.Collections.IEnumerator DisableAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        gameObject.SetActive(false);
    }
    
    /// <summary>
    /// Reset enemy for object pooling
    /// </summary>
    public void ResetEnemy()
    {
        _currentHealth = _maxHealth;
        _isDead = false;
    }
}
