using UnityEngine;
using System.Collections.Generic;

public class WaveManager : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private List<EnemySpawner> _spawners;
    [SerializeField] private bool _autoStart = true;

    private void Start()
    {
        if (_autoStart)
        {
            StartAllSpawners();
        }
    }

    public void StartAllSpawners()
    {
        if (_spawners == null) return;

        foreach (var spawner in _spawners)
        {
            if(spawner != null)
                spawner.StartSpawning();
        }
        
        Debug.Log("WaveManager: All spawners started.");
    }
}
