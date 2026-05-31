using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;

public class ButtonInteractable : MonoBehaviour, IInteractable
{
    [Header("Visuals")]
    [SerializeField] private Animator _animator;
    [SerializeField] private string _pressTriggerName = "Press";
    [SerializeField] private GameObject _loopVFXPrefab;
    [SerializeField] private Transform _vfxSpawnPoint;

    [Header("Settings")]
    [SerializeField] private bool _isOneTimeOnly = true;

    [Header("UI Prompt")]
    [SerializeField] private GameObject _promptUI;
    [SerializeField] private float _promptShowDistance = 2f;

    [Header("Events")]
    public UnityEvent OnActivated;

    [Header("FMOD Sound")]
    [SerializeField] private string _pressSound = "event:/Button_Press_To_Open";

    private bool _hasBeenActivated = false;
    private GameObject _spawnedVFXObject;
    private Transform _playerTransform;

    private void Start()
    {
        // Spawn looping VFX locally if assigned
        if (_loopVFXPrefab != null)
        {
            Vector3 spawnPos = _vfxSpawnPoint != null ? _vfxSpawnPoint.position : transform.position;
            _spawnedVFXObject = Instantiate(_loopVFXPrefab, spawnPos, Quaternion.identity, transform);
        }
        
        if (_promptUI == null)
        {
            CreateAutoPromptUI();
        }

        if (_promptUI != null)
        {
            _promptUI.SetActive(false);
        }
    }

    private void Update()
    {
        if (_promptUI == null) return;

        // Jeżeli przycisk został wciśnięty i ma działać tylko raz, wyłącz prompt.
        if (_isOneTimeOnly && _hasBeenActivated)
        {
            if (_promptUI.activeSelf) _promptUI.SetActive(false);
            return;
        }

        // Pobranie transform'a playera
        if (_playerTransform == null)
        {
            Player p = FindFirstObjectByType<Player>();
            if (p != null) _playerTransform = p.transform;
            else return;
        }

        // Sprawdzenie dystansu
        float dist = Vector3.Distance(transform.position, _playerTransform.position);
        bool shouldShow = dist <= _promptShowDistance;

        if (_promptUI.activeSelf != shouldShow)
        {
            _promptUI.SetActive(shouldShow);
        }
    }

    public void Interact(Player player)
    {
        if (_isOneTimeOnly && _hasBeenActivated) return;

        _hasBeenActivated = true;

        // Play button press sound
        if (!string.IsNullOrEmpty(_pressSound))
            FMODHelper.PlayOneShot(_pressSound, transform.position);

        // Visual Feedback
        if (_animator != null)
        {
            _animator.SetTrigger(_pressTriggerName);
        }

        // Stop VFX
        if (_spawnedVFXObject != null)
        {
            // Find ALL particle systems, including children
            var particles = _spawnedVFXObject.GetComponentsInChildren<ParticleSystem>();
            foreach (var ps in particles)
            {
                ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }
            
            // Optionally detach to let particles fade out naturally
            _spawnedVFXObject.transform.SetParent(null);
            Destroy(_spawnedVFXObject, 5f);
            _spawnedVFXObject = null;
        }
        
        // Wyłącz prompt po wciśnięciu (jeżeli działa raz)
        if (_isOneTimeOnly && _promptUI != null)
        {
            _promptUI.SetActive(false);
        }

        // Trigger Action (Door Open, etc.)
        Debug.Log($"[ButtonInteractable] Activated: {gameObject.name}");
        OnActivated?.Invoke();
    }

    private void CreateAutoPromptUI()
    {
        // 1. Create a WorldSpace Canvas GameObject
        GameObject canvasGo = new GameObject("Button_PromptUI");
        canvasGo.transform.SetParent(transform, false);

        // Position it higher above the Button
        float height = 3.5f;
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            height = col.bounds.max.y - transform.position.y + 1.8f;
        }
        canvasGo.transform.localPosition = new Vector3(0f, height, 0f);

        Canvas canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        
        // Setup RectTransform size and scale (matching the premium save station style)
        RectTransform canvasRect = canvasGo.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(240f, 65f); 
        canvasRect.localScale = new Vector3(0.015f, 0.015f, 0.015f);

        // 2. Add FaceCamera component to automatically face the camera
        canvasGo.AddComponent<FaceCamera>();

        // 3. Create a sleek dark panel background
        GameObject bgGo = new GameObject("Background");
        bgGo.transform.SetParent(canvasGo.transform, false);
        
        RectTransform bgRect = bgGo.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;

        Image bgImage = bgGo.AddComponent<Image>();
        bgImage.color = new Color(0.03f, 0.03f, 0.03f, 0.85f); // Sleek dark translucent charcoal

        // Subtle gold border outline
        Outline outline = bgGo.AddComponent<Outline>();
        outline.effectColor = new Color(1f, 0.55f, 0f, 0.6f);
        outline.effectDistance = new Vector2(1.5f, 1.5f);

        // 4. Horizontal Layout Container
        GameObject layoutGo = new GameObject("LayoutContainer");
        layoutGo.transform.SetParent(bgGo.transform, false);
        
        RectTransform layoutRect = layoutGo.AddComponent<RectTransform>();
        layoutRect.anchorMin = Vector2.zero;
        layoutRect.anchorMax = Vector2.one;
        layoutRect.offsetMin = Vector2.zero;
        layoutRect.offsetMax = Vector2.zero;

        HorizontalLayoutGroup layoutGroup = layoutGo.AddComponent<HorizontalLayoutGroup>();
        layoutGroup.padding = new RectOffset(12, 12, 10, 10);
        layoutGroup.spacing = 14f;
        layoutGroup.childAlignment = TextAnchor.MiddleCenter;
        layoutGroup.childControlWidth = false;
        layoutGroup.childControlHeight = false;
        layoutGroup.childForceExpandWidth = false;
        layoutGroup.childForceExpandHeight = false;

        // 5. Key Badge (simulating physical key button - dark body with gold border)
        GameObject keyBadgeGo = new GameObject("KeyBadge");
        keyBadgeGo.transform.SetParent(layoutGo.transform, false);

        RectTransform keyBadgeRect = keyBadgeGo.AddComponent<RectTransform>();
        keyBadgeRect.sizeDelta = new Vector2(45f, 45f);

        Image keyBadgeImage = keyBadgeGo.AddComponent<Image>();
        keyBadgeImage.color = new Color(0.12f, 0.12f, 0.12f, 1.0f); // Dark button body

        // Gold border for Key Badge
        Outline keyOutline = keyBadgeGo.AddComponent<Outline>();
        keyOutline.effectColor = new Color(1f, 0.55f, 0f, 0.8f);
        keyOutline.effectDistance = new Vector2(1.5f, 1.5f);

        GameObject keyTextGo = new GameObject("Text");
        keyTextGo.transform.SetParent(keyBadgeGo.transform, false);

        RectTransform keyTextRect = keyTextGo.AddComponent<RectTransform>();
        keyTextRect.anchorMin = Vector2.zero;
        keyTextRect.anchorMax = Vector2.one;
        keyTextRect.offsetMin = Vector2.zero;
        keyTextRect.offsetMax = Vector2.zero;

        TextMeshProUGUI keyText = keyTextGo.AddComponent<TextMeshProUGUI>();
        keyText.text = "F";
        keyText.color = new Color(1f, 0.55f, 0f, 1f); // Vibrant gold letter
        keyText.fontSize = 24f;
        keyText.alignment = TextAlignmentOptions.Center;
        keyText.fontStyle = FontStyles.Bold;

        // 6. Action Label ("F to open doors" style)
        GameObject actionTextGo = new GameObject("ActionText");
        actionTextGo.transform.SetParent(layoutGo.transform, false);

        RectTransform actionTextRect = actionTextGo.AddComponent<RectTransform>();
        actionTextRect.sizeDelta = new Vector2(140f, 45f);

        TextMeshProUGUI actionText = actionTextGo.AddComponent<TextMeshProUGUI>();
        actionText.text = "OPEN DOORS";
        actionText.color = Color.white;
        actionText.fontSize = 18f;
        actionText.alignment = TextAlignmentOptions.Left;
        actionText.fontStyle = FontStyles.Bold;

        // 7. Caret Pointer (pointing down arrow below the panel)
        GameObject caretGo = new GameObject("CaretPointer");
        caretGo.transform.SetParent(canvasGo.transform, false);

        RectTransform caretRect = caretGo.AddComponent<RectTransform>();
        caretRect.anchorMin = new Vector2(0.5f, 0f);
        caretRect.anchorMax = new Vector2(0.5f, 0f);
        caretRect.pivot = new Vector2(0.5f, 1f);
        caretRect.sizeDelta = new Vector2(30f, 20f);
        caretRect.localPosition = new Vector3(0f, -32.5f, 0f); // Bottom edge of 65 height canvas

        TextMeshProUGUI caretText = caretGo.AddComponent<TextMeshProUGUI>();
        caretText.text = "▼";
        caretText.color = new Color(1f, 0.55f, 0f, 0.8f); // matching gold outline
        caretText.fontSize = 16f;
        caretText.alignment = TextAlignmentOptions.Center;

        _promptUI = canvasGo;
    }
}
