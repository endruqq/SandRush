using UnityEngine;
using UnityEngine.EventSystems;

public class PauseButtonHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("References")]
    [Tooltip("Normalny przycisk (zazwyczaj pierwszy podobiekt). Jeśli puste, zostanie wykryty automatycznie.")]
    [SerializeField] private GameObject _normalImage;

    [Tooltip("Podświetlony przycisk (zazwyczaj drugi podobiekt). Jeśli puste, zostanie wykryty automatycznie.")]
    [SerializeField] private GameObject _hoverImage;

    private void Awake()
    {
        // Automatyczne przypisanie, jeśli nie wybrano w inspektorze
        if (_normalImage == null && transform.childCount > 0)
        {
            _normalImage = transform.GetChild(0).gameObject;
        }
        if (_hoverImage == null && transform.childCount > 1)
        {
            _hoverImage = transform.GetChild(1).gameObject;
        }

        ResetVisuals();
    }

    private void OnEnable()
    {
        ResetVisuals();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (_normalImage != null) _normalImage.SetActive(false);
        if (_hoverImage != null) _hoverImage.SetActive(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        ResetVisuals();
    }

    private void OnDisable()
    {
        ResetVisuals();
    }

    private void ResetVisuals()
    {
        if (_normalImage != null) _normalImage.SetActive(true);
        if (_hoverImage != null) _hoverImage.SetActive(false);
    }
}
