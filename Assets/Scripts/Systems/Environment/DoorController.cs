using UnityEngine;

public class DoorController : MonoBehaviour
{
    [SerializeField] private Animator _animator;
    [SerializeField] private string _openTriggerName = "Open";

    public void OpenDoor()
    {
        if (_animator != null)
        {
            _animator.SetTrigger(_openTriggerName);
            Debug.Log($"[DoorController] Opened: {gameObject.name}");
        }
        else
        {
            Debug.LogWarning("[DoorController] Animator not assigned!");
        }
    }
}
