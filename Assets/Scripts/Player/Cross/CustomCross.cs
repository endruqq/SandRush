using UnityEngine;
using UnityEngine.UI;

public class CursorCross : MonoBehaviour
{
    [SerializeField] private RectTransform _cursorImage;
    [SerializeField] private bool _turnOnCursor;

    [Header("Shoot Feedback")]
    [Tooltip("How much the crosshair scales up on each shot (additive)")]
    [SerializeField] private float _shootScalePunch = 0.15f;
    [Tooltip("How fast the scale returns to normal")]
    [SerializeField] private float _scaleRecoverSpeed = 5f;

    [Header("Reload Spin")]
    [Tooltip("Full 360° rotation happens over the reload duration")]
    [SerializeField] private float _reloadSpinDegrees = 360f;
    [Tooltip("Spin easing - higher = snappier start/end")]
    [SerializeField] private float _spinSmoothing = 3f;

    // Runtime state
    private float _currentScale = 1f;
    private float _targetScale = 1f;
    private float _baseScale = 1f;

    private bool _isReloading;
    private float _reloadProgress; // 0 → 1
    private float _reloadDuration;
    private float _reloadTimer;
    private float _currentRotation;
    private float _targetRotation;

    void Start()
    {
        if (_turnOnCursor)
        {
            Cursor.visible = false;
        }
        else
        {
            Cursor.visible = true;
        }

        if (_cursorImage != null)
        {
            _baseScale = _cursorImage.localScale.x;
            _currentScale = _baseScale;
            _targetScale = _baseScale;

            // Disable raycastTarget so the crosshair doesn't block UI raycasts
            Image img = _cursorImage.GetComponent<Image>();
            if (img != null) img.raycastTarget = false;
            // Also disable on all children
            foreach (var childImg in _cursorImage.GetComponentsInChildren<Image>())
                childImg.raycastTarget = false;
        }
    }

    void Update()
    {
        if (!_turnOnCursor || _cursorImage == null) return;

        bool isUIMode = Player.IsUIModeActive || Time.timeScale == 0f;

        if (isUIMode)
        {
            if (_cursorImage.gameObject.activeSelf) _cursorImage.gameObject.SetActive(false);
            if (!Cursor.visible) Cursor.visible = true;
            return; // Skip position/scale updates while hidden
        }
        else
        {
            if (!_cursorImage.gameObject.activeSelf) _cursorImage.gameObject.SetActive(true);
            if (Cursor.visible) Cursor.visible = false;
        }

        // --- Position ---
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _cursorImage.parent as RectTransform, Input.mousePosition, null, out Vector2 pos);
        _cursorImage.localPosition = pos;

        // --- Scale (shoot punch) ---
        _currentScale = Mathf.Lerp(_currentScale, _targetScale, Time.unscaledDeltaTime * _scaleRecoverSpeed);
        _cursorImage.localScale = Vector3.one * _currentScale;

        // --- Rotation (reload spin) ---
        if (_isReloading)
        {
            _reloadTimer += Time.unscaledDeltaTime;
            _reloadProgress = Mathf.Clamp01(_reloadTimer / _reloadDuration);

            // Smooth ease-in-out using SmoothStep
            float easedProgress = Mathf.SmoothStep(0f, 1f, _reloadProgress);
            _targetRotation = easedProgress * _reloadSpinDegrees;

            if (_reloadProgress >= 1f)
            {
                _isReloading = false;
                _targetRotation = 0f; // Reset
            }
        }

        _currentRotation = Mathf.Lerp(_currentRotation, _targetRotation,
            Time.unscaledDeltaTime * _spinSmoothing * 5f);

        _cursorImage.localRotation = Quaternion.Euler(0f, 0f, -_currentRotation);

        // Snap rotation to zero when very close (avoid float drift)
        if (!_isReloading && Mathf.Abs(_currentRotation) < 0.5f)
        {
            _currentRotation = 0f;
            _cursorImage.localRotation = Quaternion.identity;
        }
    }

    /// <summary>
    /// Call this when the player shoots. Crosshair punches up in size.
    /// </summary>
    public void OnShoot()
    {
        _targetScale = _baseScale + _shootScalePunch;
        // Immediately reset target so it smoothly comes back down
        // We do this next frame via the Lerp in Update
        Invoke(nameof(ResetScale), 0.05f);
    }

    /// <summary>
    /// Call this when reload state changes. Pass true to start spin, false when done.
    /// </summary>
    public void OnReloadStateChanged(bool isReloading, float reloadDuration = 1.5f)
    {
        if (isReloading)
        {
            _isReloading = true;
            _reloadDuration = reloadDuration;
            _reloadTimer = 0f;
            _reloadProgress = 0f;
            _targetRotation = 0f;
            _currentRotation = 0f;
        }
        else
        {
            // Natural end handled by progress reaching 1.0
            _isReloading = false;
        }
    }

    private void ResetScale()
    {
        _targetScale = _baseScale;
    }
}
