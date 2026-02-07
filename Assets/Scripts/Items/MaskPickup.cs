using UnityEngine;

public class MaskPickup : MonoBehaviour, IInteractable
{
    [SerializeField] private GameObject _visualModel;
    [SerializeField] private GameObject _loopVFXPrefab;
    private ParticleSystem _spawnedVFX;

    private void Start()
    {
        if (_loopVFXPrefab != null)
        {
            GameObject vfx = Instantiate(_loopVFXPrefab, transform.position, Quaternion.identity, transform);
            _spawnedVFX = vfx.GetComponent<ParticleSystem>();
        }
    }

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

        // Fade out VFX (Stop emission)
        if (_spawnedVFX != null)
        {
            _spawnedVFX.transform.SetParent(null); // Detach so it doesn't vanish instantly
            _spawnedVFX.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            Destroy(_spawnedVFX.gameObject, 3f); // Clean up detached VFX later
        }
        
        // Destroy the pickup object
        Destroy(gameObject);
    }
}
