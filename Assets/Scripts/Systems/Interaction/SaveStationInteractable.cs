using UnityEngine;
using System.Collections;

public class SaveStationInteractable : MonoBehaviour, IInteractable
{
    [Header("UI Prompt")]
    [Tooltip("Rozpiska klawisza (np. E - Zapisz) z Canvasa, która ma się pokazywać obok")]
    [SerializeField] private GameObject _promptUI;
    [SerializeField] private float _promptShowDistance = 2f;

    [Header("Visual & Audio")]
    [Tooltip("Prefab odpalany po zapisaniu dla efektu potwierdzenia (np. rozbłysk iskierek)")]
    [SerializeField] private GameObject _saveEffectPrefab;
    [SerializeField] private string _saveSound = "event:/UI/SaveGame_Success";

    private bool _justSaved = false;
    private Transform _playerTransform;

    private void Start()
    {
        if (_promptUI != null) _promptUI.SetActive(false);
    }

    private void Update()
    {
        if (_promptUI == null) return;

        if (_justSaved)
        {
            if (_promptUI.activeSelf) _promptUI.SetActive(false);
            return;
        }

        if (_playerTransform == null)
        {
            Player p = FindFirstObjectByType<Player>();
            if (p != null) _playerTransform = p.transform;
            else return;
        }

        // System pojawiania UI przy podejściu do stacji
        float dist = Vector3.Distance(transform.position, _playerTransform.position);
        bool shouldShow = dist <= _promptShowDistance;

        if (_promptUI.activeSelf != shouldShow)
        {
            _promptUI.SetActive(shouldShow);
        }
    }

    // Wywołane, gdy gracz wejdzie w interakcję (wciśnie 'E' tak samo jak dla przycisków mapy)
    public void Interact(Player player)
    {
        if (_justSaved) return;

        // Oznaczamy w globalnym sejvie, że posiadamy fizyczny nowy punkt kontrolny (Checkpoint Station)
        PlayerPrefs.SetInt("HasCustomSave", 1);

        // Zapisujemy idealnie koordynaty z tej maszyny/obiektu by zrespinić się dokładnie w niej
        PlayerPrefs.SetFloat("RespawnPosX", transform.position.x);
        PlayerPrefs.SetFloat("RespawnPosY", transform.position.y);
        PlayerPrefs.SetFloat("RespawnPosZ", transform.position.z);
        
        PlayerPrefs.Save();

        Debug.Log($"[SaveStation] Gra pomyślnie zapisana. Nowy punkt Odrodzenia: {transform.position}");

        // Feedback
        if (_saveEffectPrefab != null)
        {
            Instantiate(_saveEffectPrefab, transform.position, Quaternion.identity);
        }

        if (!string.IsNullOrEmpty(_saveSound))
        {
            FMODHelper.PlayOneShot(_saveSound, transform.position);
        }

        // Błysk ekranu (akceptacja zapisu)
        if (ScreenFlash.Instance != null)
        {
            ScreenFlash.Instance.Flash(0.1f, 0.25f); 
        }

        StartCoroutine(SaveCooldownRoutine());
    }

    private IEnumerator SaveCooldownRoutine()
    {
        _justSaved = true;
        // Odczekujemy by gracz nie spamił przycisku tysiąc razy zbijając zapis
        yield return new WaitForSeconds(5f); 
        _justSaved = false;
    }
}
