using UnityEngine;

public class EnemyManager : MonoBehaviour
{
    [SerializeField] private float _maxHealth = 100f;
    
    [Header("Animation")]
    [SerializeField] private Animator _animator;
    [SerializeField] private string _deathTrigger = "Death";
    [SerializeField] private float _deathAnimationDuration = 1f;

    private float _currentHealth;
    private bool _isDead;
    private EnemySpawner _mySpawner;

    void Awake()
    {
        _currentHealth = _maxHealth;
    }

    public void Initialize(EnemySpawner spawner)
    {
        _mySpawner = spawner;
    }

    public void TakeDamage(float amount)
    {
        if (_isDead) return;

        _currentHealth -= amount;

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
        
        // Disable AI and movement
        if (TryGetComponent<EnemyAI>(out var ai)) ai.enabled = false;
        if (TryGetComponent<UnityEngine.AI.NavMeshAgent>(out var nav)) nav.enabled = false;
        
        // Play death animation
        if (_animator != null && !string.IsNullOrEmpty(_deathTrigger))
        {
            _animator.SetTrigger(_deathTrigger);
        }
        
        if (_mySpawner != null)
        {
            _mySpawner.OnEnemyDied(this);
        }

        Debug.Log($"{gameObject.name} is dead!");

        // Delay before disabling to allow animation to play
        StartCoroutine(DisableAfterDelay(_deathAnimationDuration));
    }
    
    private System.Collections.IEnumerator DisableAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        gameObject.SetActive(false);
    }
}
