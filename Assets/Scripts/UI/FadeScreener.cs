using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class FadeScreener : MonoBehaviour
{
    public static FadeScreener Instance { get; private set; }

    [Header("Loading Screen")]
    [SerializeField] private Image _fadeImage; // UI Image with black color
    [SerializeField] private float _fadeDuration = 0.5f;
    
    [Tooltip("If true, automatically fades from black to clear when the scene starts.")]
    [SerializeField] private bool _fadeInOnStart = true;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        if (_fadeInOnStart && _fadeImage != null)
        {
            // Set immediately black
            Color c = _fadeImage.color;
            c.a = 1f;
            _fadeImage.color = c;
            _fadeImage.gameObject.SetActive(true);
            
            // Fade to clear
            StartCoroutine(FadeOutCoroutine());
        }
        else if (_fadeImage != null)
        {
            Color c = _fadeImage.color;
            c.a = 0f;
            _fadeImage.color = c;
            _fadeImage.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Animates the screen to black. (e.g. before teleporting)
    /// </summary>
    public IEnumerator FadeIn()
    {
        yield return StartCoroutine(Fade(0f, 1f));
    }

    /// <summary>
    /// Animates the screen from black to clear. (e.g. after teleporting)
    /// </summary>
    public IEnumerator FadeOut()
    {
        yield return StartCoroutine(Fade(1f, 0f));
    }

    private IEnumerator FadeOutCoroutine()
    {
        yield return StartCoroutine(Fade(1f, 0f));
    }

    private IEnumerator Fade(float from, float to)
    {
        if (_fadeImage == null) yield break;
        
        _fadeImage.gameObject.SetActive(true);
        float elapsed = 0f;
        Color c = _fadeImage.color;
        
        while (elapsed < _fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime; // Unscaled ensures it isn't blocked by HitStop or Pauses
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

    /// <summary>
    /// Call this from normal scripts or UI buttons to gracefully fade out and load a new scene!
    /// Usage: FadeScreener.Instance.FadeAndLoadScene("SandRush_Level_1");
    /// </summary>
    public void FadeAndLoadScene(string sceneName)
    {
        StartCoroutine(LoadSceneRoutine(sceneName));
    }

    private IEnumerator LoadSceneRoutine(string sceneName)
    {
        // 1. Fade to black
        yield return StartCoroutine(FadeIn());

        // 2. Load scene
        // Since we don't carry this prefab over (DontDestroyOnLoad issues), we just gracefully load.
        // The newly loaded scene MUST have its own FadeScreener prefab which will automatically FadeOut(clear) on Start()
        SceneManager.LoadScene(sceneName);
    }
}
