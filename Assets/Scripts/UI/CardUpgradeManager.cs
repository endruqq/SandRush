using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class CardUpgradeManager : MonoBehaviour
{
    private static CardUpgradeManager _instance;
    private GameObject _canvasObject;

    public static void ShowUpgradeScreen()
    {
        if (_instance != null) return; // Prevent multiple upgrade screens at once

        GameObject managerGo = new GameObject("CardUpgradeManager");
        _instance = managerGo.AddComponent<CardUpgradeManager>();
        _instance.OpenScreen();
    }

    private void OpenScreen()
    {
        // 1. Pause game
        Time.timeScale = 0f;
        Player.IsUIModeActive = true;

        // 2. Configure cursor
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // 3. Create UI
        CreateUpgradeUI();
    }

    private void CloseScreen()
    {
        // 1. Resume game
        Time.timeScale = 1f;
        Player.IsUIModeActive = false;

        // 2. Hide cursor
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = false;

        // 3. Cleanup
        if (_canvasObject != null)
        {
            Destroy(_canvasObject);
        }
        _instance = null;
        Destroy(gameObject);
    }

    private void CreateUpgradeUI()
    {
        // Try custom prefab first
        if (Player.Instance != null && Player.Instance.CardUpgradeCanvasPrefab != null)
        {
            if (SetupCustomUpgradeUI(Player.Instance.CardUpgradeCanvasPrefab))
            {
                return;
            }
        }

        // Programmatic Fallback UI
        _canvasObject = new GameObject("CardUpgradeCanvas");
        Canvas canvas = _canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 9999; // Topmost sorting order
        
        _canvasObject.AddComponent<CanvasScaler>();
        _canvasObject.AddComponent<GraphicRaycaster>();

        // Background dark overlay
        GameObject overlay = new GameObject("Overlay");
        overlay.transform.SetParent(_canvasObject.transform, false);
        Image overlayImage = overlay.AddComponent<Image>();
        overlayImage.color = new Color(0.02f, 0.02f, 0.02f, 0.88f); // 88% dark glass overlay
        
        RectTransform overlayRect = overlay.GetComponent<RectTransform>();
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;

        // Attach Egypt Futuristic Particle Emitter to overlay if enabled
        if (Player.Instance != null && Player.Instance.EnableUpgradeScreenVFX)
        {
            overlay.AddComponent<UIParticleEmitter>();
        }

        // Header Title Text
        GameObject titleGo = new GameObject("TitleText");
        titleGo.transform.SetParent(overlay.transform, false);
        TextMeshProUGUI titleText = titleGo.AddComponent<TextMeshProUGUI>();
        titleText.text = "SELECT AN UPGRADE";
        titleText.fontSize = 46;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.color = new Color(1f, 0.55f, 0f, 1f); // Sleek gold/orange
        titleText.fontStyle = FontStyles.Bold;
        
        RectTransform titleRect = titleGo.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 0.78f);
        titleRect.anchorMax = new Vector2(1f, 0.93f);
        titleRect.offsetMin = Vector2.zero;
        titleRect.offsetMax = Vector2.zero;

        // Cards Container Layout
        GameObject container = new GameObject("CardsContainer");
        container.transform.SetParent(overlay.transform, false);
        
        HorizontalLayoutGroup layout = container.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 50f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlHeight = true;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = true;
        layout.childForceExpandWidth = true;
        
        RectTransform containerRect = container.GetComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0.12f, 0.22f);
        containerRect.anchorMax = new Vector2(0.88f, 0.72f);
        containerRect.offsetMin = Vector2.zero;
        containerRect.offsetMax = Vector2.zero;

        List<RectTransform> cardsList = new List<RectTransform>();

        // 1. Life Card
        cardsList.Add(CreateCard(container.transform, "LIFE CARD", "+10 Max Health\nHeals instantly", () =>
        {
            Player.IncreaseMaxHealth(10);
        }));

        // 2. Damage Card
        cardsList.Add(CreateCard(container.transform, "DAMAGE CARD", "+10 Bullet Damage\nHeavy impact bullets", () =>
        {
            Player.IncreaseDamage(10f);
        }));

        // 3. Weapon or Speed Card
        if (Player.Instance != null && Player.Instance.HasRifleUpgradeAvailable())
        {
            cardsList.Add(CreateCard(container.transform, "WEAPON CARD", "Equip automatic\nRifle prototype", () =>
            {
                Player.Instance.EquipRifle();
            }));
        }
        else
        {
            cardsList.Add(CreateCard(container.transform, "SPEED CARD", "+20% Fire Rate\nShoot faster", () =>
            {
                if (Player.Instance != null && Player.Instance.Shooting != null)
                {
                    Player.Instance.Shooting.ModifyFireRate(1.2f);
                }
            }));
        }

        // Start Entry Animation
        StartCoroutine(AnimateCardsStaggered(cardsList.ToArray()));
    }

    private bool SetupCustomUpgradeUI(GameObject prefab)
    {
        _canvasObject = Instantiate(prefab);
        UpgradeCardButton[] cardButtons = _canvasObject.GetComponentsInChildren<UpgradeCardButton>();

        if (cardButtons == null || cardButtons.Length < 3)
        {
            Debug.LogWarning("[CardUpgradeManager] Custom Canvas prefab does not contain at least 3 UpgradeCardButton components. Falling back to default UI.");
            Destroy(_canvasObject);
            return false;
        }

        // Configure Life Card
        ConfigureCustomCard(cardButtons[0], "LIFE CARD", "+10 Max Health\nHeals instantly", () =>
        {
            Player.IncreaseMaxHealth(10);
        });

        // Configure Damage Card
        ConfigureCustomCard(cardButtons[1], "DAMAGE CARD", "+10 Bullet Damage\nHeavy impact bullets", () =>
        {
            Player.IncreaseDamage(10f);
        });

        // Configure Weapon or Speed Card
        if (Player.Instance != null && Player.Instance.HasRifleUpgradeAvailable())
        {
            ConfigureCustomCard(cardButtons[2], "WEAPON CARD", "Equip automatic\nRifle prototype", () =>
            {
                Player.Instance.EquipRifle();
            });
        }
        else
        {
            ConfigureCustomCard(cardButtons[2], "SPEED CARD", "+20% Fire Rate\nShoot faster", () =>
            {
                if (Player.Instance != null && Player.Instance.Shooting != null)
                {
                    Player.Instance.Shooting.ModifyFireRate(1.2f);
                }
            });
        }

        // Find parent container to attach particle emitter if enabled
        if (Player.Instance != null && Player.Instance.EnableUpgradeScreenVFX)
        {
            Transform overlayTransform = _canvasObject.transform.Find("Overlay");
            if (overlayTransform == null && _canvasObject.transform.childCount > 0)
            {
                overlayTransform = _canvasObject.transform.GetChild(0);
            }
            GameObject targetVfxParent = overlayTransform != null ? overlayTransform.gameObject : _canvasObject;
            targetVfxParent.AddComponent<UIParticleEmitter>();
        }

        // Gather RectTransforms for animation
        RectTransform[] cardRects = new RectTransform[3];
        for (int i = 0; i < 3; i++)
        {
            cardRects[i] = cardButtons[i].GetComponent<RectTransform>();
        }

        StartCoroutine(AnimateCardsStaggered(cardRects));
        return true;
    }

    private void ConfigureCustomCard(UpgradeCardButton card, string title, string description, System.Action onSelect)
    {
        if (card == null) return;

        if (card.TitleText != null) card.TitleText.text = title;
        if (card.DescriptionText != null) card.DescriptionText.text = description;

        if (card.Button != null)
        {
            card.Button.onClick.RemoveAllListeners();
            card.Button.onClick.AddListener(() =>
            {
                onSelect?.Invoke();
                CloseScreen();
            });

            // Automatically inject hover scaling and button audio components if they aren't configured
            if (card.GetComponent<CardHoverEffect>() == null)
            {
                card.gameObject.AddComponent<CardHoverEffect>();
            }
            if (card.GetComponent<UIButtonSound>() == null)
            {
                card.gameObject.AddComponent<UIButtonSound>();
            }
        }
    }

    private RectTransform CreateCard(Transform parent, string cardTitle, string description, System.Action onSelect)
    {
        GameObject cardGo = new GameObject("Card_" + cardTitle.Replace(" ", ""));
        cardGo.transform.SetParent(parent, false);

        RectTransform cardRect = cardGo.AddComponent<RectTransform>();

        // Add Image
        Image bgImage = cardGo.AddComponent<Image>();
        bgImage.color = new Color(0.08f, 0.08f, 0.08f, 1f); // Dark charcoal card body

        // Add Outline Border (orange/gold outline)
        Outline outline = cardGo.AddComponent<Outline>();
        outline.effectColor = new Color(1f, 0.55f, 0f, 0.6f);
        outline.effectDistance = new Vector2(3f, 3f);

        // Add Button
        Button button = cardGo.AddComponent<Button>();
        button.onClick.AddListener(() =>
        {
            onSelect?.Invoke();
            CloseScreen();
        });

        // Button Color Tint
        ColorBlock cb = button.colors;
        cb.normalColor = new Color(0.08f, 0.08f, 0.08f, 1f);
        cb.highlightedColor = new Color(0.18f, 0.18f, 0.18f, 1f);
        cb.pressedColor = new Color(0.25f, 0.25f, 0.25f, 1f);
        cb.selectedColor = new Color(0.08f, 0.08f, 0.08f, 1f);
        button.colors = cb;

        // Hover scale animation
        cardGo.AddComponent<CardHoverEffect>();

        // Sound triggers (FMOD UI buttons)
        cardGo.AddComponent<UIButtonSound>();

        // Card Vertical Layout
        VerticalLayoutGroup cardLayout = cardGo.AddComponent<VerticalLayoutGroup>();
        cardLayout.spacing = 25f;
        cardLayout.childAlignment = TextAnchor.MiddleCenter;
        cardLayout.childControlHeight = false;
        cardLayout.childControlWidth = false;
        cardLayout.childForceExpandHeight = false;
        cardLayout.childForceExpandWidth = false;
        cardLayout.padding = new RectOffset(20, 20, 30, 30);

        // Card Title Text
        GameObject titleGo = new GameObject("CardTitle");
        titleGo.transform.SetParent(cardGo.transform, false);
        TextMeshProUGUI titleTxt = titleGo.AddComponent<TextMeshProUGUI>();
        titleTxt.text = cardTitle;
        titleTxt.fontSize = 24;
        titleTxt.alignment = TextAlignmentOptions.Center;
        titleTxt.color = new Color(1f, 0.55f, 0f, 1f); // Gold/Orange Title
        titleTxt.fontStyle = FontStyles.Bold;
        
        RectTransform titleRect = titleGo.GetComponent<RectTransform>();
        titleRect.sizeDelta = new Vector2(240f, 40f);

        // Card Description Text
        GameObject descGo = new GameObject("CardDescription");
        descGo.transform.SetParent(cardGo.transform, false);
        TextMeshProUGUI descTxt = descGo.AddComponent<TextMeshProUGUI>();
        descTxt.text = description;
        descTxt.fontSize = 16;
        descTxt.alignment = TextAlignmentOptions.Center;
        descTxt.color = new Color(0.85f, 0.85f, 0.85f, 1f); // Off-white
        
        RectTransform descRect = descGo.GetComponent<RectTransform>();
        descRect.sizeDelta = new Vector2(240f, 100f);

        return cardRect;
    }

    private IEnumerator AnimateCardsStaggered(RectTransform[] cards)
    {
        float[] delays = new float[] { 0f, 0.12f, 0.24f };
        float duration = 0.45f;

        // Initialize scales to zero
        for (int i = 0; i < cards.Length; i++)
        {
            if (cards[i] != null) cards[i].localScale = Vector3.zero;
        }

        float startTime = Time.realtimeSinceStartup;
        bool allDone = false;

        while (!allDone)
        {
            allDone = true;
            float elapsed = Time.realtimeSinceStartup - startTime;

            for (int i = 0; i < cards.Length; i++)
            {
                if (cards[i] == null) continue;

                float cardTime = elapsed - delays[i];
                if (cardTime < 0f)
                {
                    cards[i].localScale = Vector3.zero;
                    allDone = false;
                }
                else if (cardTime < duration)
                {
                    float t = cardTime / duration;
                    cards[i].localScale = Vector3.one * EvaluateOvershoot(t);
                    allDone = false;
                }
                else
                {
                    cards[i].localScale = Vector3.one;
                }
            }

            yield return null;
        }
    }

    private float EvaluateOvershoot(float t)
    {
        // Cubic Back Ease Out: f(t) = 1 + c3 * (t - 1)^3 + c1 * (t - 1)^2
        float c1 = 1.70158f;
        float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
    }
}
