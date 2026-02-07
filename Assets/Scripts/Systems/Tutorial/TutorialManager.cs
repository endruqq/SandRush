using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private GameObject _tutorialPanel;
    [SerializeField] private TextMeshProUGUI _contentTextField;
    [SerializeField] private Image _contentImageField;
    [SerializeField] private Button _mainButton;
    [SerializeField] private TextMeshProUGUI _mainButtonText;

    private Action _onCloseCallback;
    private bool _isTutorialActive;
    
    private TutorialStep[] _currentSteps;
    private int _currentStepIndex;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (_tutorialPanel != null) _tutorialPanel.SetActive(false);
        
        if (_mainButton != null)
        {
            _mainButton.onClick.AddListener(OnMainButtonClicked);
        }
        else
        {
            Debug.LogError("TutorialManager: Main Button is NOT assigned in Inspector!");
        }
    }

    public void ShowTutorial(TutorialStep[] steps, Action onClosed)
    {
        if (_isTutorialActive || steps == null || steps.Length == 0) return;
        
        _isTutorialActive = true;
        _onCloseCallback = onClosed;
        _currentSteps = steps;
        _currentStepIndex = 0;

        ShowStep(_currentStepIndex);
        
        // Show Panel
        if (_tutorialPanel != null) _tutorialPanel.SetActive(true);
        if (_mainButton != null) _mainButton.gameObject.SetActive(true);

        // Pause Game
        Time.timeScale = 0f;

        // Unlock Cursor
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    private void ShowStep(int index)
    {
        if (index < 0 || index >= _currentSteps.Length) return;

        TutorialStep step = _currentSteps[index];

        // Update Content
        if (_contentTextField != null) _contentTextField.text = step.Text;
        if (_contentImageField != null)
        {
            _contentImageField.sprite = step.Image;
            _contentImageField.gameObject.SetActive(step.Image != null);
        }

        // Update Button Text
        if (_mainButtonText != null)
        {
            bool isLastStep = index == _currentSteps.Length - 1;
            _mainButtonText.text = isLastStep ? "OK" : "Next";
        }
        
        Debug.Log($"TutorialManager: Showing Step {index + 1}/{_currentSteps.Length}");
    }

    private void OnMainButtonClicked()
    {
        Debug.Log("TutorialManager: Main Button Clicked!");
        _currentStepIndex++;

        if (_currentStepIndex < _currentSteps.Length)
        {
            ShowStep(_currentStepIndex);
        }
        else
        {
            CloseTutorial();
        }
    }

    private void CloseTutorial()
    {
        _isTutorialActive = false;
        _currentSteps = null;

        // Hide Panel
        if (_tutorialPanel != null) _tutorialPanel.SetActive(false);
        if (_mainButton != null) _mainButton.gameObject.SetActive(false);

        // Resume Game
        Time.timeScale = 1f;

        // Reset Cursor (Top-Down Shooter Style)
        // Cursor should be invisible but NOT locked, so mouse can move freely on screen for raycasting
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.None; // Changed from Locked to None

        // Trigger callback
        _onCloseCallback?.Invoke();
        _onCloseCallback = null;
    }
}

[System.Serializable]
public struct TutorialStep
{
    [TextArea(3, 10)] public string Text;
    public Sprite Image;
}
