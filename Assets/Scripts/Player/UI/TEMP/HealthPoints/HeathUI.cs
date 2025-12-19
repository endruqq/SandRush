using UnityEngine;
using UnityEngine.UI;

public class HealthUI : MonoBehaviour
{
    [SerializeField] private Sprite[] _healthSprites;
    [SerializeField] private Image _healthImage;
    [SerializeField] private Player _player;

    private static HealthUI _instance;

    void Awake() => _instance = this;

    public static void UpdateHealth(int currentHealth)
    {
        _instance._player.CurrentHealth = Mathf.Clamp(currentHealth, 0, 100);
        _instance.UpdateHealthSprite();
    }

    private void UpdateHealthSprite()
    {
        int hp = _player.CurrentHealth;

        if (hp > 100)
            _healthImage.sprite = _healthSprites[0];
        else if (hp > 84)
            _healthImage.sprite = _healthSprites[1];
        else if (hp > 66)
            _healthImage.sprite = _healthSprites[2];
        else if (hp > 50)
            _healthImage.sprite = _healthSprites[3];
        else if (hp > 33)
            _healthImage.sprite = _healthSprites[4];
        else
            _healthImage.sprite = _healthSprites[5];
    }
}
