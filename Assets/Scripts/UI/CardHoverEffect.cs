using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CardHoverEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private Vector3 _originalScale = Vector3.one;
    private Sprite _originalSprite;
    private UpgradeCardButton _cardButton;
    private bool _hasCachedSprite = false;

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

        CacheCardButton();
    }

    private void CacheCardButton()
    {
        if (_cardButton == null)
        {
            _cardButton = GetComponent<UpgradeCardButton>();
            if (_cardButton != null && _cardButton.CardBackgroundImage != null)
            {
                _originalSprite = _cardButton.CardBackgroundImage.sprite;
                _hasCachedSprite = true;
            }
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        transform.localScale = _originalScale * 1.05f;
        CacheCardButton();

        if (_cardButton != null && _cardButton.CardBackgroundImage != null && _cardButton.HoverBackgroundSprite != null)
        {
            _cardButton.CardBackgroundImage.sprite = _cardButton.HoverBackgroundSprite;
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        transform.localScale = _originalScale;
        if (_cardButton != null && _cardButton.CardBackgroundImage != null && _hasCachedSprite)
        {
            _cardButton.CardBackgroundImage.sprite = _originalSprite;
        }
    }

    private void OnDisable()
    {
        transform.localScale = _originalScale;
        if (_cardButton != null && _cardButton.CardBackgroundImage != null && _hasCachedSprite)
        {
            _cardButton.CardBackgroundImage.sprite = _originalSprite;
        }
    }
}
