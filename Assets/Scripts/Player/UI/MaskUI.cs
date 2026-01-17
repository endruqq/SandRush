using UnityEngine;
using UnityEngine.UI;

public class MaskUI : MonoBehaviour
{
    [SerializeField] private GameObject _maskIconObject;
    [SerializeField] private Image _cooldownImage;
    [SerializeField] private GameObject _activeEffectVisual;

    private void Start()
    {
        if (_maskIconObject != null) _maskIconObject.SetActive(false);
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
        if (_maskIconObject != null) _maskIconObject.SetActive(equipped);
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
