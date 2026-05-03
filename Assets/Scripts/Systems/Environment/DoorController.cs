using UnityEngine;

public class DoorController : MonoBehaviour
{
    [SerializeField] private Animator _animator;
    [SerializeField] private string _openTriggerName = "Open";

    [Header("FMOD Sound")]
    [SerializeField] private string _doorSound = "event:/Door_Open_Small";

    public void OpenDoor()
    {
        if (_animator != null)
        {
            _animator.SetTrigger(_openTriggerName);

            // Play door sound
            if (!string.IsNullOrEmpty(_doorSound))
                FMODHelper.PlayOneShot(_doorSound, transform.position);

            Debug.Log($"[DoorController] Opened: {gameObject.name}");
        }
        else
        {
            Debug.LogWarning("[DoorController] Animator not assigned!");
        }
    }
}
