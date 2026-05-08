using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;

public class EnemySpawner : MonoBehaviour
{
    public enum SpawnMode { Timer, WaveClear }
    public enum SpawnPositionMode { Random, Center, CustomPoints }

    [Header("Settings")]
    [SerializeField] private GameObject _enemyPrefab;
    [SerializeField] private SpawnMode _spawnMode = SpawnMode.Timer;
    [SerializeField] private int _enemiesPerWave = 1;
    [SerializeField] private float _timeBetweenWaves = 10f;
    [Tooltip("Set to 0 for infinite waves.")]
    [SerializeField] private int _maxWaves = 0;
    [SerializeField] private bool _autoStart = false;
    
    [Header("Spawn Position")]
    [SerializeField] private SpawnPositionMode _spawnPositionMode = SpawnPositionMode.Random;
    [Tooltip("Radius for random spawning")]
    [SerializeField] private float _spawnRadius = 5f;
    [Tooltip("Custom spawn points (used when mode is CustomPoints)")]
    [SerializeField] private Transform[] _customSpawnPoints;
    
    [Header("Portal VFX")]
    [Tooltip("VFX prefab to spawn before each enemy appears")]
    [SerializeField] private GameObject _portalVFXPrefab;
    [Tooltip("Delay between portal appearing and enemy spawning")]
    [SerializeField] private float _portalSpawnDelay = 0.5f;
    [Tooltip("How long the portal VFX stays before being destroyed")]
    [SerializeField] private float _portalLifetime = 2f;

    [Header("FMOD Sound")]
    [SerializeField] private string _spawnSound = "event:/Drone_Spawn_Tutorial";

    [Header("Boss Trigger (Opcjonalne)")]
    [Tooltip("Wybierz bossa z mapy do którego przypięty jest spawner. Po osiągnięciu progu HP spawner wywoła zgraję minionów!")]
    [SerializeField] private BossController _bossTrigger;
    [Tooltip("Odpali spawner gdy HP Bossa spadnie <= procent (np 0.5 to 50%)")]
    [SerializeField] private float _bossHealthThreshold = 0.5f;
    private bool _triggeredByBoss = false;

    private List<EnemyManager> _activeEnemies = new List<EnemyManager>();
    private bool _isSpawning = false;
    private int _wavesSpawned = 0;
    private int _customSpawnIndex = 0;

    public bool IsCleared { get; private set; }
    public event System.Action OnSpawnerCleared;
    
    private Player _cachedPlayer;

    private void Start()
    {
        if (_autoStart && _bossTrigger == null)
        {
            StartSpawning();
        }
    }

    private void Update()
    {
        // Jeżeli przypisano bossa, Spawner działa jako "Posiłki w fazie 2"
        if (_bossTrigger != null && !_isSpawning && !_triggeredByBoss)
        {
            if (_bossTrigger.HealthPercent > 0 && _bossTrigger.HealthPercent <= _bossHealthThreshold)
            {
                _triggeredByBoss = true;
                StartSpawning();
            }
        }
    }

    public void StartSpawning()
    {
        if (_isSpawning) return;
        _isSpawning = true;
        _wavesSpawned = 0;
        _customSpawnIndex = 0;
        IsCleared = false;
        StartCoroutine(SpawnLoop());
    }

    private IEnumerator SpawnLoop()
    {
        while (_maxWaves <= 0 || _wavesSpawned < _maxWaves)
        {
            _wavesSpawned++;
            
            // Spawn Wave
            int enemiesToSpawn = _enemiesPerWave;
            for (int i = 0; i < enemiesToSpawn; i++)
            {
                SpawnEnemy();
                yield return new WaitForSeconds(0.2f); // Slight stagger
            }
            
            // Wait for portal delay to ensure all enemies are actually spawned
            if (_portalVFXPrefab != null)
            {
                yield return new WaitForSeconds(_portalSpawnDelay + 0.1f);
            }

            if (_spawnMode == SpawnMode.WaveClear)
            {
                // Wait until all enemies are dead or inactive
                yield return new WaitUntil(() => _activeEnemies.Count == 0);
                // Optional delay after clearing wave before next one
                yield return new WaitForSeconds(2f); 
            }
            else // Timer
            {
                yield return new WaitForSeconds(_timeBetweenWaves);
            }
        }

        // All waves spawned. Now wait for cleanup (if any exist).
        CheckCompletion();
    }

    public void SpawnEnemy()
    {
        if (_enemyPrefab == null) return;

        Vector3 spawnPos = GetSpawnPosition();

        // Start spawn with portal effect
        StartCoroutine(SpawnEnemyWithPortal(spawnPos));
    }
    
    private Vector3 GetSpawnPosition()
    {
        switch (_spawnPositionMode)
        {
            case SpawnPositionMode.Center:
                return transform.position;
                
            case SpawnPositionMode.CustomPoints:
                if (_customSpawnPoints != null && _customSpawnPoints.Length > 0)
                {
                    // Cycle through custom points
                    Vector3 pos = _customSpawnPoints[_customSpawnIndex].position;
                    _customSpawnIndex = (_customSpawnIndex + 1) % _customSpawnPoints.Length;
                    return pos;
                }
                return transform.position; // Fallback to center
                
            case SpawnPositionMode.Random:
            default:
                Vector3 randomPoint = transform.position + Random.insideUnitSphere * _spawnRadius;
                randomPoint.y = transform.position.y;

                if (NavMesh.SamplePosition(randomPoint, out NavMeshHit hit, _spawnRadius, NavMesh.AllAreas))
                {
                    return hit.position;
                }
                return transform.position;
        }
    }
    
    private IEnumerator SpawnEnemyWithPortal(Vector3 spawnPos)
    {
        // Spawn portal VFX first
        if (_portalVFXPrefab != null)
        {
            GameObject portal = Instantiate(_portalVFXPrefab, spawnPos, Quaternion.identity);
            Destroy(portal, _portalLifetime);
            
            // Play spawn sound (but mute it if it's the very start of the level to prevent 50 spawners deafening the player)
            if (!string.IsNullOrEmpty(_spawnSound) && Time.timeSinceLevelLoad > 1f)
                FMODHelper.PlayOneShot(_spawnSound, spawnPos);
            
            // Wait for portal to appear before spawning enemy
            yield return new WaitForSeconds(_portalSpawnDelay);
        }
        
        // Calculate rotation towards player
        Quaternion spawnRotation = Quaternion.identity;
        if (_cachedPlayer == null) _cachedPlayer = FindFirstObjectByType<Player>(); // Retry find if null
        
        if (_cachedPlayer != null)
        {
            Vector3 direction = (_cachedPlayer.transform.position - spawnPos);
            direction.y = 0; // Keep horizontal
            if (direction.sqrMagnitude > 0.01f)
            {
                spawnRotation = Quaternion.LookRotation(direction);
            }
        }

        // Now spawn the enemy
        GameObject enemyObj = Instantiate(_enemyPrefab, spawnPos, spawnRotation);
        
        // Force NavMeshAgent to proper position
        if (enemyObj.TryGetComponent<NavMeshAgent>(out var navAgent))
        {
            navAgent.Warp(spawnPos);
            Debug.Log($"[EnemySpawner] Warped {enemyObj.name} to {spawnPos}");
        }
        
        if (enemyObj.TryGetComponent<EnemyManager>(out var manager))
        {
            manager.Initialize(this);
            _activeEnemies.Add(manager);
        }
    }

    public void OnEnemyDied(EnemyManager enemy)
    {
        if (_activeEnemies.Contains(enemy))
        {
            _activeEnemies.Remove(enemy);
        }
        CheckCompletion();
    }

    private void CheckCompletion()
    {
        // Only consider cleared if we finish ALL valid waves (and not infinite) AND empty list
        bool finishedSpawning = _maxWaves > 0 && _wavesSpawned >= _maxWaves;
        
        if (finishedSpawning && _activeEnemies.Count == 0 && !IsCleared)
        {
            IsCleared = true;
            Debug.Log($"Spawner {name} Cleared!");
            OnSpawnerCleared?.Invoke();
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = _spawnMode == SpawnMode.WaveClear ? Color.blue : Color.red;
        Gizmos.DrawWireSphere(transform.position, _spawnRadius);
    }
}
