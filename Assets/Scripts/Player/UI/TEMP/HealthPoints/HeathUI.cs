using UnityEngine;
using UnityEngine.UI;

public class HealthUI : MonoBehaviour
{
    [SerializeField] private Slider _healthSlider;
    [SerializeField] private float _maxHealth = 100f;
    [SerializeField] private float _smoothSpeed = 5f;
    
    private float _targetHealth;
    private float _currentDisplayedHealth;

    void Awake()
    {
        _targetHealth = _maxHealth;
        _currentDisplayedHealth = _maxHealth;
        
        if (_healthSlider != null)
        {
            _healthSlider.maxValue = _maxHealth;
            _healthSlider.value = _maxHealth;
        }
    }
    
    void Update()
    {
        // Smoothly animate the health bar
        if (_healthSlider != null && _currentDisplayedHealth != _targetHealth)
        {
            _currentDisplayedHealth = Mathf.MoveTowards(_currentDisplayedHealth, _targetHealth, _smoothSpeed * Time.deltaTime * _maxHealth);
            _healthSlider.value = _currentDisplayedHealth;
        }
    }

    public void UpdateHealth(float currentHealth)
    {
        _targetHealth = Mathf.Clamp(currentHealth, 0, _maxHealth);
    }
    
    /// <summary>
    /// Initialize health bar with specific max health
    /// </summary>
    public void Initialize(float currentHealth, float maxHealth)
    {
        _maxHealth = maxHealth;
        _targetHealth = currentHealth;
        _currentDisplayedHealth = currentHealth;
        
        if (_healthSlider != null)
        {
            _healthSlider.maxValue = maxHealth;
            _healthSlider.value = currentHealth;
        }
    }
    
    /// <summary>
    /// Set health immediately without animation (for initialization)
    /// </summary>
    public void SetHealthImmediate(float health)
    {
        _targetHealth = health;
        _currentDisplayedHealth = health;
        if (_healthSlider != null)
        {
            _healthSlider.value = health;
        }
    }
}
