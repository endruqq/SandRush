using UnityEngine;

[RequireComponent(typeof(Collider))]
public class PickupItem : MonoBehaviour
{
    public enum PickupType { Health, Shield }
    
    [Header("Settings")]
    public PickupType type;
    public int amount = 25;
    
    [Header("Sound Effect")]
    [SerializeField] private string _pickupSoundEvent = "event:/UI/Select";

    private bool _collected = false;

    private void OnTriggerEnter(Collider other)
    {
        if (_collected) return;

        // Sprawdzamy czy to Player dotknął przedmiotu
        if (other.CompareTag("Player") || other.GetComponentInParent<Player>() != null)
        {
            _collected = true;

            // Leczenie zależne od typu Pickupa
            if (type == PickupType.Health)
            {
                Player.Heal(amount);
            }
            else if (type == PickupType.Shield)
            {
                Player.AddShield(amount);
            }

            if (!string.IsNullOrEmpty(_pickupSoundEvent))
            {
                 FMODHelper.PlayOneShot(_pickupSoundEvent, transform.position);
            }
            
            Destroy(gameObject);
        }
    }
}
