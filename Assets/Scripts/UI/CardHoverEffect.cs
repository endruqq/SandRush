using UnityEngine;
using UnityEngine.EventSystems;

public class CardHoverEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private Vector3 _originalScale = Vector3.one;

    private void Awake()
    {
        // Awake executes immediately upon AddComponent.
        // If the scale has already been set to zero for animation, default to Vector3.one.
        if (transform.localScale.sqrMagnitude > 0.001f)
        {
            _originalScale = transform.localScale;
        }
        else
        {
            _originalScale = Vector3.one;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        transform.localScale = _originalScale * 1.05f;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        transform.localScale = _originalScale;
    }

    private void OnDisable()
    {
        transform.localScale = _originalScale;
    }
}
