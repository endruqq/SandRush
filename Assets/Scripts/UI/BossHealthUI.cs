using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class BossHealthUI : MonoBehaviour
{
    private GameObject _canvasObject;
    private CanvasGroup _canvasGroup;
    private RectTransform _mainContainer;
    
    // UI Fills
    private Image _leftRedBar;
    private Image _leftCatchUpBar;
    private Image _rightRedBar;
    private Image _rightCatchUpBar;
    
    // Text Fields
    private TextMeshProUGUI _bossNameText;
    private TextMeshProUGUI _healthText;
    
    // Boss Stats Tracking
    private float _maxHealth;
    private float _currentHealth;
    private string _bossName;
    
    // Animation/State variables
    private float _currentHealthPercent = 1f;
    private float _catchUpPercent = 1f;
    private float _catchUpDelayTimer = 0f;
    
    private bool _isIntroActive = true;
    private float _displayPercent = 0f;
    
    private Vector2 _originalContainerPos;
    private Coroutine _shakeCoroutine;
    private Coroutine _fadeCoroutine;

    private Sprite _dynamicSprite;
    private Texture2D _dynamicTexture;

    public void Initialize(float maxHealth, string bossName)
    {
        _maxHealth = maxHealth;
        _currentHealth = maxHealth;
        _bossName = bossName;
        
        CreateUI();
        
        StartCoroutine(IntroRoutine());
    }

    private void CreateUI()
    {
        // 1. Create root Canvas object
        _canvasObject = new GameObject("BossHealthCanvas");
        Canvas canvas = _canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 99; // Positioned below menus, above gameplay UI
        
        _canvasObject.AddComponent<CanvasScaler>();
        _canvasObject.AddComponent<GraphicRaycaster>();
        _canvasGroup = _canvasObject.AddComponent<CanvasGroup>();
        _canvasGroup.alpha = 0f; // Start hidden for fade-in

        // Generate a 2x2 solid white sprite dynamically to avoid missing resource issues
        _dynamicTexture = new Texture2D(2, 2);
        _dynamicTexture.SetPixels(new Color[] { Color.white, Color.white, Color.white, Color.white });
        _dynamicTexture.Apply();
        _dynamicSprite = Sprite.Create(_dynamicTexture, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f));
        Sprite uiSprite = _dynamicSprite;

        // 2. Main Container (Top-Center) - shorter height since name is removed
        GameObject containerGo = new GameObject("MainContainer");
        containerGo.transform.SetParent(_canvasObject.transform, false);
        _mainContainer = containerGo.AddComponent<RectTransform>();
        _mainContainer.anchorMin = new Vector2(0.5f, 1f);
        _mainContainer.anchorMax = new Vector2(0.5f, 1f);
        _mainContainer.pivot = new Vector2(0.5f, 1f);
        _mainContainer.anchoredPosition = new Vector2(0f, -40f); // Sits closer to top
        _mainContainer.sizeDelta = new Vector2(600f, 40f);
        _originalContainerPos = _mainContainer.anchoredPosition;

        // 3. Inner Background Panel (Translucent Charcoal Black - Minimalist Strip, no border)
        GameObject innerPanelGo = new GameObject("InnerPanel");
        innerPanelGo.transform.SetParent(_mainContainer.transform, false);
        RectTransform innerPanelRect = innerPanelGo.AddComponent<RectTransform>();
        innerPanelRect.anchorMin = new Vector2(0.5f, 0.5f);
        innerPanelRect.anchorMax = new Vector2(0.5f, 0.5f);
        innerPanelRect.pivot = new Vector2(0.5f, 0.5f);
        innerPanelRect.anchoredPosition = Vector2.zero;
        innerPanelRect.sizeDelta = new Vector2(600f, 16f); // Minimalist bar height
        
        Image innerPanelImage = innerPanelGo.AddComponent<Image>();
        innerPanelImage.sprite = uiSprite;
        innerPanelImage.type = Image.Type.Simple;
        innerPanelImage.color = new Color(0.04f, 0.04f, 0.04f, 0.6f); // Clean semi-transparent charcoal

        // 4. LEFT BAR GROUP (Anchored at X = 0, Pivot = Right)
        GameObject leftBarGo = new GameObject("LeftBarGroup");
        leftBarGo.transform.SetParent(innerPanelGo.transform, false);
        RectTransform leftBarRect = leftBarGo.AddComponent<RectTransform>();
        leftBarRect.anchorMin = new Vector2(0.5f, 0.5f);
        leftBarRect.anchorMax = new Vector2(0.5f, 0.5f);
        leftBarRect.pivot = new Vector2(1f, 0.5f);
        leftBarRect.anchoredPosition = Vector2.zero;
        leftBarRect.sizeDelta = new Vector2(300f, 16f);

        // Left Background Fill (Dark Crimson)
        GameObject leftBgGo = new GameObject("BG");
        leftBgGo.transform.SetParent(leftBarGo.transform, false);
        RectTransform leftBgRect = leftBgGo.AddComponent<RectTransform>();
        leftBgRect.anchorMin = Vector2.zero;
        leftBgRect.anchorMax = Vector2.one;
        leftBgRect.offsetMin = Vector2.zero;
        leftBgRect.offsetMax = Vector2.zero;
        Image leftBgImg = leftBgGo.AddComponent<Image>();
        leftBgImg.sprite = uiSprite;
        leftBgImg.color = new Color(0.18f, 0.04f, 0.04f, 1f);

        // Left Catch-Up Bar (Gold-Yellow Fill)
        GameObject leftCatchUpGo = new GameObject("CatchUp");
        leftCatchUpGo.transform.SetParent(leftBarGo.transform, false);
        RectTransform leftCatchUpRect = leftCatchUpGo.AddComponent<RectTransform>();
        leftCatchUpRect.anchorMin = Vector2.zero;
        leftCatchUpRect.anchorMax = Vector2.one;
        leftCatchUpRect.offsetMin = Vector2.zero;
        leftCatchUpRect.offsetMax = Vector2.zero;
        _leftCatchUpBar = leftCatchUpGo.AddComponent<Image>();
        _leftCatchUpBar.sprite = uiSprite;
        _leftCatchUpBar.color = new Color(0.9f, 0.65f, 0.15f, 1f);
        _leftCatchUpBar.type = Image.Type.Filled;
        _leftCatchUpBar.fillMethod = Image.FillMethod.Horizontal;
        _leftCatchUpBar.fillOrigin = (int)Image.OriginHorizontal.Right;
        _leftCatchUpBar.fillAmount = 0f;

        // Left Health Bar (Red Fill)
        GameObject leftRedGo = new GameObject("RedFill");
        leftRedGo.transform.SetParent(leftBarGo.transform, false);
        RectTransform leftRedRect = leftRedGo.AddComponent<RectTransform>();
        leftRedRect.anchorMin = Vector2.zero;
        leftRedRect.anchorMax = Vector2.one;
        leftRedRect.offsetMin = Vector2.zero;
        leftRedRect.offsetMax = Vector2.zero;
        _leftRedBar = leftRedGo.AddComponent<Image>();
        _leftRedBar.sprite = uiSprite;
        _leftRedBar.color = new Color(0.8f, 0.12f, 0.12f, 1f);
        _leftRedBar.type = Image.Type.Filled;
        _leftRedBar.fillMethod = Image.FillMethod.Horizontal;
        _leftRedBar.fillOrigin = (int)Image.OriginHorizontal.Right;
        _leftRedBar.fillAmount = 0f;

        // 5. RIGHT BAR GROUP (Anchored at X = 0, Pivot = Left)
        GameObject rightBarGo = new GameObject("RightBarGroup");
        rightBarGo.transform.SetParent(innerPanelGo.transform, false);
        RectTransform rightBarRect = rightBarGo.AddComponent<RectTransform>();
        rightBarRect.anchorMin = new Vector2(0.5f, 0.5f);
        rightBarRect.anchorMax = new Vector2(0.5f, 0.5f);
        rightBarRect.pivot = new Vector2(0f, 0.5f);
        rightBarRect.anchoredPosition = Vector2.zero;
        rightBarRect.sizeDelta = new Vector2(300f, 16f);

        // Right Background Fill (Dark Crimson)
        GameObject rightBgGo = new GameObject("BG");
        rightBgGo.transform.SetParent(rightBarGo.transform, false);
        RectTransform rightBgRect = rightBgGo.AddComponent<RectTransform>();
        rightBgRect.anchorMin = Vector2.zero;
        rightBgRect.anchorMax = Vector2.one;
        rightBgRect.offsetMin = Vector2.zero;
        rightBgRect.offsetMax = Vector2.zero;
        Image rightBgImg = rightBgGo.AddComponent<Image>();
        rightBgImg.sprite = uiSprite;
        rightBgImg.color = new Color(0.18f, 0.04f, 0.04f, 1f);

        // Right Catch-Up Bar (Gold-Yellow Fill)
        GameObject rightCatchUpGo = new GameObject("CatchUp");
        rightCatchUpGo.transform.SetParent(rightBarGo.transform, false);
        RectTransform rightCatchUpRect = rightCatchUpGo.AddComponent<RectTransform>();
        rightCatchUpRect.anchorMin = Vector2.zero;
        rightCatchUpRect.anchorMax = Vector2.one;
        rightCatchUpRect.offsetMin = Vector2.zero;
        rightCatchUpRect.offsetMax = Vector2.zero;
        _rightCatchUpBar = rightCatchUpGo.AddComponent<Image>();
        _rightCatchUpBar.sprite = uiSprite;
        _rightCatchUpBar.color = new Color(0.9f, 0.65f, 0.15f, 1f);
        _rightCatchUpBar.type = Image.Type.Filled;
        _rightCatchUpBar.fillMethod = Image.FillMethod.Horizontal;
        _rightCatchUpBar.fillOrigin = (int)Image.OriginHorizontal.Left;
        _rightCatchUpBar.fillAmount = 0f;

        // Right Health Bar (Red Fill)
        GameObject rightRedGo = new GameObject("RedFill");
        rightRedGo.transform.SetParent(rightBarGo.transform, false);
        RectTransform rightRedRect = rightRedGo.AddComponent<RectTransform>();
        rightRedRect.anchorMin = Vector2.zero;
        rightRedRect.anchorMax = Vector2.one;
        rightRedRect.offsetMin = Vector2.zero;
        rightRedRect.offsetMax = Vector2.zero;
        _rightRedBar = rightRedGo.AddComponent<Image>();
        _rightRedBar.sprite = uiSprite;
        _rightRedBar.color = new Color(0.8f, 0.12f, 0.12f, 1f);
        _rightRedBar.type = Image.Type.Filled;
        _rightRedBar.fillMethod = Image.FillMethod.Horizontal;
        _rightRedBar.fillOrigin = (int)Image.OriginHorizontal.Left;
        _rightRedBar.fillAmount = 0f;

        // 6. Health Status Text (Centered below the bar)
        GameObject healthTextGo = new GameObject("HealthText");
        healthTextGo.transform.SetParent(_mainContainer.transform, false);
        RectTransform healthRect = healthTextGo.AddComponent<RectTransform>();
        healthRect.anchorMin = new Vector2(0f, 0f);
        healthRect.anchorMax = new Vector2(1f, 0f);
        healthRect.pivot = new Vector2(0.5f, 1f);
        healthRect.anchoredPosition = new Vector2(0f, -6f); // Positioned closely below the bar
        healthRect.sizeDelta = new Vector2(600f, 20f);

        _healthText = healthTextGo.AddComponent<TextMeshProUGUI>();
        _healthText.text = $"[ {(int)_currentHealth} / {(int)_maxHealth} ]";
        _healthText.fontSize = 11f;
        _healthText.alignment = TextAlignmentOptions.Center;
        _healthText.fontStyle = FontStyles.Bold;
        _healthText.color = new Color(0.85f, 0.65f, 0.2f, 0.85f);
        _healthText.outlineColor = Color.black;
        _healthText.outlineWidth = 0.15f;
    }

    private void Update()
    {
        if (_canvasObject == null) return;

        if (_isIntroActive)
        {
            // Set fills to display percent during introductory fill
            _leftRedBar.fillAmount = _displayPercent;
            _leftCatchUpBar.fillAmount = _displayPercent;
            _rightRedBar.fillAmount = _displayPercent;
            _rightCatchUpBar.fillAmount = _displayPercent;
            
            float tempHp = Mathf.Lerp(0f, _maxHealth, _displayPercent);
            _healthText.text = $"[ {(int)tempHp} / {(int)_maxHealth} ]";
        }
        else
        {
            // Instantly apply current target health percentage to main red bar
            _leftRedBar.fillAmount = _currentHealthPercent;
            _rightRedBar.fillAmount = _currentHealthPercent;

            // Handle delayed catch-up bar transition
            if (_catchUpDelayTimer > 0f)
            {
                _catchUpDelayTimer -= Time.deltaTime;
            }
            else
            {
                _catchUpPercent = Mathf.MoveTowards(_catchUpPercent, _currentHealthPercent, Time.deltaTime * 0.9f);
                _leftCatchUpBar.fillAmount = _catchUpPercent;
                _rightCatchUpBar.fillAmount = _catchUpPercent;
            }

            _healthText.text = $"[ {Mathf.Max(0, (int)_currentHealth)} / {(int)_maxHealth} ]";
        }
    }

    public void UpdateHealth(float current)
    {
        _currentHealth = current;
        float newPercent = Mathf.Clamp01(_currentHealth / _maxHealth);
        
        if (newPercent < _currentHealthPercent)
        {
            // Trigger impact shake if damage was taken
            TriggerImpactShake();
            _catchUpDelayTimer = 0.4f; // Wait 0.4s before catch-up drains
        }
        
        _currentHealthPercent = newPercent;
    }

    private void TriggerImpactShake()
    {
        if (_shakeCoroutine != null) StopCoroutine(_shakeCoroutine);
        _shakeCoroutine = StartCoroutine(ShakeRoutine(0.2f, 6f));
    }

    private IEnumerator ShakeRoutine(float duration, float magnitude)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float strength = Mathf.Lerp(magnitude, 0f, elapsed / duration);
            Vector2 offset = Random.insideUnitCircle * strength;
            _mainContainer.anchoredPosition = _originalContainerPos + offset;
            yield return null;
        }
        _mainContainer.anchoredPosition = _originalContainerPos;
        _shakeCoroutine = null;
    }

    private IEnumerator IntroRoutine()
    {
        _isIntroActive = true;
        _displayPercent = 0f;
        
        // 1. Fade in container alpha
        float elapsed = 0f;
        float fadeDuration = 0.5f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            _canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / fadeDuration);
            yield return null;
        }
        _canvasGroup.alpha = 1f;

        // 2. Fill health bar symmetrically
        elapsed = 0f;
        float fillDuration = 1.2f;
        while (elapsed < fillDuration)
        {
            elapsed += Time.deltaTime;
            _displayPercent = Mathf.Lerp(0f, 1f, elapsed / fillDuration);
            yield return null;
        }
        
        _displayPercent = 1f;
        _currentHealthPercent = Mathf.Clamp01(_currentHealth / _maxHealth);
        _catchUpPercent = _currentHealthPercent;
        _isIntroActive = false;
    }

    public void StartFadeOut()
    {
        if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
        _fadeCoroutine = StartCoroutine(FadeOutRoutine());
    }

    private IEnumerator FadeOutRoutine()
    {
        // Cancel shake to prevent resets
        if (_shakeCoroutine != null)
        {
            StopCoroutine(_shakeCoroutine);
            _mainContainer.anchoredPosition = _originalContainerPos;
        }

        float elapsed = 0f;
        float duration = 1.0f;
        Vector2 startPos = _mainContainer.anchoredPosition;
        Vector2 targetPos = startPos + new Vector2(0f, -25f); // Slide down slightly

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            _canvasGroup.alpha = Mathf.Lerp(1f, 0f, t);
            _mainContainer.anchoredPosition = Vector2.Lerp(startPos, targetPos, t);
            yield return null;
        }

        DestroyUI();
    }

    private void DestroyUI()
    {
        if (_canvasObject != null)
        {
            Destroy(_canvasObject);
            _canvasObject = null;
        }
        if (_dynamicSprite != null)
        {
            Destroy(_dynamicSprite);
            _dynamicSprite = null;
        }
        if (_dynamicTexture != null)
        {
            Destroy(_dynamicTexture);
            _dynamicTexture = null;
        }
    }

    private void OnDisable()
    {
        DestroyUI();
    }

    private void OnDestroy()
    {
        DestroyUI();
    }
}
