using UnityEngine;

public class UIImageRotator : MonoBehaviour
{
    [Tooltip("Rotation speed in degrees per second.")]
    [SerializeField] private float rotationSpeed = -180f;

    private RectTransform _rectTransform;

    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
        
        // Fallback gracefully if placed on a non-UI object
        if (_rectTransform == null)
        {
            Debug.LogWarning("UIImageRotator should be placed on a UI element with a RectTransform.");
        }
    }

    private void Update()
    {
        // Rotate the UI element around the Z axis (standard for 2D/UI rotation)
        if (_rectTransform != null)
        {
            _rectTransform.Rotate(0f, 0f, rotationSpeed * Time.deltaTime);
        }
        else
        {
            // Fallback for regular 3D objects if accidentally attached to one
            transform.Rotate(0f, 0f, rotationSpeed * Time.deltaTime);
        }
    }
}
