using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class HitFlash : MonoBehaviour
{
    [Tooltip("The material to use when flashing (e.g., Unlit White).")]
    [SerializeField] private Material _flashMaterial;
    [Tooltip("Duration of the flash in seconds.")]
    [SerializeField] private float _duration = 0.1f;
    [Tooltip("Renderers to affect. If empty, will try to find them automatically.")]
    [SerializeField] private Renderer[] _renderers;

    private Dictionary<Renderer, Material[]> _originalMaterials = new Dictionary<Renderer, Material[]>();
    private Coroutine _flashCoroutine;

    private void Awake()
    {
        // Auto-find renderers if not assigned
        if (_renderers == null || _renderers.Length == 0)
        {
            _renderers = GetComponentsInChildren<Renderer>();
        }

        // Cache original materials using sharedMaterials to avoid instantiating copies!
        foreach (var renderer in _renderers)
        {
            if (renderer != null)
            {
                _originalMaterials[renderer] = renderer.sharedMaterials;
            }
        }
    }

    private void OnDisable()
    {
        RestoreMaterials();
    }

    public void Flash()
    {
        if (_flashMaterial == null) return;

        // Stop existing coroutine to restart flash
        if (_flashCoroutine != null)
        {
            StopCoroutine(_flashCoroutine);
        }
        
        _flashCoroutine = StartCoroutine(FlashRoutine());
    }

    private IEnumerator FlashRoutine()
    {
        // Swap to flash material using sharedMaterials to avoid allocations
        foreach (var renderer in _renderers)
        {
            if (renderer == null) continue;
            
            if (_originalMaterials.TryGetValue(renderer, out var originals))
            {
                Material[] flashMats = new Material[originals.Length];
                for (int i = 0; i < flashMats.Length; i++)
                {
                    flashMats[i] = _flashMaterial;
                }
                renderer.sharedMaterials = flashMats;
            }
        }

        yield return new WaitForSeconds(_duration);

        RestoreMaterials();
        _flashCoroutine = null;
    }

    public void RestoreMaterials()
    {
        foreach (var renderer in _renderers)
        {
            if (renderer == null) continue;
            
            if (_originalMaterials.ContainsKey(renderer))
            {
                renderer.sharedMaterials = _originalMaterials[renderer];
            }
        }
    }
}
