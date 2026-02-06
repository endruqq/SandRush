using UnityEngine;
using UnityEngine.UI;

public class DashUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image[] _chargeIcons;
    
    [Header("Settings")]
    [SerializeField] private Color _activeColor = Color.white;
    [SerializeField] private Color _inactiveColor = new Color(1, 1, 1, 0.3f); // Semi-transparent
    [SerializeField] private bool _useFillAnimation = true;

    public void UpdateDashUI(int currentCharges, float rechargeProgress)
    {
        if (_chargeIcons == null) return;

        for (int i = 0; i < _chargeIcons.Length; i++)
        {
            Image icon = _chargeIcons[i];
            if (icon == null) continue;

            if (i < currentCharges)
            {
                // Charge is available
                icon.color = _activeColor;
                if (_useFillAnimation) icon.fillAmount = 1f;
            }
            else if (i == currentCharges)
            {
                // This is the charge currently refilling
                icon.color = _inactiveColor;
                if (_useFillAnimation)
                {
                    icon.fillAmount = rechargeProgress;
                }
                else
                {
                    // If not using fill, maybe just keep it inactive
                    // icon.fillAmount = 0f; // assuming filled type
                }
            }
            else
            {
                // Charge is empty and waiting for previous ones
                icon.color = _inactiveColor;
                if (_useFillAnimation) icon.fillAmount = 0f;
            }
        }
    }
}
