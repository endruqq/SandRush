using UnityEngine;
using System.Collections.Generic;

public class ObjectFader : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private LayerMask _obstacleLayer;
    [SerializeField] private float _fadeSpeed = 10f;
    [SerializeField] private float _fadedAlpha = 0.2f;
    [SerializeField] private float _playerRadius = 2f; // Objects within this radius of player also fade (optional High Wall check)

    [Header("References")]
    [SerializeField] private Transform _player;

    private List<FadingObject> _currentlyFadingObjects = new List<FadingObject>();
    
    // We keep track of hits this frame to know what stopped blocking
    private HashSet<FadingObject> _hitsThisFrame = new HashSet<FadingObject>();

    private Camera _cam;

    private void Start()
    {
        _cam = Camera.main;
        if (_player == null)
        {
            // Try to auto-find player if not assigned
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) _player = p.transform;
        }
    }

    private void Update()
    {
        if (_player == null || _cam == null) return;

        // Update Global Shader Player Position for Cutout Shader
        Shader.SetGlobalVector("_PlayerPos", _player.position);

        _hitsThisFrame.Clear();

        // 1. Raycast from Camera to Player (Screen Center usually, or Player Position)
        // We use direction from Cam to Player (adding a small offset up to aim for head/body, not feet)
        Vector3 playerPos = _player.position + Vector3.up * 1.0f; // Aim for Center
        Vector3 dir = (playerPos - _cam.transform.position).normalized;
        float dist = Vector3.Distance(_cam.transform.position, playerPos);

        // Raycast
        RaycastHit[] hits = Physics.RaycastAll(_cam.transform.position, dir, dist, _obstacleLayer);

        foreach (RaycastHit hit in hits)
        {
            ProcessHit(hit.collider);
        }

        // 2. Proximity Check Logic (like Synthetik high walls nearby)
        if (_playerRadius > 0)
        {
            Collider[] proximityHits = Physics.OverlapSphere(_player.position, _playerRadius, _obstacleLayer);
            foreach (Collider col in proximityHits)
            {
                ProcessHit(col);
            }
        }

        // Manage Fading State
        ManageFadingObjects();
    }

    private void ProcessHit(Collider collider)
    {
        FadingObject fader = collider.GetComponent<FadingObject>();
        
        // If script misses, add it
        if (fader == null)
        {
            fader = collider.gameObject.AddComponent<FadingObject>();
        }

        _hitsThisFrame.Add(fader);
    }

    private void ManageFadingObjects()
    {
        // 1. Start fading NEW hits
        foreach (FadingObject fader in _hitsThisFrame)
        {
            fader.StartFading(_fadedAlpha, _fadeSpeed);
            if (!_currentlyFadingObjects.Contains(fader))
            {
                _currentlyFadingObjects.Add(fader);
            }
        }

        // 2. Restore fading for objects NOT hit this frame
        for (int i = _currentlyFadingObjects.Count - 1; i >= 0; i--)
        {
            FadingObject fader = _currentlyFadingObjects[i];
            
            if (fader == null)
            {
                _currentlyFadingObjects.RemoveAt(i);
                continue;
            }

            if (!_hitsThisFrame.Contains(fader))
            {
                fader.RestoreFade(_fadeSpeed);
                _currentlyFadingObjects.RemoveAt(i);
            }
        }
    }
}
