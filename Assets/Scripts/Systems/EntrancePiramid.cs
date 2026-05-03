using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Teleports the player to a spawn point when entering the trigger.
/// Shows a fade-in/fade-out loading screen during teleportation.
/// </summary>
[RequireComponent(typeof(BoxCollider))]
public class EntrancePiramid : MonoBehaviour
{
    [Header("Teleport Settings")]
    [SerializeField] private Transform _spawnPoint;
    [SerializeField] private string _playerTag = "Player";
    
    [Header("Loading Screen")]
    [Tooltip("Requires FadeScreener to exist in the scene!")]
    [SerializeField] private float _loadingDuration = 0.5f; // Time to stay black
    
    private bool _isTeleporting = false;
    
    private void Start()
    {
        // Ensure collider is trigger
        BoxCollider col = GetComponent<BoxCollider>();
        if (col != null) col.isTrigger = true;
    }
    
    private void OnTriggerEnter(Collider other)
    {
        if (_isTeleporting) return;
        if (!other.CompareTag(_playerTag)) return;
        if (_spawnPoint == null)
        {
            Debug.LogWarning("EntrancePiramid: Spawn point not assigned!", this);
            return;
        }
        
        StartCoroutine(TeleportSequence(other.transform));
    }
    
    private IEnumerator TeleportSequence(Transform player)
    {
        _isTeleporting = true;
        
        // Fade in (to black) using universal tool
        if (FadeScreener.Instance != null)
        {
            yield return StartCoroutine(FadeScreener.Instance.FadeIn());
        }
        
        // Teleport player
        CharacterController cc = player.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false; // Disable to allow position change
        
        player.position = _spawnPoint.position;
        player.rotation = _spawnPoint.rotation;
        
        if (cc != null) cc.enabled = true;
        
        // Wait while loading screen is visible
        yield return new WaitForSeconds(_loadingDuration);
        
        // Fade out (from black)
        if (FadeScreener.Instance != null)
        {
            yield return StartCoroutine(FadeScreener.Instance.FadeOut());
        }
        
        _isTeleporting = false;
    }
}
