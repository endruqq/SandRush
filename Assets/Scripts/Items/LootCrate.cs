using UnityEngine;

public class LootCrate : MonoBehaviour
{
    [Header("Skrzynka")]
    [SerializeField] private float _health = 20f;
    [Tooltip("Jeżeli masz wersję zniszczoną skrzynki, przyporządkuj go tutaj w celu podmiany przy zniszczeniu")]
    [SerializeField] private GameObject _openCrateModel;
    [Tooltip("Przypisz tu model zamkniętej skrzyni (zniknie po jej otwarciu)")]
    [SerializeField] private GameObject _closedCrateModel;

    [Header("Loot")]
    [SerializeField] private GameObject _healthPrefab; // Prefab apteczki (plusika)
    [SerializeField] private GameObject _shieldPrefab; // Prefab tarczy
    [SerializeField] private int _healthItemsCount = 1;
    [SerializeField] private int _shieldItemsCount = 1;

    [Header("Ejection Physics / Rzut do góry")]
    [SerializeField] private float _upwardForce = 6f;
    [SerializeField] private float _forwardForce = 2f;
    [SerializeField] private float _spread = 1.5f;

    [Header("FMOD Dźwięk / Efekty")]
    [SerializeField] private string _breakSoundEvent = "event:/Impacts/Wood_Break";
    [SerializeField] private GameObject _breakVFX;

    private bool _isOpened = false;

    public void TakeDamage(float amount)
    {
        Debug.Log($"[LootCrate] Otrzymano {amount} obrazen! (Crate: {gameObject.name})");
        if (_isOpened) return;

        _health -= amount;
        Debug.Log($"[LootCrate] HP spadło do: {_health}");
        if (_health <= 0)
        {
            Debug.Log($"[LootCrate] HP <= 0, Otwieram skrzynkę!");
            OpenCrate();
        }
    }

    private void OpenCrate()
    {
        _isOpened = true;

        if (!string.IsNullOrEmpty(_breakSoundEvent))
            FMODHelper.PlayOneShot(_breakSoundEvent, transform.position);

        if (_breakVFX != null)
        {
            Instantiate(_breakVFX, transform.position, Quaternion.identity);
        }

        // Zmień model
        if (_closedCrateModel) _closedCrateModel.SetActive(false);
        if (_openCrateModel) _openCrateModel.SetActive(true);

        // Wyłączamy kolizję skrzyni - ewentualnie zostawiamy kolizję tła/modelu
        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        // Pojawienie loot'u
        SpawnItems(_healthPrefab, _healthItemsCount);
        SpawnItems(_shieldPrefab, _shieldItemsCount);
        
        // Zniszczenie skrzyni (odkomentuj, jesli nie podpinasz _openCrateModel i wolisz żeby od razu zniknęła)
        // Destroy(gameObject, 0.1f);
    }

    private void SpawnItems(GameObject prefab, int count)
    {
        if (prefab == null) return;

        for (int i = 0; i < count; i++)
        {
            // Pojawiamy delikatnie wyżej, żeby zminimalizować przycięcie się w podłodze
            GameObject item = Instantiate(prefab, transform.position + Vector3.up * 0.8f, Quaternion.identity);
            Rigidbody rb = item.GetComponent<Rigidbody>();
            if (rb != null)
            {
                // Określamy kierunek fizyczny po Instantiate
                Vector3 forwardEject = transform.forward * _forwardForce;
                Vector3 upwardEject = Vector3.up * _upwardForce;
                Vector3 randomSpread = new Vector3(
                    Random.Range(-_spread, _spread),
                    0,
                    Random.Range(-_spread, _spread)
                );

                Vector3 finalForce = forwardEject + upwardEject + randomSpread;
                rb.AddForce(finalForce, ForceMode.Impulse);
                
                // Losowa rotacja podczas lotu dla lepszego wyglądu
                rb.AddTorque(Random.insideUnitSphere * 4f, ForceMode.Impulse);
            }
        }
    }
}
