using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class EnemyManager : MonoBehaviour
{
    [SerializeField] private float _maxHealth = 100f;
    
    [Header("Animation")]
    [SerializeField] private Animator _animator;

    [Header("Effects")]
    [SerializeField] private GameObject _bloodSplatPrefab;
    [Tooltip("Optional: Handles dismemberment on death.")]
    [SerializeField] private BodyPartExploder _bodyPartExploder;
    [SerializeField] private Renderer[] _modelRenderers; // Assign all meshes here or let it auto-find

    [Header("FMOD Sounds")]
    [SerializeField] private string _deathSound = "event:/Scarab_Death";

    [Header("UI")]
    [SerializeField] private EnemyHealthBar _healthBar;

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

        if (_healthBar == null) _healthBar = GetComponentInChildren<EnemyHealthBar>();
        if (_healthBar == null)
        {
            CreateAutoHealthBar();
        }

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

        BossController boss = GetComponent<BossController>();
        if (boss != null)
        {
            boss.TakeDamage(amount, hitDirection);
        }

        _currentHealth -= amount;
        _lastHitDirection = hitDirection;

        // Visual Feedback
        if (_hitFlash != null)
        {
            _hitFlash.Flash();
        }
        
        if (_healthBar != null)
        {
            _healthBar.UpdateHealth(_currentHealth, _maxHealth);
        }
        
        // Global screen flash to emphasize hit impact
        if (ScreenFlash.Instance != null)
        {
            ScreenFlash.Instance.Flash();
        }

        Debug.Log($"{gameObject.name} taking {amount} damage. HP = {_currentHealth}");

        if (_currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        _isDead = true;

        // Light slow-motion and heavier screen shake on death
        if (HitStopManager.Instance != null)
        {
            HitStopManager.Instance.TriggerSlowMo(0.5f, 0.3f);
        }
        
        Player.TriggerHeavyCameraShake();
        
        // Play death sound
        if (!string.IsNullOrEmpty(_deathSound))
            FMODHelper.PlayOneShot(_deathSound, transform.position);
        
        // Disable AI and movement
        if (TryGetComponent<EnemyAI>(out var ai)) ai.enabled = false;
        if (TryGetComponent<UnityEngine.AI.NavMeshAgent>(out var nav)) nav.enabled = false;
        
        // Disable ALL colliders so we can't hit dead body
        Collider[] allColliders = GetComponentsInChildren<Collider>();
        foreach (var c in allColliders)
        {
            c.enabled = false;
        }
        
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
            // Use a mask to avoid hitting the enemy itself or other enemies
            int layerMask = LayerMask.GetMask("Default", "Ground", "Terrain");
            
            RaycastHit[] hits = Physics.RaycastAll(spawnPos + Vector3.up * 3f, Vector3.down, 10f, layerMask);
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            bool foundGround = false;
            foreach (var hit in hits)
            {
                // Ignore self, other enemies, and player
                if (hit.collider.GetComponentInParent<EnemyManager>() != null) continue;
                if (hit.collider.GetComponentInParent<Player>() != null) continue;
                // Removed CompareTag("Enemy") check as it was redundant and caused errors if the tag was missing.

                spawnPos = hit.point + Vector3.up * 0.05f; // Slightly above ground
                foundGround = true;
                break;
            }

            if (!foundGround)
            {
                 // Fallback: If over void, just place at feet level
                 spawnPos.y = transform.position.y + 0.05f;
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

        // Trigger the kill marker effect on the health bar canvas
        if (_healthBar != null)
        {
            _healthBar.ShowKillMarker();
        }

        // Handle Visual Death: Either Explode parts or just Hide
        if (_bodyPartExploder != null)
        {
            // Ensure we aren't flashing white when we explode
            if (_hitFlash != null) _hitFlash.RestoreMaterials();
            
            _bodyPartExploder.Explode(_lastHitDirection);
            // Don't disable renderers manually, exploded parts need them!
            // But we do destroy the main object eventually to clean up the empty shell.
            StartCoroutine(DisableAfterDelay(1.6f)); // Increased delay to allow kill marker to finish
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
            StartCoroutine(DisableAfterDelay(1.6f)); // Increased delay to allow kill marker to finish
        }
    }
    
    private System.Collections.IEnumerator DisableAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        Destroy(gameObject);
    }
    
    /// <summary>
    /// Reset enemy for object pooling
    /// </summary>
    public void ResetEnemy()
    {
        _currentHealth = _maxHealth;
        _isDead = false;
    }

    private void CreateAutoHealthBar()
    {
        // 1. Create a WorldSpace Canvas GameObject for the Health Bar
        GameObject canvasGo = new GameObject("EnemyHealthBar_Auto");
        canvasGo.transform.SetParent(transform, false);

        // Position it above the enemy
        float height = 2.0f;
        Collider col = GetComponent<Collider>();
        if (col == null) col = GetComponentInChildren<Collider>();
        if (col != null)
        {
            height = col.bounds.max.y - transform.position.y + 0.2f;
        }
        canvasGo.transform.localPosition = new Vector3(0f, height, 0f);

        Canvas canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        
        RectTransform canvasRect = canvasGo.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(100f, 15f); 
        canvasRect.localScale = new Vector3(0.015f, 0.015f, 0.015f);

        // 2. Add CanvasGroup
        canvasGo.AddComponent<CanvasGroup>();

        // 3. Create Slider
        GameObject sliderGo = new GameObject("Slider");
        sliderGo.transform.SetParent(canvasGo.transform, false);
        
        RectTransform sliderRect = sliderGo.AddComponent<RectTransform>();
        sliderRect.anchorMin = Vector2.zero;
        sliderRect.anchorMax = Vector2.one;
        sliderRect.offsetMin = Vector2.zero;
        sliderRect.offsetMax = Vector2.zero;

        Slider slider = sliderGo.AddComponent<Slider>();

        // Background
        GameObject bgGo = new GameObject("Background");
        bgGo.transform.SetParent(sliderGo.transform, false);
        RectTransform bgRect = bgGo.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;
        Image bgImage = bgGo.AddComponent<Image>();
        bgImage.color = new Color(0.1f, 0.1f, 0.1f, 0.7f); // Dark translucent background

        Outline bgOutline = bgGo.AddComponent<Outline>();
        bgOutline.effectColor = new Color(0f, 0f, 0f, 0.8f);
        bgOutline.effectDistance = new Vector2(1f, 1f);

        // Fill Area
        GameObject fillAreaGo = new GameObject("Fill Area");
        fillAreaGo.transform.SetParent(sliderGo.transform, false);
        RectTransform fillAreaRect = fillAreaGo.AddComponent<RectTransform>();
        fillAreaRect.anchorMin = Vector2.zero;
        fillAreaRect.anchorMax = Vector2.one;
        fillAreaRect.offsetMin = Vector2.zero;
        fillAreaRect.offsetMax = Vector2.zero;

        // Fill
        GameObject fillGo = new GameObject("Fill");
        fillGo.transform.SetParent(fillAreaGo.transform, false);
        RectTransform fillRect = fillGo.AddComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;
        Image fillImage = fillGo.AddComponent<Image>();
        fillImage.color = new Color(0.85f, 0.15f, 0.15f, 0.9f); // Bright red health fill

        slider.targetGraphic = fillImage;
        slider.fillRect = fillRect;
        slider.minValue = 0f;
        slider.maxValue = _maxHealth;
        slider.value = _maxHealth;

        // 4. Create optional Kill Markers
        // Large Cross
        GameObject largeCrossGo = new GameObject("LargeCross");
        largeCrossGo.transform.SetParent(canvasGo.transform, false);
        RectTransform largeCrossRect = largeCrossGo.AddComponent<RectTransform>();
        largeCrossRect.anchorMin = Vector2.zero;
        largeCrossRect.anchorMax = Vector2.one;
        largeCrossRect.offsetMin = Vector2.zero;
        largeCrossRect.offsetMax = Vector2.zero;

        TextMeshProUGUI largeCrossText = largeCrossGo.AddComponent<TextMeshProUGUI>();
        largeCrossText.text = "✕";
        largeCrossText.color = new Color(0.9f, 0.1f, 0.1f, 0.9f);
        largeCrossText.fontSize = 24f;
        largeCrossText.alignment = TextAlignmentOptions.Center;
        largeCrossGo.SetActive(false);

        // Small Cross
        GameObject smallCrossGo = new GameObject("SmallCross");
        smallCrossGo.transform.SetParent(canvasGo.transform, false);
        RectTransform smallCrossRect = smallCrossGo.AddComponent<RectTransform>();
        smallCrossRect.anchorMin = Vector2.zero;
        smallCrossRect.anchorMax = Vector2.one;
        smallCrossRect.offsetMin = Vector2.zero;
        smallCrossRect.offsetMax = Vector2.zero;

        TextMeshProUGUI smallCrossText = smallCrossGo.AddComponent<TextMeshProUGUI>();
        smallCrossText.text = "✕";
        smallCrossText.color = new Color(0.9f, 0.1f, 0.1f, 0.9f);
        smallCrossText.fontSize = 20f;
        smallCrossText.alignment = TextAlignmentOptions.Center;
        smallCrossGo.SetActive(false);

        // 5. Add EnemyHealthBar script
        EnemyHealthBar healthBarComponent = canvasGo.AddComponent<EnemyHealthBar>();
        healthBarComponent.SetCrosses(largeCrossGo, smallCrossGo);

        _healthBar = healthBarComponent;
    }
}
