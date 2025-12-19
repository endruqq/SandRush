using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;

public class EnemySpawner : MonoBehaviour
{
    public enum SpawnMode { Timer, WaveClear }

    [Header("Settings")]
    [SerializeField] private GameObject _enemyPrefab;
    [SerializeField] private float _spawnRadius = 5f;
    [SerializeField] private SpawnMode _spawnMode = SpawnMode.Timer;
    [SerializeField] private int _enemiesPerWave = 1;
    [SerializeField] private float _timeBetweenWaves = 10f;
    [Tooltip("Set to 0 for infinite waves.")]
    [SerializeField] private int _maxWaves = 0;
    [SerializeField] private bool _autoStart = false;

    private List<EnemyManager> _activeEnemies = new List<EnemyManager>();
    private bool _isSpawning = false;
    private int _wavesSpawned = 0;

    public bool IsCleared { get; private set; }
    public event System.Action OnSpawnerCleared;

    private void Start()
    {
        if (_autoStart)
        {
            StartSpawning();
        }
    }

    public void StartSpawning()
    {
        if (_isSpawning) return;
        _isSpawning = true;
        _wavesSpawned = 0;
        IsCleared = false;
        StartCoroutine(SpawnLoop());
    }

    private IEnumerator SpawnLoop()
    {
        while (_maxWaves <= 0 || _wavesSpawned < _maxWaves)
        {
            _wavesSpawned++;
            
            // Spawn Wave
            for (int i = 0; i < _enemiesPerWave; i++)
            {
                SpawnEnemy();
                yield return new WaitForSeconds(0.2f); // Slight stagger
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

        Vector3 spawnPos = transform.position;
        Vector3 randomPoint = transform.position + Random.insideUnitSphere * _spawnRadius;
        randomPoint.y = transform.position.y;

        if (NavMesh.SamplePosition(randomPoint, out NavMeshHit hit, _spawnRadius, NavMesh.AllAreas))
        {
            spawnPos = hit.position;
        }

        GameObject enemyObj = Instantiate(_enemyPrefab, spawnPos, Quaternion.identity);
        
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
