using UnityEngine;
using TMPro;

/// <summary>
/// Displays ammo count in UI. Connect to PlayerShooting events.
/// </summary>
public class AmmoUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _ammoText;
    [SerializeField] private string _format = "{0} / {1}"; // {0} = current, {1} = max
    [SerializeField] private string _reloadingText = "RELOADING...";
    
    private PlayerShooting _shooting;
    
    public void Initialize(PlayerShooting shooting)
    {
        _shooting = shooting;
        
        if (_ammoText == null)
        {
            Debug.LogWarning("[AmmoUI] _ammoText is null! Please assign a TextMeshProUGUI component in the Inspector.", this);
            return;
        }

        // Subscribe to events
        _shooting.OnAmmoChanged += UpdateAmmoDisplay;
        _shooting.OnReloadStateChanged += OnReloadStateChanged;
        
        // Initial display
        UpdateAmmoDisplay(_shooting.CurrentAmmo, _shooting.MagazineSize);
        Debug.Log($"[AmmoUI] Initialized successfully. Current ammo: {_shooting.CurrentAmmo}/{_shooting.MagazineSize}");
    }

    private void Start()
    {
        // Fallback: If not initialized by Player (e.g. references missing or order of execution issue), auto-initialize
        if (_shooting == null)
        {
            Player player = FindFirstObjectByType<Player>();
            if (player != null && player.Shooting != null)
            {
                Initialize(player.Shooting);
            }
            else
            {
                Debug.LogWarning("[AmmoUI] Fallback initialization failed: Player or PlayerShooting not found in scene.");
            }
        }
    }
    
    private void OnDestroy()
    {
        if (_shooting != null)
        {
            _shooting.OnAmmoChanged -= UpdateAmmoDisplay;
            _shooting.OnReloadStateChanged -= OnReloadStateChanged;
        }
    }
    
    private void UpdateAmmoDisplay(int current, int max)
    {
        if (_ammoText == null) return;
        _ammoText.text = string.Format(_format, current, max);
    }
    
    private void OnReloadStateChanged(bool isReloading)
    {
        if (_ammoText == null) return;
        
        if (isReloading)
        {
            _ammoText.text = _reloadingText;
        }
        else
        {
            UpdateAmmoDisplay(_shooting.CurrentAmmo, _shooting.MagazineSize);
        }
    }
}
