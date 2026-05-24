using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class UIParticleEmitter : MonoBehaviour
{
    private class UIParticle
    {
        public GameObject GameObject;
        public Image Image;
        public RectTransform RectTransform;
        public Vector2 Velocity;
        public float RotationSpeed;
        public float MaxLifetime;
        public float Age;
        public float SwaySpeed;
        public float SwayAmount;
        public float SwayOffset;
        public Color BaseColor;
    }

    [Header("Particle Settings")]
    [SerializeField] private float _spawnRate = 0.05f; // Spawn one every 0.05s
    [SerializeField] private int _maxParticles = 80;

    private List<UIParticle> _particles = new List<UIParticle>();
    private float _spawnTimer = 0f;
    private RectTransform _containerRect;

    private Color[] _themeColors = new Color[]
    {
        new Color(1f, 0.55f, 0f, 1f),       // Egyptian Gold (#FF9D00)
        new Color(1f, 0.7f, 0.1f, 1f),      // Light Amber
        new Color(0f, 0.96f, 1f, 1f),       // Futuristic Cyan (#00F6FF)
        new Color(0f, 0.7f, 0.9f, 1f)        // Deep Turquoise
    };

    private void Awake()
    {
        _containerRect = GetComponent<RectTransform>();
    }

    private void Update()
    {
        // 1. Handle Spawning (using unscaled time since game is paused)
        _spawnTimer += Time.unscaledDeltaTime;
        if (_spawnTimer >= _spawnRate && _particles.Count < _maxParticles)
        {
            _spawnTimer = 0f;
            SpawnParticle();
        }

        // 2. Update and animate existing particles
        for (int i = _particles.Count - 1; i >= 0; i--)
        {
            UIParticle p = _particles[i];
            p.Age += Time.unscaledDeltaTime;

            if (p.Age >= p.MaxLifetime)
            {
                Destroy(p.GameObject);
                _particles.RemoveAt(i);
                continue;
            }

            float normalizedAge = p.Age / p.MaxLifetime;

            // Apply upward velocity and horizontal sway
            float sway = Mathf.Sin((p.Age * p.SwaySpeed) + p.SwayOffset) * p.SwayAmount;
            Vector2 position = p.RectTransform.anchoredPosition;
            position.y += p.Velocity.y * Time.unscaledDeltaTime;
            position.x += (p.Velocity.x + sway) * Time.unscaledDeltaTime;
            p.RectTransform.anchoredPosition = position;

            // Rotate
            p.RectTransform.Rotate(0, 0, p.RotationSpeed * Time.unscaledDeltaTime);

            // Fade out near the end of lifetime
            float alpha = 1f;
            if (normalizedAge > 0.6f)
            {
                alpha = Mathf.Lerp(1f, 0f, (normalizedAge - 0.6f) / 0.4f);
            }
            // Fade in at the start
            else if (normalizedAge < 0.1f)
            {
                alpha = Mathf.Lerp(0f, 1f, normalizedAge / 0.1f);
            }

            p.Image.color = new Color(p.BaseColor.r, p.BaseColor.g, p.BaseColor.b, p.BaseColor.a * alpha);
        }
    }

    private void SpawnParticle()
    {
        GameObject pGo = new GameObject("UIParticle");
        pGo.transform.SetParent(transform, false);

        // Position at the bottom of the container
        RectTransform rt = pGo.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0f);
        rt.anchorMax = new Vector2(0.5f, 0f);
        
        float width = _containerRect.rect.width;
        float randomX = Random.Range(-width * 0.5f, width * 0.5f);
        rt.anchoredPosition = new Vector2(randomX, -20f);

        // Particle shape and color
        Image img = pGo.AddComponent<Image>();
        Color randomColor = _themeColors[Random.Range(0, _themeColors.Length)];
        img.color = new Color(randomColor.r, randomColor.g, randomColor.b, 0f); // Start faded

        // Set random size (simulate dust / shards)
        float size = Random.Range(6f, 18f);
        rt.sizeDelta = new Vector2(size, size);

        // Setup physics and life parameters
        UIParticle particle = new UIParticle
        {
            GameObject = pGo,
            Image = img,
            RectTransform = rt,
            Velocity = new Vector2(Random.Range(-20f, 20f), Random.Range(60f, 140f)),
            RotationSpeed = Random.Range(-120f, 120f),
            MaxLifetime = Random.Range(3f, 6f),
            Age = 0f,
            SwaySpeed = Random.Range(2f, 5f),
            SwayAmount = Random.Range(15f, 40f),
            SwayOffset = Random.Range(0f, Mathf.PI * 2f),
            BaseColor = randomColor
        };

        // Add a slight rotation offset initially to look like diamonds/pyramids
        rt.rotation = Quaternion.Euler(0, 0, Random.Range(0f, 360f));

        _particles.Add(particle);
    }

    private void OnDestroy()
    {
        foreach (var p in _particles)
        {
            if (p.GameObject != null)
            {
                Destroy(p.GameObject);
            }
        }
        _particles.Clear();
    }
}
