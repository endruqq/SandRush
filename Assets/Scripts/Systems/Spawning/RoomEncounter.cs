using UnityEngine;
using System.Collections.Generic;

public class RoomEncounter : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private List<EnemySpawner> _spawners;
    [SerializeField] private List<GameObject> _doors;
    [SerializeField] private bool _startOnEnter = true;

    private bool _hasStarted = false;
    private bool _isCompleted = false;

    private void Start()
    {
        // Ensure doors are active at start
        foreach (var door in _doors)
        {
            if(door != null) door.SetActive(true);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_hasStarted || _isCompleted) return;

        if (other.CompareTag("Player"))
        {
            StartEncounter();
        }
    }

    public void StartEncounter()
    {
        _hasStarted = true;
        Debug.Log("Room Encounter Started!");

        foreach (var spawner in _spawners)
        {
            if (spawner != null)
            {
                spawner.OnSpawnerCleared += CheckCompletion;
                
                if (_startOnEnter)
                {
                    spawner.StartSpawning();
                }
            }
        }
        
        // Initial check just in case spawners were already done (unlikely but safe)
        CheckCompletion();
    }

    private void CheckCompletion()
    {
        if (_isCompleted) return;

        bool allCleared = true;
        foreach (var spawner in _spawners)
        {
            if (spawner != null && !spawner.IsCleared)
            {
                allCleared = false;
                break;
            }
        }

        if (allCleared)
        {
            CompleteEncounter();
        }
    }

    private void CompleteEncounter()
    {
        _isCompleted = true;
        Debug.Log("Room Encounter Completed! Opening doors.");

        foreach (var door in _doors)
        {
            if (door != null)
            {
                door.SetActive(false);
            }
        }

        // Cleanup events
        foreach (var spawner in _spawners)
        {
            if (spawner != null)
            {
                spawner.OnSpawnerCleared -= CheckCompletion;
            }
        }
    }
}
