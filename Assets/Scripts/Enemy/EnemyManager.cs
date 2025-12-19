using UnityEngine;

public class EnemyManager : MonoBehaviour
{
    [SerializeField] private float _maxHealth = 100f;

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
        
        if (_mySpawner != null)
        {
            _mySpawner.OnEnemyDied(this);
        }

        Debug.Log($"{gameObject.name} is dead!");

        gameObject.SetActive(false);
    }
}
