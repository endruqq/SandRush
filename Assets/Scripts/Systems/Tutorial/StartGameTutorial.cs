using UnityEngine;
using UnityEngine.Events;
using System.Collections;


using System.Collections.Generic;

public class StartGameTutorial : MonoBehaviour
{
    [Header("Spawn Settings")]
    [SerializeField] private Transform _spawnPoint; // Intro spawn point
    [SerializeField] private Transform _gameplaySpawnPoint; // Checkpoint spawn point (after tutorial)
    [SerializeField] private GameObject _portalVFXPrefab;
    [SerializeField] private float _spawnDelay = 1.0f; // Time before player appears
    [SerializeField] private float _portalDuration = 2.5f; // Total time before tutorial starts

    [Header("Tutorial Content")]
    [SerializeField] private TutorialStep[] _steps;

    [Header("Actions")]
    [Tooltip("Drag WaveManager.StartAllSpawners here")]
    public UnityEvent OnTutorialClosed;

    // Keys for PlayerPrefs
    public const string PREF_TUTORIAL_COMPLETED = "TutorialCompleted";
    public const string PREF_MASK_COLLECTED = "MaskCollected";

    private Player _player;
    private PlayerShooting _shooting;
    private CharacterController _controller;
    private Renderer[] _playerRenderers;

    private void Start()
    {
        bool isTutorialDone = PlayerPrefs.GetInt(PREF_TUTORIAL_COMPLETED, 0) == 1;
        Debug.Log($"[StartGameTutorial] Started! TutorialCompleted: {isTutorialDone}");
        
        if (isTutorialDone)
        {
            // Czy nadpisaliśmy checkpoint z poziomu Obozu/Stacji Zapisu (SaveStationInteractable)?
            if (PlayerPrefs.GetInt("HasCustomSave", 0) == 1)
            {
                Vector3 savedPos = new Vector3(
                    PlayerPrefs.GetFloat("RespawnPosX"),
                    PlayerPrefs.GetFloat("RespawnPosY"),
                    PlayerPrefs.GetFloat("RespawnPosZ")
                );

                // Tworzy ułotny ułamek sprawna (celownik) do którego przyciągnięty zostanie gracz
                GameObject tempSpawn = new GameObject("Loaded_Custom_SavePoint");
                tempSpawn.transform.position = savedPos;
                tempSpawn.transform.rotation = Quaternion.identity;

                StartCoroutine(PlaySpawnSequence(tempSpawn.transform, false));
            }
            else
            {
                // Domyślny główny Checkpoint przed wejściem na pustynie
                StartCoroutine(PlaySpawnSequence(_gameplaySpawnPoint, false));
            }
        }
        else
        {
            // Intro Spawn (Tutorial)
            StartCoroutine(PlaySpawnSequence(_spawnPoint, true));
        }
    }

    private IEnumerator PlaySpawnSequence(Transform targetSpawn, bool showTutorial)
    {
        Debug.Log("[StartGameTutorial] Looking for Player...");
        
        // 1. Find Player & IMMEDIATELY Hide
        while (_player == null)
        {
            _player = FindFirstObjectByType<Player>();
            if (_player != null)
            {
                // Cache components
                _shooting = _player.Shooting;
                _controller = _player.GetComponent<CharacterController>();
                _playerRenderers = _player.GetComponentsInChildren<Renderer>();
                
                // Hide Visuals + Lock Input ASAP (fixes glitch)
                SetPlayerControls(false);
                SetPlayerVisuals(false);
                break; // Found! Exit loop immediately to avoid 1 frame delay
            }
            yield return null;
        }

        // 2. Teleport to correct Spawn Point
        if (targetSpawn != null)
        {
            _controller.enabled = false; // Disable CC to allow teleport
            _player.transform.position = targetSpawn.position;
            _player.transform.rotation = targetSpawn.rotation;
            Physics.SyncTransforms();
            _controller.enabled = true; // Re-enable CC (but controls still locked)
            Debug.Log($"[StartGameTutorial] Player Teleported to: {targetSpawn.name}");
        }

        // 3. Equip Mask if previously collected
        if (PlayerPrefs.GetInt(PREF_MASK_COLLECTED, 0) == 1)
        {
            _player.EquipMask();
            Debug.Log("[StartGameTutorial] Mask Auto-Equipped from Save.");
        }

        // 4. Spawn Portal VFX (Always play portal effect!)
        if (_portalVFXPrefab != null && targetSpawn != null)
        {
            Instantiate(_portalVFXPrefab, targetSpawn.position, targetSpawn.rotation);
        }

        // 5. Wait for visual spawn moment
        yield return new WaitForSeconds(_spawnDelay);

        // 6. Reveal Player (Visuals Only)
        SetPlayerVisuals(true);
        Debug.Log("[StartGameTutorial] Player Revealed.");

        // 7. Wait for Portal Animation to finish
        yield return new WaitForSeconds(_portalDuration - _spawnDelay);

        // 8. Trigger Next Step (Tutorial vs Gameplay)
        if (showTutorial)
        {
            Debug.Log("[StartGameTutorial] Starting Tutorial...");
            
            // Unlock controls so the player can actually do the tutorial tasks!
            SetPlayerControls(true);

            if (TutorialManager.Instance != null && _steps.Length > 0)
            {
                TutorialManager.Instance.ShowTutorial(_steps, OnTutorialFinished);
            }
            else
            {
                OnTutorialFinished(); // Fallback if missing
            }
        }
        else
        {
            // Just start gameplay
            Debug.Log("[StartGameTutorial] Checkpoint Spawn Complete. Starting Gameplay.");
            OnTutorialFinished(); 
        }
    }

    private void OnTutorialFinished()
    {
        // Unlock controls and start game logic
        SetPlayerControls(true);
        OnTutorialClosed?.Invoke();
    }

    private void SetPlayerControls(bool isActive)
    {
        if (_player != null) _player.enabled = isActive;
        if (_shooting != null) _shooting.IsEnabled = isActive;
        if (_controller != null) _controller.enabled = isActive;
    }

    private void SetPlayerVisuals(bool isVisible)
    {
        if (_playerRenderers != null)
        {
            foreach (var r in _playerRenderers)
            {
                r.enabled = isVisible;
            }
        }
    }
}
