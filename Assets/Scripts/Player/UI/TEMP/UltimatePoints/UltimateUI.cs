using UnityEngine;
using UnityEngine.UI;

public class UltimateUI : MonoBehaviour
{
    [SerializeField] private Image[] _healthSprites;
    [SerializeField] private Player _player;

    [Header("Colors")]
    [SerializeField] private Color _activeColor = new(0f, 190f, 192f, 255f);
    [SerializeField] private Color _inactiveColor = new(255f, 255f, 255f, 255f);

    private static UltimateUI _instance;

    void Awake() => _instance = this;

    public static void UpdateUltimate(int currentUlt)
    {
        if (_instance == null) return;

        currentUlt = Mathf.Clamp(currentUlt, 0, _instance._healthSprites.Length);
        _instance._player.CurrentUltimate = currentUlt;

        _instance.UpdateUI(currentUlt);
    }

    private void UpdateUI(int currentUlt)
    {
        for (int i = 0; i < _healthSprites.Length; i++)
        {
            if (i < currentUlt)
                _healthSprites[i].color = _activeColor;
            else
                _healthSprites[i].color = _inactiveColor;
        }
    }
}
