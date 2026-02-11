using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Cinematic player death: ragdoll + slow-mo + fade to black.
/// Attach to the Player GameObject.
/// </summary>
public class PlayerDeathEffect : MonoBehaviour
{
    [Header("Slow Motion")]
    [SerializeField] private float _slowMoTimeScale = 0.15f;
    [SerializeField] private float _slowMoDuration = 2.5f;

    [Header("Ragdoll")]
    [SerializeField] private float _ragdollForce = 1.5f;
    [SerializeField] private float _ragdollUpForce = 0f;

    [Header("Fade")]
    [SerializeField] private float _fadeDelay = 1f;
    [SerializeField] private float _fadeDuration = 1.5f;
    [SerializeField] private Color _fadeColor = Color.black;

    [Header("Restart")]
    [Tooltip("Scene to load after death. Leave empty to reload current scene.")]
    [SerializeField] private string _restartSceneName = "";
    [SerializeField] private float _restartDelay = 1f;

    private bool _isDead = false;
    private Canvas _fadeCanvas;
    private Image _fadeImage;

    /// <summary>
    /// Trigger the full death sequence.
    /// </summary>
    public void TriggerDeath(Vector3 lastHitDirection)
    {
        if (_isDead) return;
        _isDead = true;

        // Create fade overlay
        CreateFadeOverlay();

        // Start the cinematic death sequence (uses unscaledTime so slow-mo doesn't affect it)
        StartCoroutine(DeathSequence(lastHitDirection));
    }

    private IEnumerator DeathSequence(Vector3 hitDirection)
    {
        // === 1. DISABLE PLAYER CONTROLS ===
        // Disable shooting, movement, aiming
        var player = GetComponent<Player>();
        if (player != null) player.enabled = false;

        var controller = GetComponent<CharacterController>();

        // === 2. SLOW MOTION ===
        Time.timeScale = _slowMoTimeScale;
        Time.fixedDeltaTime = 0.02f * _slowMoTimeScale;

        // === 3. ACTIVATE RAGDOLL ===
        ActivateRagdoll(hitDirection, controller);

        // === 4. WAIT (in real time, not game time) ===
        yield return new WaitForSecondsRealtime(_fadeDelay);

        // === 5. FADE TO BLACK ===
        float elapsed = 0f;
        while (elapsed < _fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float alpha = Mathf.Clamp01(elapsed / _fadeDuration);
            if (_fadeImage != null)
                _fadeImage.color = new Color(_fadeColor.r, _fadeColor.g, _fadeColor.b, alpha);
            yield return null;
        }

        // === 6. RESTORE TIME AND RESTART ===
        yield return new WaitForSecondsRealtime(_restartDelay);

        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;

        string sceneToLoad = string.IsNullOrEmpty(_restartSceneName)
            ? SceneManager.GetActiveScene().name
            : _restartSceneName;

        SceneManager.LoadScene(sceneToLoad);
    }

    private void ActivateRagdoll(Vector3 hitDirection, CharacterController controller)
    {
        // Disable Animator & Controller
        Animator animator = GetComponentInChildren<Animator>();
        if (animator != null) animator.enabled = false;
        if (controller != null) controller.enabled = false;

        // Trigger Body Part Explosion (Debris)
        var exploder = GetComponent<BodyPartExploder>();
        if (exploder != null)
        {
            exploder.Explode(hitDirection);
        }
        else
        {
            // Fallback if no exploder: just disable renderer
            var renderers = GetComponentsInChildren<Renderer>();
            foreach (var r in renderers) r.enabled = false;
        }
    }

    private void CreateFadeOverlay()
    {
        // Create a full-screen canvas with a black overlay for the fade effect
        GameObject canvasObj = new GameObject("DeathFadeCanvas");
        _fadeCanvas = canvasObj.AddComponent<Canvas>();
        _fadeCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _fadeCanvas.sortingOrder = 999; // Always on top

        canvasObj.AddComponent<CanvasScaler>();
        canvasObj.AddComponent<GraphicRaycaster>();

        GameObject imageObj = new GameObject("FadeImage");
        imageObj.transform.SetParent(canvasObj.transform, false);

        _fadeImage = imageObj.AddComponent<Image>();
        _fadeImage.color = new Color(_fadeColor.r, _fadeColor.g, _fadeColor.b, 0f);

        // Stretch to fill screen
        RectTransform rt = _fadeImage.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private void OnDestroy()
    {
        // Safety: restore time if object is destroyed mid-death
        if (_isDead)
        {
            Time.timeScale = 1f;
            Time.fixedDeltaTime = 0.02f;
        }
    }
}
