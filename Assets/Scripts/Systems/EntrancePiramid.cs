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
    [SerializeField] private Image _fadeImage; // UI Image with black color
    [SerializeField] private float _fadeDuration = 0.5f;
    [SerializeField] private float _loadingDuration = 0.5f; // Time to stay black
    
    private bool _isTeleporting = false;
    
    private void Start()
    {
        // Ensure collider is trigger
        BoxCollider col = GetComponent<BoxCollider>();
        if (col != null) col.isTrigger = true;
        
        // Hide fade image at start
        if (_fadeImage != null)
        {
            Color c = _fadeImage.color;
            c.a = 0f;
            _fadeImage.color = c;
            _fadeImage.gameObject.SetActive(false);
        }
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
        
        // Fade in (to black)
        yield return StartCoroutine(Fade(0f, 1f));
        
        // Teleport player
        CharacterController cc = player.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false; // Disable to allow position change
        
        player.position = _spawnPoint.position;
        player.rotation = _spawnPoint.rotation;
        
        if (cc != null) cc.enabled = true;
        
        // Wait while loading screen is visible
        yield return new WaitForSeconds(_loadingDuration);
        
        // Fade out (from black)
        yield return StartCoroutine(Fade(1f, 0f));
        
        _isTeleporting = false;
    }
    
    private IEnumerator Fade(float from, float to)
    {
        if (_fadeImage == null) yield break;
        
        _fadeImage.gameObject.SetActive(true);
        
        float elapsed = 0f;
        Color c = _fadeImage.color;
        
        while (elapsed < _fadeDuration)
        {
            elapsed += Time.deltaTime;
            c.a = Mathf.Lerp(from, to, elapsed / _fadeDuration);
            _fadeImage.color = c;
            yield return null;
        }
        
        c.a = to;
        _fadeImage.color = c;
        
        if (to == 0f)
        {
            _fadeImage.gameObject.SetActive(false);
        }
    }
}
