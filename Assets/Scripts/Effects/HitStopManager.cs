using UnityEngine;
using System.Collections;

public class HitStopManager : MonoBehaviour
{
    public static HitStopManager Instance { get; private set; }

    private bool _isWaiting = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Freezes the game for a specified duration in real time.
    /// Used to add weight and satisfaction to combat impacts.
    /// </summary>
    /// <param name="duration">How long the freeze lasts in seconds.</param>
    public void TriggerHitStop(float duration = 0.05f)
    {
        if (_isWaiting) return;
        
        // Prevent Time.timeScale from getting permanently stuck at 0 if called constantly
        Time.timeScale = 0f;
        StartCoroutine(WaitRoutine(duration));
    }

    /// <summary>
    /// Slows down time for a specified duration in real time.
    /// </summary>
    /// <param name="duration">How long the slow-mo lasts in real seconds.</param>
    /// <param name="timeScale">The time scale to set (e.g. 0.3 for 30% speed).</param>
    public void TriggerSlowMo(float duration = 0.5f, float timeScale = 0.3f)
    {
        if (_isWaiting) return;
        
        Time.timeScale = timeScale;
        StartCoroutine(WaitRoutine(duration));
    }

    private IEnumerator WaitRoutine(float duration)
    {
        _isWaiting = true;
        
        // Use Realtime so it ignores the Time.timeScale = 0
        yield return new WaitForSecondsRealtime(duration);
        
        // Restore time scale
        Time.timeScale = 1f;
        _isWaiting = false;
    }
}
