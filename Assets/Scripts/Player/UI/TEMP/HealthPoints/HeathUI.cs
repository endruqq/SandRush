using UnityEngine;
using UnityEngine.UI;

public class HealthUI : MonoBehaviour
{
    [SerializeField] private Slider _healthSlider;
    [SerializeField] private float _maxHealth = 100f;
    [SerializeField] private float _smoothSpeed = 5f;
    
    private static HealthUI _instance;
    private float _targetHealth;
    private float _currentDisplayedHealth;

    void Awake()
    {
        _instance = this;
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

    public static void UpdateHealth(int currentHealth)
    {
        if (_instance == null) return;
        
        _instance._targetHealth = Mathf.Clamp(currentHealth, 0, _instance._maxHealth);
    }
    
    /// <summary>
    /// Initialize health bar with specific max health
    /// </summary>
    public static void Initialize(int currentHealth, int maxHealth)
    {
        if (_instance == null) return;

        _instance._maxHealth = maxHealth;
        _instance._targetHealth = currentHealth;
        _instance._currentDisplayedHealth = currentHealth;
        
        if (_instance._healthSlider != null)
        {
            _instance._healthSlider.maxValue = maxHealth;
            _instance._healthSlider.value = currentHealth;
        }
    }
    
    /// <summary>
    /// Set health immediately without animation (for initialization)
    /// </summary>
    public static void SetHealthImmediate(int health)
    {
        if (_instance == null) return;
        
        _instance._targetHealth = health;
        _instance._currentDisplayedHealth = health;
        if (_instance._healthSlider != null)
        {
            _instance._healthSlider.value = health;
        }
    }
}
