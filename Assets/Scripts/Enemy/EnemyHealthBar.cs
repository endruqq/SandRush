using UnityEngine;
using UnityEngine.UI;

public class EnemyHealthBar : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private Slider _slider;
    [SerializeField] private CanvasGroup _canvasGroup;

    [Header("Settings")]
    [SerializeField] private float _showDuration = 3f;
    [SerializeField] private float _fadeSpeed = 5f;

    [Header("Kill Marker")]
    [SerializeField] private GameObject _largeCross;
    [SerializeField] private GameObject _smallCross;
    [Tooltip("Początkowa wielkość dużego krzyżyka")]
    [SerializeField] private float _largeCrossStartScale = 0.15f; 
    [Tooltip("Końcowa wielkość dużego krzyżyka tuż przed zniknięciem")]
    [SerializeField] private float _largeCrossEndScale = 0.05f;
    [Tooltip("Siła trzęsienia się markerów (w pikselach UI)")]
    [SerializeField] private float _shakeIntensity = 15f;

    private Camera _mainCamera;
    private float _timeSinceLastHit;
    private bool _isVisible;

    private void Awake()
    {
        _mainCamera = Camera.main;
        
        if (_slider == null)
            _slider = GetComponentInChildren<Slider>();

        if (_canvasGroup == null)
            _canvasGroup = GetComponent<CanvasGroup>();

        // Hide initially
        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = 0f;
        }
        else
        {
            gameObject.SetActive(false);
        }

        if (_largeCross != null) _largeCross.SetActive(false);
        if (_smallCross != null) _smallCross.SetActive(false);
        
        _isVisible = false;
        _timeSinceLastHit = _showDuration; // Start fully hidden
    }

    private void LateUpdate()
    {
        // Billboarding - always face camera
        if (_mainCamera != null)
        {
            transform.forward = _mainCamera.transform.forward;
        }

        // Auto-hide logic
        if (_isVisible)
        {
            _timeSinceLastHit += Time.deltaTime;

            if (_timeSinceLastHit >= _showDuration)
            {
                HideHealthBar();
            }
        }

        // Fade out
        if (_canvasGroup != null)
        {
            float targetAlpha = _timeSinceLastHit < _showDuration ? 1f : 0f;
            _canvasGroup.alpha = Mathf.MoveTowards(_canvasGroup.alpha, targetAlpha, _fadeSpeed * Time.deltaTime);
            
            // Optimization: Disable completely if invisible
            if (_canvasGroup.alpha == 0f && gameObject.activeSelf && !_isVisible)
            {
                 // Instead of completely deactivating, we just let it be transparent,
                 // because LateUpdate billboarding is cheap, and we want to avoid GC from activating/deactivating.
                 // Actually it's fine to just stay transparent.
            }
        }
    }

    public void UpdateHealth(float currentHealth, float maxHealth)
    {
        if (_slider != null)
        {
            _slider.maxValue = maxHealth;
            _slider.value = currentHealth;
        }

        ShowHealthBar();
    }

    private void ShowHealthBar()
    {
        _isVisible = true;
        _timeSinceLastHit = 0f;

        if (_canvasGroup == null && !gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }
    }

    private void HideHealthBar()
    {
        _isVisible = false;
        
        if (_canvasGroup == null && gameObject.activeSelf)
        {
            gameObject.SetActive(false);
        }
    }

    public void ShowKillMarker()
    {
        StartCoroutine(KillMarkerRoutine());
    }

    private System.Collections.IEnumerator KillMarkerRoutine()
    {
        _isVisible = false; // Stop auto-hide
        
        // Hide normal health bar
        if (_slider != null) _slider.gameObject.SetActive(false);
        if (_canvasGroup != null) _canvasGroup.alpha = 1f;

        Vector3 largeOriginalPos = _largeCross != null ? _largeCross.transform.localPosition : Vector3.zero;
        Vector3 smallOriginalPos = _smallCross != null ? _smallCross.transform.localPosition : Vector3.zero;

        // 1. DYNAMIC SHRINK: Large cross starts big and quickly shrinks
        if (_largeCross != null)
        {
            _largeCross.SetActive(true);
            if (_smallCross != null) _smallCross.SetActive(false);
            
            float duration = 0.15f;
            float elapsed = 0f;
            Vector3 startScale = Vector3.one * _largeCrossStartScale; 
            Vector3 endScale = Vector3.one * _largeCrossEndScale;   
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                // Ease-out curve for snappy, punchy feeling
                float t = elapsed / duration;
                float easeOut = Mathf.Sin(t * Mathf.PI * 0.5f); 
                _largeCross.transform.localScale = Vector3.Lerp(startScale, endScale, easeOut);
                
                // Shake effect
                _largeCross.transform.localPosition = largeOriginalPos + (Vector3)UnityEngine.Random.insideUnitCircle * _shakeIntensity;

                yield return null;
            }
            
            _largeCross.SetActive(false);
            _largeCross.transform.localScale = Vector3.one; // Reset for object pooling
            _largeCross.transform.localPosition = largeOriginalPos; // Reset position
        }

        // 2. FLICKER & SHAKE: Small cross blinks quickly
        if (_smallCross != null)
        {
            for (int i = 0; i < 3; i++)
            {
                _smallCross.SetActive(true);
                
                float blinkTime = 0.04f;
                float blinkElapsed = 0f;
                while (blinkElapsed < blinkTime)
                {
                     blinkElapsed += Time.deltaTime;
                     // Shake smaller cross slightly less
                     _smallCross.transform.localPosition = smallOriginalPos + (Vector3)UnityEngine.Random.insideUnitCircle * (_shakeIntensity * 0.6f);
                     yield return null;
                }
                
                _smallCross.SetActive(false);
                yield return new WaitForSeconds(0.04f);
            }
            
            _smallCross.transform.localPosition = smallOriginalPos; // Reset position
        }

        // Clean up
        if (_slider != null) _slider.gameObject.SetActive(true); // Restore for pooling
    }
}
