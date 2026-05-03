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

    private RectTransform _panelRect;
    private Vector2 _targetAnchorPos;
    private Vector2 _hiddenAnchorPos;
    private Coroutine _animationRoutine;

    private Action _onCloseCallback;
    private bool _isTutorialActive;
    
    private TutorialStep[] _currentSteps;
    private int _currentStepIndex;

    private float _actionTimer = 0f;
    private bool _waitingForSpawner = false;
    private EnemySpawner _currentSpawner;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (_tutorialPanel != null)
        {
            _panelRect = _tutorialPanel.GetComponent<RectTransform>();
            if (_panelRect != null)
            {
                _targetAnchorPos = _panelRect.anchoredPosition;
                // Move off-screen to the left
                _hiddenAnchorPos = new Vector2(-Screen.width * 2f, _targetAnchorPos.y);
            }
            _tutorialPanel.SetActive(false);
        }
    }

    public void ShowTutorial(TutorialStep[] steps, Action onClosed)
    {
        if (_isTutorialActive || steps == null || steps.Length == 0) return;
        
        _isTutorialActive = true;
        _onCloseCallback = onClosed;
        _currentSteps = steps;
        _currentStepIndex = 0;
        _actionTimer = 0f;
        _waitingForSpawner = false;

        if (_tutorialPanel != null)
        {
            _tutorialPanel.SetActive(true);
            if (_panelRect != null)
            {
                _panelRect.anchoredPosition = _hiddenAnchorPos;
            }
        }

        ShowStep(_currentStepIndex);
    }

    private void ShowStep(int index)
    {
        if (index < 0 || index >= _currentSteps.Length) return;

        TutorialStep step = _currentSteps[index];

        if (_contentTextField != null) _contentTextField.text = step.Text;
        if (_contentImageField != null)
        {
            _contentImageField.sprite = step.Image;
            _contentImageField.gameObject.SetActive(step.Image != null);
        }
        
        _actionTimer = 0f;
        _waitingForSpawner = false;

        // Perform specific init logic based on action type
        if (step.ActionType == TutorialActionType.KillEnemies)
        {
            if (step.Spawner != null)
            {
                _currentSpawner = step.Spawner;
                _waitingForSpawner = true;
                _currentSpawner.OnSpawnerCleared += OnSpawnerClearedHandler;
                _currentSpawner.StartSpawning();
            }
            else
            {
                Debug.LogWarning("TutorialManager: KillEnemies step has no spawner assigned! Skipping.");
                AdvanceStep();
                return;
            }
        }
        else if (step.ActionType == TutorialActionType.SelectMask)
        {
            Player player = FindFirstObjectByType<Player>();
            if (player != null)
            {
                player.OnMaskChanged += OnMaskSelectedHandler;
            }
            else
            {
                Debug.LogWarning("TutorialManager: Could not find Player for SelectMask step! Skipping.");
                AdvanceStep();
                return;
            }
        }

        Debug.Log($"TutorialManager: Showing Step {index + 1}/{_currentSteps.Length} - {step.ActionType}");

        // Slide in
        if (_panelRect != null && _tutorialPanel != null && _tutorialPanel.activeSelf)
        {
            if (_animationRoutine != null) StopCoroutine(_animationRoutine);
            _animationRoutine = StartCoroutine(AnimatePanelRoutine(_targetAnchorPos, null));
        }
    }

    private void Update()
    {
        if (!_isTutorialActive) return;

        TutorialStep currentStep = _currentSteps[_currentStepIndex];

        switch (currentStep.ActionType)
        {
            case TutorialActionType.Move:
                // Check if user is pressing movement keys (WASD)
                if (Mathf.Abs(Input.GetAxisRaw("Horizontal")) > 0.1f || Mathf.Abs(Input.GetAxisRaw("Vertical")) > 0.1f)
                {
                    _actionTimer += Time.deltaTime;
                    // Require 1 second of moving to avoid accidental touches clearing the task
                    if (_actionTimer >= 1f) 
                    {
                        AdvanceStep();
                    }
                }
                break;

            case TutorialActionType.Shoot:
                // Check for primary fire
                if (Input.GetMouseButtonDown(0))
                {
                    AdvanceStep();
                }
                break;

            case TutorialActionType.KillEnemies:
                // Relies on event callback `OnSpawnerClearedHandler` 
                break;

            case TutorialActionType.Custom:
                // Check custom actions manually
                break;
        }
    }

    private void OnSpawnerClearedHandler()
    {
        if (_currentSpawner != null)
        {
            _currentSpawner.OnSpawnerCleared -= OnSpawnerClearedHandler;
            _currentSpawner = null;
        }
        
        if (_isTutorialActive && _waitingForSpawner && _currentSteps[_currentStepIndex].ActionType == TutorialActionType.KillEnemies)
        {
            _waitingForSpawner = false;
            AdvanceStep();
        }
    }

    private void OnMaskSelectedHandler(Sprite _)
    {
        Player player = FindFirstObjectByType<Player>();
        if (player != null)
        {
            player.OnMaskChanged -= OnMaskSelectedHandler;
        }

        if (_isTutorialActive && _currentSteps[_currentStepIndex].ActionType == TutorialActionType.SelectMask)
        {
            AdvanceStep();
        }
    }

    public void AdvanceStep()
    {
        if (!_isTutorialActive) return;

        if (_currentSpawner != null)
        {
            _currentSpawner.OnSpawnerCleared -= OnSpawnerClearedHandler;
            _currentSpawner = null;
        }

        Player player = FindFirstObjectByType<Player>();
        if (player != null)
        {
            player.OnMaskChanged -= OnMaskSelectedHandler;
        }

        _actionTimer = 0f;

        // Animate out before progressing
        if (_panelRect != null && _tutorialPanel.activeSelf)
        {
            if (_animationRoutine != null) StopCoroutine(_animationRoutine);
            _animationRoutine = StartCoroutine(AnimatePanelRoutine(_hiddenAnchorPos, OnStepHidden));
        }
        else
        {
            OnStepHidden();
        }
    }

    private void OnStepHidden()
    {
        _currentStepIndex++;

        if (_currentStepIndex < _currentSteps.Length)
        {
            ShowStep(_currentStepIndex);
        }
        else
        {
            // If it's the last step, finish it setup
            OnCloseAnimationComplete();
        }
    }

    private void CloseTutorial() // Can be called externally to force close
    {
        if (_panelRect != null && _tutorialPanel.activeSelf)
        {
            if (_animationRoutine != null) StopCoroutine(_animationRoutine);
            _animationRoutine = StartCoroutine(AnimatePanelRoutine(_hiddenAnchorPos, OnCloseAnimationComplete));
        }
        else
        {
            OnCloseAnimationComplete();
        }
    }

    private void OnCloseAnimationComplete()
    {
        _isTutorialActive = false;
        _currentSteps = null;

        if (_tutorialPanel != null) _tutorialPanel.SetActive(false);

        _onCloseCallback?.Invoke();
        _onCloseCallback = null;
    }

    private System.Collections.IEnumerator AnimatePanelRoutine(Vector2 targetPos, Action onComplete)
    {
        float timer = 0f;
        Vector2 startPos = _panelRect.anchoredPosition;
        float duration = 0.35f;

        while (timer < duration)
        {
            timer += Time.deltaTime; 
            float t = timer / duration;
            t = Mathf.SmoothStep(0f, 1f, t);
            
            _panelRect.anchoredPosition = Vector2.Lerp(startPos, targetPos, t);
            yield return null;
        }

        _panelRect.anchoredPosition = targetPos;
        onComplete?.Invoke();
    }
}

public enum TutorialActionType 
{ 
    Move, 
    Shoot, 
    KillEnemies, 
    SelectMask,
    Custom 
}

[System.Serializable]
public struct TutorialStep
{
    public TutorialActionType ActionType;
    [TextArea(3, 10)] public string Text;
    public Sprite Image;
    public EnemySpawner Spawner;
}
