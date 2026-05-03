using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Lightweight screen flash effect using Update instead of Coroutines.
/// No stacking, no lag, works correctly during Hit Stop.
/// </summary>
public class ScreenFlash : MonoBehaviour
{
    public static ScreenFlash Instance { get; private set; }

    [Header("Dependencies")]
    [SerializeField] private Image _flashImage;

    private float _currentAlpha = 0f;
    private float _fadeSpeed = 0f;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        if (_flashImage != null)
        {
            Color c = _flashImage.color;
            c.a = 0f;
            _flashImage.color = c;
        }
    }

    private void Update()
    {
        if (_flashImage == null || _currentAlpha <= 0f) return;

        // Decay alpha every frame using real time (ignores Hit Stop)
        _currentAlpha -= _fadeSpeed * Time.unscaledDeltaTime;

        if (_currentAlpha <= 0f)
        {
            _currentAlpha = 0f;
        }

        Color c = _flashImage.color;
        c.a = _currentAlpha;
        _flashImage.color = c;
    }

    /// <summary>
    /// Trigger a screen flash. Duration is how long it takes to fully fade out.
    /// maxAlpha is the starting opacity (keep VERY low, e.g. 0.03-0.06).
    /// </summary>
    public void Flash(float duration = 0.03f, float maxAlpha = 0.03f)
    {
        if (_flashImage == null) return;

        _currentAlpha = maxAlpha;
        _fadeSpeed = maxAlpha / Mathf.Max(duration, 0.001f);

        Color c = _flashImage.color;
        c.a = _currentAlpha;
        _flashImage.color = c;
    }
}
