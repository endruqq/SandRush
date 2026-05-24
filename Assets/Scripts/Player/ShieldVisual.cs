using System.Collections;
using UnityEngine;

public class ShieldVisual : MonoBehaviour
{
    [Header("Scale Animation Settings")]
    [SerializeField] private float _animationDuration = 0.5f;
    [SerializeField] private float _targetScale = 3f;
    [SerializeField] private float _springDamping = 8.0f; // c parameter in exp(-c*t)
    [SerializeField] private float _springFrequency = 15.0f; // w parameter in cos(w*t)

    [Header("Rotation Settings")]
    [SerializeField] private Vector3 _rotationSpeed = new Vector3(5f, 10f, 3f); // Degrees per second

    private Vector3 _originalLocalScale;
    private Coroutine _scaleCoroutine;

    private void Awake()
    {
        // Store original scale as a baseline
        _originalLocalScale = Vector3.one * _targetScale;
    }

    private void OnEnable()
    {
        // Reset scale to 0 and play the spring pop-in animation
        transform.localScale = Vector3.zero;
        
        if (_scaleCoroutine != null)
        {
            StopCoroutine(_scaleCoroutine);
        }
        _scaleCoroutine = StartCoroutine(AnimateShieldPopIn());
    }

    private void OnDisable()
    {
        if (_scaleCoroutine != null)
        {
            StopCoroutine(_scaleCoroutine);
            _scaleCoroutine = null;
        }
    }

    private void Update()
    {
        // Continuous slow rotation to make hexagons drift organically in 3D space
        transform.Rotate(_rotationSpeed * Time.deltaTime);
    }

    private IEnumerator AnimateShieldPopIn()
    {
        float elapsedTime = 0f;

        while (elapsedTime < _animationDuration)
        {
            elapsedTime += Time.deltaTime;
            
            // Normalize time
            float t = elapsedTime / _animationDuration;
            
            // Elastic spring calculation: starts at 0, overshoots slightly, and settles at 1
            // Formula: 1 - exp(-damp * t) * cos(freq * t)
            float tSpring = 1f - Mathf.Exp(-_springDamping * t) * Mathf.Cos(_springFrequency * t);
            
            transform.localScale = _originalLocalScale * Mathf.Max(0f, tSpring);
            
            yield return null;
        }

        // Ensure we settle precisely at the target scale
        transform.localScale = _originalLocalScale;
        _scaleCoroutine = null;
    }
}
