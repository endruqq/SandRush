using UnityEngine;

public class TutorialFinishTrigger : MonoBehaviour
{
    private bool _triggered = false;

    private void OnTriggerEnter(Collider other)
    {
        if (_triggered) return;

        // Check if it's the player
        if (other.GetComponent<Player>() != null)
        {
            _triggered = true;
            
            // Mark tutorial as completed
            PlayerPrefs.SetInt(StartGameTutorial.PREF_TUTORIAL_COMPLETED, 1);
            PlayerPrefs.Save();
            
            Debug.Log("[TutorialFinishTrigger] Player entered trigger. Tutorial Marked as Completed!");
            
            // Optional: Disable this trigger so it doesn't fire again
            gameObject.SetActive(false);
        }
    }
}
