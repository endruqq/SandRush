using UnityEngine;
using UnityEngine.Events;

public class ButtonInteractable : MonoBehaviour, IInteractable
{
    [Header("Visuals")]
    [SerializeField] private Animator _animator;
    [SerializeField] private string _pressTriggerName = "Press";
    [SerializeField] private GameObject _loopVFXPrefab;
    [SerializeField] private Transform _vfxSpawnPoint;

    [Header("Settings")]
    [SerializeField] private bool _isOneTimeOnly = true;

    [Header("Events")]
    public UnityEvent OnActivated;

    private bool _hasBeenActivated = false;
    private GameObject _spawnedVFXObject;

    private void Start()
    {
        // Spawn looping VFX locally if assigned
        if (_loopVFXPrefab != null)
        {
            Vector3 spawnPos = _vfxSpawnPoint != null ? _vfxSpawnPoint.position : transform.position;
            _spawnedVFXObject = Instantiate(_loopVFXPrefab, spawnPos, Quaternion.identity, transform);
        }
    }

    public void Interact(Player player)
    {
        if (_isOneTimeOnly && _hasBeenActivated) return;

        _hasBeenActivated = true;

        // Visual Feedback
        if (_animator != null)
        {
            _animator.SetTrigger(_pressTriggerName);
        }

        // Stop VFX
        if (_spawnedVFXObject != null)
        {
            // Find ALL particle systems, including children
            var particles = _spawnedVFXObject.GetComponentsInChildren<ParticleSystem>();
            foreach (var ps in particles)
            {
                ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }
            
            // Optionally detach to let particles fade out naturally
            _spawnedVFXObject.transform.SetParent(null);
            Destroy(_spawnedVFXObject, 5f);
            _spawnedVFXObject = null;
        }

        // Trigger Action (Door Open, etc.)
        Debug.Log($"[ButtonInteractable] Activated: {gameObject.name}");
        OnActivated?.Invoke();
    }
}
