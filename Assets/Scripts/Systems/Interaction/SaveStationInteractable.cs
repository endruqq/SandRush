using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class SaveStationInteractable : MonoBehaviour, IInteractable
{
    [Header("UI Prompt")]
    [Tooltip("Rozpiska klawisza (np. E - Zapisz) z Canvasa, która ma się pokazywać obok")]
    [SerializeField] private GameObject _promptUI;
    [SerializeField] private float _promptShowDistance = 2f;

    [Header("Visual & Audio")]
    [Tooltip("Prefab odpalany po zapisaniu dla efektu potwierdzenia (np. rozbłysk iskierek)")]
    [SerializeField] private GameObject _saveEffectPrefab;
    [SerializeField] private string _saveSound = "event:/UI/SaveGame_Success";

    private bool _justSaved = false;
    private Transform _playerTransform;

    private void Start()
    {
        if (_promptUI == null)
        {
            CreateAutoPromptUI();
        }

        if (_promptUI != null) _promptUI.SetActive(false);
    }

    private void Update()
    {
        if (_promptUI == null) return;

        if (_justSaved)
        {
            if (_promptUI.activeSelf) _promptUI.SetActive(false);
            return;
        }

        if (_playerTransform == null)
        {
            Player p = FindFirstObjectByType<Player>();
            if (p != null) _playerTransform = p.transform;
            else return;
        }

        // System pojawiania UI przy podejściu do stacji
        float dist = Vector3.Distance(transform.position, _playerTransform.position);
        bool shouldShow = dist <= _promptShowDistance;

        if (_promptUI.activeSelf != shouldShow)
        {
            _promptUI.SetActive(shouldShow);
        }
    }

    // Wywołane, gdy gracz wejdzie w interakcję (wciśnie 'E' tak samo jak dla przycisków mapy)
    public void Interact(Player player)
    {
        if (_justSaved) return;

        // Oznaczamy w globalnym sejvie, że posiadamy fizyczny nowy punkt kontrolny (Checkpoint Station)
        PlayerPrefs.SetInt("HasCustomSave", 1);

        // Obliczamy kierunek od stacji do gracza, aby odsunąć go przy respawnie (by nie wchodził w model)
        Vector3 dirFromStation = (player.transform.position - transform.position).normalized;
        dirFromStation.y = 0; // Utrzymujemy płasko
        if (dirFromStation.sqrMagnitude < 0.01f) dirFromStation = -transform.forward; // Zabezpieczenie
        dirFromStation.Normalize();

        // Odsunąć gracza o dodatkowe 1.25 jednostki od jego aktualnej pozycji w stronę wolnej przestrzeni
        Vector3 spawnPos = player.transform.position + dirFromStation * 1.25f;

        // Zapisujemy nowe koordynaty z offsetem
        PlayerPrefs.SetFloat("RespawnPosX", spawnPos.x);
        PlayerPrefs.SetFloat("RespawnPosY", spawnPos.y);
        PlayerPrefs.SetFloat("RespawnPosZ", spawnPos.z);
        
        PlayerPrefs.Save();

        Debug.Log($"[SaveStation] Gra pomyślnie zapisana. Nowy punkt Odrodzenia: {transform.position}");

        // Feedback
        if (_saveEffectPrefab != null)
        {
            Instantiate(_saveEffectPrefab, transform.position, Quaternion.identity);
        }

        if (!string.IsNullOrEmpty(_saveSound))
        {
            FMODHelper.PlayOneShot(_saveSound, transform.position);
        }

        // Błysk ekranu (akceptacja zapisu)
        if (ScreenFlash.Instance != null)
        {
            ScreenFlash.Instance.Flash(0.1f, 0.25f); 
        }

        StartCoroutine(SaveCooldownRoutine());
    }

    private IEnumerator SaveCooldownRoutine()
    {
        _justSaved = true;
        // Odczekujemy by gracz nie spamił przycisku tysiąc razy zbijając zapis
        yield return new WaitForSeconds(5f); 
        _justSaved = false;
    }

    private void CreateAutoPromptUI()
    {
        // 1. Create a WorldSpace Canvas GameObject
        GameObject canvasGo = new GameObject("SaveStation_PromptUI");
        canvasGo.transform.SetParent(transform, false);

        // Position it higher above the Save Station
        float height = 2.6f;
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            height = col.bounds.max.y - transform.position.y + 0.9f;
        }
        canvasGo.transform.localPosition = new Vector3(0f, height, 0f);

        Canvas canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        
        // Setup RectTransform size and scale (slightly larger in scale and width)
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

        // 6. Action Label
        GameObject actionTextGo = new GameObject("ActionText");
        actionTextGo.transform.SetParent(layoutGo.transform, false);

        RectTransform actionTextRect = actionTextGo.AddComponent<RectTransform>();
        actionTextRect.sizeDelta = new Vector2(140f, 45f);

        TextMeshProUGUI actionText = actionTextGo.AddComponent<TextMeshProUGUI>();
        actionText.text = "SAVE GAME";
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
