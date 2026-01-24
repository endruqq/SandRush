using UnityEngine;
using System.Collections.Generic;

public class FadingObject : MonoBehaviour
{
    private Dictionary<Renderer, Material[]> _rendererMaterials = new Dictionary<Renderer, Material[]>();
    private float _initialAlpha = 1.0f;
    
    // Use _Fade property for the cutout shader
    private static readonly int FadeID = Shader.PropertyToID("_Fade");
    
    // We use MaterialPropertyBlock to avoid instantiating new materials, which is better for performance.
    private MaterialPropertyBlock _propBlock;

    private void Awake()
    {
        _propBlock = new MaterialPropertyBlock();
        CacheRenderers();
    }

    private void CacheRenderers()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (Renderer rend in renderers)
        {
            _rendererMaterials[rend] = rend.sharedMaterials;
        }
    }

    // Simplified Method: Immediate Set (Lerping handled by Manager or INTERNAL Update)
    private float _currentAlpha = 1.0f;
    private float _targetAlpha = 1.0f;
    private float _fadeSpeed = 5.0f;

    public void StartFading(float targetAlpha, float speed)
    {
        _targetAlpha = targetAlpha;
        _fadeSpeed = speed;
        
        if (!enabled) enabled = true;
    }

    public void RestoreFade(float speed)
    {
        _targetAlpha = _initialAlpha;
        _fadeSpeed = speed;
    }

    private void Update()
    {
        if (Mathf.Abs(_currentAlpha - _targetAlpha) < 0.01f)
        {
            _currentAlpha = _targetAlpha;
            ApplyFade(_currentAlpha);
            
            if (_currentAlpha >= 0.99f)
            {
                // Disable script to save update calls when fully opaque
                enabled = false;
            }
            return;
        }

        _currentAlpha = Mathf.MoveTowards(_currentAlpha, _targetAlpha, Time.deltaTime * _fadeSpeed);
        ApplyFade(_currentAlpha);
    }

    private void ApplyFade(float alpha)
    {
        foreach (var kvp in _rendererMaterials)
        {
            Renderer rend = kvp.Key;
            if (rend == null) continue;
            
            rend.GetPropertyBlock(_propBlock);
            
            // FadingObject now controls the "_Fade" property.
            // 0.0 = Opaque (No Cutout), 1.0 = Max Cutout
            // Alpha passed from ObjectFader is usually 0.2 (transparent) to 1.0 (opaque)
            // So Fade = 1.0 - alpha
            
            _propBlock.SetFloat(FadeID, 1.0f - alpha);
            
            rend.SetPropertyBlock(_propBlock);
        }
    }
}
