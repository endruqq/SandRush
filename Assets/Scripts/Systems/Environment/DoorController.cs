using UnityEngine;
using System.Collections;

public class DoorController : MonoBehaviour
{
    public enum SlideAxis { X, Y, Z }

    [Header("Door Parts")]
    [Tooltip("Lewa część drzwi (lub górna/tylna).")]
    [SerializeField] private Transform _leftDoor;
    [Tooltip("Prawa część drzwi (lub dolna/przednia).")]
    [SerializeField] private Transform _rightDoor;

    [Header("Movement Settings")]
    [Tooltip("Oś, wzdłuż której rozsuwają się drzwi.")]
    [SerializeField] private SlideAxis _slideAxis = SlideAxis.X;
    [Tooltip("Dystans, o jaki rozsuwa się każda część drzwi.")]
    [SerializeField] private float _slideDistance = 2f;
    [Tooltip("Czas trwania otwierania w sekundach.")]
    [SerializeField] private float _duration = 1.0f;
    [Tooltip("Krzywa ruchu (np. Ease In Out dla płynnego startu i zatrzymania).")]
    [SerializeField] private AnimationCurve _slideCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("FMOD Sound")]
    [SerializeField] private string _doorSound = "event:/Door_Open_Small";

    private Vector3 _leftStartPos;
    private Vector3 _rightStartPos;
    private bool _isOpen = false;
    private Coroutine _slideCoroutine;

    private void Start()
    {
        // Zapamiętanie pozycji startowych
        if (_leftDoor != null) _leftStartPos = _leftDoor.localPosition;
        if (_rightDoor != null) _rightStartPos = _rightDoor.localPosition;
    }

    /// <summary>
    /// Otwiera drzwi rozsuwając je w przeciwne strony.
    /// </summary>
    public void OpenDoor()
    {
        if (_isOpen) return;
        _isOpen = true;

        if (_slideCoroutine != null) StopCoroutine(_slideCoroutine);
        _slideCoroutine = StartCoroutine(SlideDoorsRoutine(true));

        // Odtwarzanie dźwięku FMOD
        if (!string.IsNullOrEmpty(_doorSound))
        {
            FMODHelper.PlayOneShot(_doorSound, transform.position);
        }

        Debug.Log($"[DoorController] Opened sliding doors: {gameObject.name}");
    }

    /// <summary>
    /// Zamyka drzwi zasuwając je do pozycji początkowych.
    /// </summary>
    public void CloseDoor()
    {
        if (!_isOpen) return;
        _isOpen = false;

        if (_slideCoroutine != null) StopCoroutine(_slideCoroutine);
        _slideCoroutine = StartCoroutine(SlideDoorsRoutine(false));

        // Można dodać opcjonalny dźwięk zamykania
        if (!string.IsNullOrEmpty(_doorSound))
        {
            FMODHelper.PlayOneShot(_doorSound, transform.position);
        }

        Debug.Log($"[DoorController] Closed sliding doors: {gameObject.name}");
    }

    /// <summary>
    /// Przełącza stan drzwi (otwórz / zamknij).
    /// </summary>
    public void ToggleDoor()
    {
        if (_isOpen) CloseDoor();
        else OpenDoor();
    }

    private IEnumerator SlideDoorsRoutine(bool open)
    {
        float elapsed = 0f;

        Vector3 leftTargetPos = _leftStartPos;
        Vector3 rightTargetPos = _rightStartPos;

        if (open)
        {
            Vector3 slideDir = GetSlideDirection();
            leftTargetPos = _leftStartPos - (slideDir * _slideDistance);
            rightTargetPos = _rightStartPos + (slideDir * _slideDistance);
        }

        Vector3 leftStartPos = _leftDoor != null ? _leftDoor.localPosition : Vector3.zero;
        Vector3 rightStartPos = _rightDoor != null ? _rightDoor.localPosition : Vector3.zero;

        while (elapsed < _duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / _duration);
            float evaluatedT = _slideCurve.Evaluate(t);

            if (_leftDoor != null)
            {
                _leftDoor.localPosition = Vector3.Lerp(leftStartPos, leftTargetPos, evaluatedT);
            }
            if (_rightDoor != null)
            {
                _rightDoor.localPosition = Vector3.Lerp(rightStartPos, rightTargetPos, evaluatedT);
            }

            yield return null;
        }

        // Snap do pozycji końcowych
        if (_leftDoor != null) _leftDoor.localPosition = leftTargetPos;
        if (_rightDoor != null) _rightDoor.localPosition = rightTargetPos;

        _slideCoroutine = null;
    }

    private Vector3 GetSlideDirection()
    {
        switch (_slideAxis)
        {
            case SlideAxis.X: return Vector3.right;
            case SlideAxis.Y: return Vector3.up;
            case SlideAxis.Z: return Vector3.forward;
            default: return Vector3.right;
        }
    }
}
