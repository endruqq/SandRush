using UnityEngine;
using UnityEngine.Events;

public class TutorialTrigger : MonoBehaviour
{
    [Header("Content")]
    [SerializeField] private TutorialStep[] _steps;

    [Header("Actions")]
    [Tooltip("What happens after the player clicks OK on the LAST step?")]
    public UnityEvent OnTutorialClosed;

    private bool _hasTriggered = false;

    private void OnTriggerEnter(Collider other)
    {
        if (_hasTriggered) return;

        if (other.CompareTag("Player"))
        {
            _hasTriggered = true;
            
            if (TutorialManager.Instance != null)
            {
                TutorialManager.Instance.ShowTutorial(_steps, OnTutorialClosedCallback);
            }
            else
            {
                Debug.LogWarning("TutorialManager missing! Executing event immediately.");
                OnTutorialClosedCallback();
            }
        }
    }

    private void OnTutorialClosedCallback()
    {
        OnTutorialClosed?.Invoke();
        
        // Destroy the trigger so it doesn't happen again
        Destroy(gameObject);
    }
}
