using UnityEngine;
using UnityEngine.EventSystems;

namespace SandRush.UI
{
    public class MaskOption : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        [Header("Hover Settings")]
        [Tooltip("The scale to apply when the mouse hovers over this mask option")]
        [SerializeField] private Vector3 _hoverScale = new Vector3(1.1f, 1.1f, 1.1f);
        
        private Vector3 _originalScale;
        private MaskSelectionWheel _wheelManager;
        private MaskSelectionWheel.MaskWheelOption _optionData;

        public void Initialize(MaskSelectionWheel manager, MaskSelectionWheel.MaskWheelOption optionData)
        {
            _wheelManager = manager;
            _optionData = optionData;
        }

        private void Awake()
        {
            _originalScale = transform.localScale;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            // Scale up slightly on hover
            transform.localScale = _hoverScale;
            if (_wheelManager != null)
            {
                _wheelManager.OnMaskOptionHovered(_optionData);
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            // Reset to original scale when mouse leaves
            transform.localScale = _originalScale;
            if (_wheelManager != null)
            {
                _wheelManager.OnMaskOptionHoverExited(_optionData);
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (_wheelManager != null)
            {
                // Inform the manager that this item was clicked
                _wheelManager.OnMaskOptionClicked(_optionData);
            }
        }

        private void OnDisable()
        {
            // In case the object is disabled while hovered, reset the scale and hover state
            transform.localScale = _originalScale;
            if (_wheelManager != null)
            {
                _wheelManager.OnMaskOptionHoverExited(_optionData);
            }
        }
    }
}
