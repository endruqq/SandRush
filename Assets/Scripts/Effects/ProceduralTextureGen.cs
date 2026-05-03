using UnityEngine;
using System.Collections.Generic;

public class ProceduralTextureGen : MonoBehaviour
{
    [SerializeField] private int _resolution = 512;
    [Tooltip("Dark Red / Black from Synthetik reference")]
    [SerializeField] private Color _color = new Color(0.1f, 0f, 0f, 1f); // Almost black red
    [SerializeField] private Renderer _targetRenderer;

    private void Start()
    {
        if (_targetRenderer == null) _targetRenderer = GetComponent<Renderer>();
        if (_targetRenderer == null) return;

        Texture2D texture = GenerateDirectionalSplat();
        
        // Settings for decal-like behavior
        if (_targetRenderer is SpriteRenderer spriteRenderer)
        {
            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, _resolution, _resolution), new Vector2(0.5f, 0.0f)); // Pivot at bottom center
            spriteRenderer.sprite = sprite;
        }
        else
        {
            _targetRenderer.material.mainTexture = texture;
            // Force Transparency setup for Standard Shader or similar
             _targetRenderer.material.SetFloat("_Mode", 2); // Fade
             _targetRenderer.material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
             _targetRenderer.material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
             _targetRenderer.material.SetInt("_ZWrite", 0);
             _targetRenderer.material.DisableKeyword("_ALPHATEST_ON");
             _targetRenderer.material.EnableKeyword("_ALPHABLEND_ON");
             _targetRenderer.material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
             _targetRenderer.material.renderQueue = 3000;
        }
    }

    private Texture2D GenerateDirectionalSplat()
    {
        Texture2D texture = new Texture2D(_resolution, _resolution);
        Color[] pixels = new Color[_resolution * _resolution];

        // Fill with transparent
        for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.clear;

        Vector2 origin = new Vector2(_resolution / 2f, _resolution * 0.1f);
        float seed = Random.value * 100f;

        // 1. Massive Base Pool
        int blobs = Random.Range(3, 6);
        for(int i=0; i<blobs; i++)
        {
            float radius = Random.Range(_resolution * 0.05f, _resolution * 0.15f);
            float offsetX = Random.Range(-radius * 0.5f, radius * 0.5f);
            float offsetY = Random.Range(-radius * 0.2f, radius * 0.5f);
            DrawIrregularBlob(pixels, (int)(origin.x + offsetX), (int)(origin.y + offsetY), (int)radius, seed + i * 13.5f);
        }

        // 2. Multi-Tendril Explosion (Main Splash)
        int tendrils = Random.Range(5, 12);
        for (int i = 0; i < tendrils; i++)
        {
            float angle = Random.Range(-45f, 45f) * Mathf.Deg2Rad;
            float length = Random.Range(_resolution * 0.3f, _resolution * 0.95f);
            float widthStart = Random.Range(_resolution * 0.02f, _resolution * 0.08f);
            float widthEnd = 0f;
            
            DrawDistortedTendril(pixels, origin, angle, length, widthStart, widthEnd, seed + i * 5.2f);
        }

        // 3. Heavy Spray Cloud
        int droplets = Random.Range(400, 800);
        for(int i=0; i<droplets; i++)
        {
            float rAngle = Random.Range(-60f, 60f) * Mathf.Deg2Rad;
            float dist = Random.Range(_resolution * 0.1f, _resolution * 0.9f);
            
            // Bias distance towards center (heavier near base)
            dist = dist * (1f - Random.value * 0.5f);

            Vector2 pos = origin + new Vector2(Mathf.Sin(rAngle), Mathf.Cos(rAngle)) * dist;
            
            if(pos.x >= 0 && pos.x < _resolution && pos.y >= 0 && pos.y < _resolution)
            {
                int r = Random.Range(1, 4);
                if (Random.value > 0.9f) r += Random.Range(2, 5); // Occasional big drop
                DrawCircle(pixels, (int)pos.x, (int)pos.y, r);
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();
        return texture;
    }

    private void DrawDistortedTendril(Color[] pixels, Vector2 start, float angleRad, float length, float widthStart, float widthEnd, float seed)
    {
        Vector2 direction = new Vector2(Mathf.Sin(angleRad), Mathf.Cos(angleRad)); // Up is 0 deg
        
        int steps = (int)(length * 2); 
        for(int i=0; i<steps; i++)
        {
            float t = (float)i / steps;
            Vector2 currentPos = start + direction * (t * length);
            
            // Add curve
            float curveAmount = Mathf.Sin(t * Mathf.PI) * (_resolution * 0.05f) * (Mathf.PerlinNoise(seed, t * 5f) - 0.5f);
            currentPos.x += curveAmount;

            float currentWidth = Mathf.Lerp(widthStart, widthEnd, t);
            
            // Noise distortion on width to make it gloopy
            float noise = Mathf.PerlinNoise(currentPos.x * 0.1f + seed, currentPos.y * 0.1f + seed);
            currentWidth *= (0.5f + noise);
            
            if (currentWidth < 1f) continue;

            DrawCircle(pixels, (int)currentPos.x, (int)currentPos.y, (int)(currentWidth * 0.5f));
        }
    }

    private void DrawCircle(Color[] pixels, int cx, int cy, int radius)
    {
        int r2 = radius * radius;
        for (int y = cy - radius; y <= cy + radius; y++)
        {
            if (y < 0 || y >= _resolution) continue;
            for (int x = cx - radius; x <= cx + radius; x++)
            {
                if (x < 0 || x >= _resolution) continue;
                int dy = y - cy;
                int dx = x - cx;
                if (dx*dx + dy*dy <= r2)
                {
                    pixels[y * _resolution + x] = _color;
                }
            }
        }
    }
    
    private void DrawIrregularBlob(Color[] pixels, int cx, int cy, int radius, float seed)
    {
         for (int y = cy - radius; y <= cy + radius; y++)
        {
            if (y < 0 || y >= _resolution) continue;
            for (int x = cx - radius; x <= cx + radius; x++)
            {
                if (x < 0 || x >= _resolution) continue;
                
                int dy = y - cy;
                int dx = x - cx;
                float angle = Mathf.Atan2(dy, dx);
                float dist = Mathf.Sqrt(dx*dx + dy*dy);
                
                float noise = Mathf.PerlinNoise(Mathf.Cos(angle) + seed, Mathf.Sin(angle) + seed);
                float distThreshold = radius * (0.5f + noise * 0.5f);
                
                if (dist <= distThreshold)
                {
                    pixels[y * _resolution + x] = _color;
                }
            }
        }
    }
}
