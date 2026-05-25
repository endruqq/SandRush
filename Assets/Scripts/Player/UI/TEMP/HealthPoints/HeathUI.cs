using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class HealthUI : MonoBehaviour
{
    [SerializeField] private Slider _healthSlider;
    [SerializeField] private float _maxHealth = 100f;
    [SerializeField] private float _smoothSpeed = 5f;

    [Header("Segmented Health Bar Settings")]
    [SerializeField] private bool _useSegments = false;
    [SerializeField] private GameObject _segmentPrefab;
    [SerializeField] private Transform _segmentsContainer;
    [SerializeField] private float _healthPerSegment = 25f;
    [SerializeField] private bool _useFixedSegmentCount = false;
    [SerializeField] private int _fixedSegmentCount = 4;
    
    private float _targetHealth;
    private float _currentDisplayedHealth;
    private float _currentHealthPerSegment;

    private List<GameObject> _instantiatedSegments = new List<GameObject>();
    private Slider[] _segmentSliders;
    private Image[] _segmentImages;

    void Awake()
    {
        _targetHealth = _maxHealth;
        _currentDisplayedHealth = _maxHealth;
        
        if (_healthSlider != null)
        {
            _healthSlider.maxValue = _maxHealth;
            _healthSlider.value = _maxHealth;
        }

        if (_useSegments)
        {
            SetupSegments();
            UpdateSegmentsVisuals(_currentDisplayedHealth);
        }
    }
    
    void Update()
    {
        // Smoothly animate the health bar
        if (_currentDisplayedHealth != _targetHealth)
        {
            _currentDisplayedHealth = Mathf.MoveTowards(_currentDisplayedHealth, _targetHealth, _smoothSpeed * Time.deltaTime * _maxHealth);
            
            if (_healthSlider != null)
            {
                _healthSlider.value = _currentDisplayedHealth;
            }

            if (_useSegments)
            {
                UpdateSegmentsVisuals(_currentDisplayedHealth);
            }
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

        if (_useSegments)
        {
            SetupSegments();
            UpdateSegmentsVisuals(_currentDisplayedHealth);
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

        if (_useSegments)
        {
            UpdateSegmentsVisuals(health);
        }
    }

    private void SetupSegments()
    {
        // Clear existing segments
        foreach (var seg in _instantiatedSegments)
        {
            if (seg != null)
            {
                if (Application.isPlaying)
                    Destroy(seg);
                else
                    DestroyImmediate(seg);
            }
        }
        _instantiatedSegments.Clear();

        if (!_useSegments || _segmentPrefab == null || _segmentsContainer == null)
            return;

        int numSegments = 0;
        if (_useFixedSegmentCount)
        {
            numSegments = Mathf.Max(1, _fixedSegmentCount);
            _currentHealthPerSegment = _maxHealth / numSegments;
        }
        else
        {
            _currentHealthPerSegment = _healthPerSegment;
            numSegments = Mathf.CeilToInt(_maxHealth / _currentHealthPerSegment);
        }

        _segmentSliders = new Slider[numSegments];
        _segmentImages = new Image[numSegments];

        for (int i = 0; i < numSegments; i++)
        {
            GameObject segGo = Instantiate(_segmentPrefab, _segmentsContainer);
            _instantiatedSegments.Add(segGo);

            // Try to find a Slider component
            Slider slider = segGo.GetComponent<Slider>();
            if (slider == null) slider = segGo.GetComponentInChildren<Slider>();
            _segmentSliders[i] = slider;

            if (slider != null)
            {
                slider.minValue = 0f;
                slider.maxValue = _currentHealthPerSegment;
                slider.value = _currentHealthPerSegment;
            }
            else
            {
                // Try to find a Filled Image component
                Image[] images = segGo.GetComponentsInChildren<Image>();
                Image filledImage = null;
                foreach (var img in images)
                {
                    if (img.type == Image.Type.Filled)
                    {
                        filledImage = img;
                        break;
                    }
                }
                // Fallback to first image found if none are marked Filled
                if (filledImage == null && images.Length > 0)
                {
                    filledImage = images[0];
                }
                _segmentImages[i] = filledImage;

                if (filledImage != null)
                {
                    filledImage.fillAmount = 1f;
                }
            }
        }
    }

    private void UpdateSegmentsVisuals(float displayedHealth)
    {
        if (_segmentSliders == null || _segmentImages == null || _currentHealthPerSegment <= 0f)
            return;

        int numSegments = _segmentSliders.Length;
        for (int i = 0; i < numSegments; i++)
        {
            float segmentHealth = Mathf.Clamp(displayedHealth - (i * _currentHealthPerSegment), 0f, _currentHealthPerSegment);

            if (_segmentSliders[i] != null)
            {
                _segmentSliders[i].value = segmentHealth;
            }
            else if (_segmentImages[i] != null)
            {
                _segmentImages[i].fillAmount = segmentHealth / _currentHealthPerSegment;
            }
        }
    }
}

