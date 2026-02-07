using UnityEngine;
using UnityEngine.UI;

public class MaskUI : MonoBehaviour
{
    [SerializeField] private Image _maskIconImage;
    [SerializeField] private Sprite _equippedMaskSprite;
    [SerializeField] private Image _cooldownImage;
    [SerializeField] private GameObject _activeEffectVisual;

    private void Start()
    {
        // Don't hide the icon initially, assuming it shows the '?' placeholder
        if (_cooldownImage != null) _cooldownImage.fillAmount = 0;
        
        // Find player and subscribe to events
        Player player = FindFirstObjectByType<Player>();
        if (player != null)
        {
            player.OnMaskEquipped += HandleMaskEquipped;
            player.OnMaskCooldownChanged += HandleCooldownChanged;
        }
    }

    private void HandleMaskEquipped(bool equipped)
    {
        if (equipped && _maskIconImage != null && _equippedMaskSprite != null)
        {
            _maskIconImage.sprite = _equippedMaskSprite;
        }
    }

    private void HandleCooldownChanged(float progress)
    {
        if (_cooldownImage != null)
        {
            _cooldownImage.fillAmount = progress;
        }
    }

    private void OnDestroy()
    {
        Player player = FindFirstObjectByType<Player>();
        if (player != null)
        {
            player.OnMaskEquipped -= HandleMaskEquipped;
            player.OnMaskCooldownChanged -= HandleCooldownChanged;
        }
    }
}
