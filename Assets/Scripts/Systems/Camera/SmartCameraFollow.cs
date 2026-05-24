using UnityEngine;
using Unity.Cinemachine; // Cinemachine 3 namespace

public class SmartCameraFollow : MonoBehaviour
{
    [Header("Behavior Settings")]
    [Tooltip("0 = Camera stays on Player. 1 = Camera stays on Cursor. Recommended: 0.2 - 0.4.")]
    [Range(0f, 1f)]
    [SerializeField] private float _cursorInfluence = 0.3f;
    
    [Tooltip("Maximum distance the camera target can move away from the player.")]
    [SerializeField] private float _maxOffsetDistance = 5f;

    [Tooltip("How fast the camera target moves to the new position.")]
    [SerializeField] private float _smoothTime = 0.1f;

    [Header("Rotation Setup")]
    [Tooltip("Ile stopni obraca się kamera wokół gracza przy wciśnięciu klawisza Q? (90 = idealny rzut izometryczny co ścianę)")]
    [SerializeField] private float _rotationStep = 90f;
    [Tooltip("Jak szybko kamera obraca się na nową pozycję")]
    [SerializeField] private float _rotationSpeed = 7f;


    [Header("References")]
    [Tooltip("The Cinemachine Camera to control. Auto-found if empty.")]
    [SerializeField] private CinemachineCamera _virtualCamera; 
    
    // Internal state
    private Player _player;
    private Transform _targetObject;
    private Vector3 _currentVelocity;
    private float _targetYRotation;

    private void Start()
    {
        // Find references
        _player = FindFirstObjectByType<Player>();
        if (_virtualCamera == null) _virtualCamera = FindFirstObjectByType<CinemachineCamera>();

        if (_player == null)
        {
            Debug.LogError("[SmartCameraFollow] Player not found! disabling.");
            enabled = false;
            return;
        }

        // Create a hidden target object for the camera to follow
        GameObject targetInfo = new GameObject("SmartCameraTarget");
        _targetObject = targetInfo.transform;
        
        // Initial position matches player
        _targetObject.position = _player.transform.position;

        // Assign to Cinemachine
        if (_virtualCamera != null)
        {
            _virtualCamera.Follow = _targetObject;
            _targetYRotation = _virtualCamera.transform.eulerAngles.y; // Zczytujemy Twój obecny kąt!
            Debug.Log($"[SmartCameraFollow] Assigned camera follow to {_targetObject.name}");
        }
        else
        {
            Debug.LogWarning("[SmartCameraFollow] No CinemachineCamera found to assign!");
        }
    }

    private void Update()
    {
        if (_virtualCamera == null) return;

        // Skok co 90 stopni po wciśnięciu Q (w lewo) lub E (w prawo)
        if (Input.GetKeyDown(KeyCode.Q))
        {
            _targetYRotation += _rotationStep;
        }
        else if (Input.GetKeyDown(KeyCode.E))
        {
            _targetYRotation -= _rotationStep;
        }

        // Orbitowanie izometryczne polegające na obracaniu Y samej widzącej kamery 
        // (ponieważ Twój Cinemachine Position Composer ma Rotation = None)
        Vector3 currentEuler = _virtualCamera.transform.eulerAngles;
        Quaternion targetRotation = Quaternion.Euler(currentEuler.x, _targetYRotation, currentEuler.z);
        
        _virtualCamera.transform.rotation = Quaternion.Slerp(
            _virtualCamera.transform.rotation, 
            targetRotation, 
            Time.deltaTime * _rotationSpeed
        );
    }

    private void LateUpdate()
    {
        if (_player == null || _targetObject == null) return;

        // 1. Get Base Positions
        Vector3 playerPos = _player.transform.position;
        Vector3 cursorWorldPos = playerPos; 

        // 2. Get Cursor Position from Player's Aiming component
        if (_player.Aiming != null)
        {
            // AimPosition is on the "Ground" level usually
            cursorWorldPos = _player.Aiming.AimPosition;
            // Flatten Y to match player height if needed, assuming top-down 2D logic in 3D space
            cursorWorldPos.y = playerPos.y; 
        }

        // 3. Calculate Target Position (Weighted Average)
        Vector3 desiredPos = Vector3.Lerp(playerPos, cursorWorldPos, _cursorInfluence);

        // 4. Clamp Offset (Visualize a circle around player)
        Vector3 offset = desiredPos - playerPos;
        if (offset.magnitude > _maxOffsetDistance)
        {
            offset = offset.normalized * _maxOffsetDistance;
            desiredPos = playerPos + offset;
        }

        // 5. Smoothly Move Target
        _targetObject.position = Vector3.SmoothDamp(_targetObject.position, desiredPos, ref _currentVelocity, _smoothTime);
    }
}
