using UnityEngine;

// This script just tells all shaders where the player is.
// Attach it to the Player or any Manager object.
public class GlobalShaderUpdater : MonoBehaviour
{
    private Transform _playerTransform;

    private void Start()
    {
        // Auto-find player if attached to manager, or use self if attached to player
        if (gameObject.CompareTag("Player"))
        {
            _playerTransform = transform;
        }
        else
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) _playerTransform = p.transform;
        }
    }

    private void Update()
    {
        if (_playerTransform != null)
        {
            // Set global shader variable
            Shader.SetGlobalVector("_PlayerPos", _playerTransform.position);
        }
    }
}
