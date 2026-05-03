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
        // Don't hide the icon initially ONLY if a placeholder sprite is set
        if (_maskIconImage != null)
        {
            _maskIconImage.enabled = _maskIconImage.sprite != null;
        }
        
        if (_cooldownImage != null) _cooldownImage.fillAmount = 0;
        
        // Check if mask is already collected (from previous session)
        if (PlayerPrefs.GetInt(StartGameTutorial.PREF_MASK_COLLECTED, 0) == 1)
        {
            HandleMaskEquipped(true);
        }

        // Find player and subscribe to events
        Player player = FindFirstObjectByType<Player>();
        if (player != null)
        {
            player.OnMaskEquipped += HandleMaskEquipped;
            player.OnMaskCooldownChanged += HandleCooldownChanged;
            player.OnMaskChanged += HandleMaskChanged;
        }
    }

    private void HandleMaskChanged(Sprite newSprite)
    {
        if (_maskIconImage != null && newSprite != null)
        {
            _maskIconImage.sprite = newSprite;
            _maskIconImage.enabled = true; // Włącz obrazek po przypisaniu maski
        }
    }

    private void HandleMaskEquipped(bool equipped)
    {
        if (equipped && _maskIconImage != null && _equippedMaskSprite != null)
        {
            _maskIconImage.sprite = _equippedMaskSprite;
            _maskIconImage.enabled = true; // Włącz obrazek po zebraniu maski
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
            player.OnMaskChanged -= HandleMaskChanged;
        }
    }
}
