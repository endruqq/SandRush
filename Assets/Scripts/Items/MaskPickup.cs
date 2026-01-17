using UnityEngine;

public class MaskPickup : MonoBehaviour, IInteractable
{
    [SerializeField] private GameObject _visualModel;

    public void Interact(Player player)
    {
        player.EquipMask();
        
        if (_visualModel != null)
        {
            _visualModel.SetActive(false);
        }
        
        // Disable collider to prevent re-pickup
        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;
        
        // Destroy this object after a short delay or keep it disabled
        Destroy(gameObject, 0.5f);
    }
}
