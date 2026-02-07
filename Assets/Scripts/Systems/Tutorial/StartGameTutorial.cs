using UnityEngine;
using UnityEngine.Events;
using System.Collections;

public class StartGameTutorial : MonoBehaviour
{
    [Header("Tutorial Content")]
    [SerializeField] private TutorialStep[] _steps;

    [Header("Actions")]
    [Tooltip("Drag WaveManager.StartAllSpawners here")]
    public UnityEvent OnTutorialClosed;

    [Header("Detection Settings")]
    [Tooltip("If empty, searches for child with Tag 'Weapon'")]
    [SerializeField] private string _weaponChildName = "Weapon";

    private void Start()
    {
        StartCoroutine(CheckForWeaponRoutine());
    }

    private IEnumerator CheckForWeaponRoutine()
    {
        // Wait for player initialization
        yield return new WaitForSeconds(0.2f); // Small buffer

        Player player = FindFirstObjectByType<Player>();
        if (player != null)
        {
            // 1. Check by Name
            Transform weaponTransform = null;
            if (!string.IsNullOrEmpty(_weaponChildName))
            {
                weaponTransform = player.transform.Find(_weaponChildName); 
                if (weaponTransform == null) weaponTransform = FindDeepChild(player.transform, _weaponChildName);
            }

            // 2. Check by Tag (Fallback)
            if (weaponTransform == null)
            {
                foreach (Transform child in player.transform.GetComponentsInChildren<Transform>(true))
                {
                    if (child.CompareTag("Weapon"))
                    {
                        weaponTransform = child;
                        break;
                    }
                }
            }

            // If found and active...
            if (weaponTransform != null && weaponTransform.gameObject.activeInHierarchy)
            {
                // Weapon found! Show tutorial.
                if (TutorialManager.Instance != null)
                {
                    TutorialManager.Instance.ShowTutorial(_steps, () => {
                        OnTutorialClosed?.Invoke();
                    });
                }
            }
        }
    }
    
    private Transform FindDeepChild(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name == name) return child;
            Transform result = FindDeepChild(child, name);
            if (result != null) return result;
        }
        return null;
    }
}
