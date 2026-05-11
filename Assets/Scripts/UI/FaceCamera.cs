using UnityEngine;

public class FaceCamera : MonoBehaviour
{
    [Tooltip("Jeśli odznaczone, patrzy prosto w ekran (Kamerę). Jeśli zaznaczone, znak obraca się fizycznie śledząc model Gracza!")]
    [SerializeField] private bool _lookAtPlayer = false;
    
    [Tooltip("Włącza obrót tylko w osi Y (w lewo/prawo). Znak będzie stał zawsze prosto jak słup, zamiast pochylać się do góry/dołu.")]
    [SerializeField] private bool _lockRotationYOnly = true;

    private Camera _mainCamera;
    private Transform _playerTransform;

    private void Start()
    {
        _mainCamera = Camera.main;
        if (_mainCamera == null) _mainCamera = FindFirstObjectByType<Camera>();

        Player p = FindFirstObjectByType<Player>();
        if (p != null) _playerTransform = p.transform;
    }

    private void LateUpdate()
    {
        Vector3 targetPosition = Vector3.zero;

        if (_lookAtPlayer && _playerTransform != null)
        {
            targetPosition = _playerTransform.position;
        }
        else if (_mainCamera != null)
        {
            targetPosition = _mainCamera.transform.position;
        }
        else return;

        // Ponieważ Canvas (UI) domyślnie "patrzy" do tyłu swoją grafiką, musimy wektor odwrócić:
        Vector3 directionAwayFromTarget = transform.position - targetPosition;

        // Jeśli chcemy by stał prosto na ziemi:
        if (_lockRotationYOnly)
        {
            directionAwayFromTarget.y = 0;
        }

        // Zastosowanie obrotu prosto w cel!
        if (directionAwayFromTarget != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(directionAwayFromTarget);
        }
    }
}
