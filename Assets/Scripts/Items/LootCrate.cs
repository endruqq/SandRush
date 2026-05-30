using UnityEngine;

public class LootCrate : MonoBehaviour
{
    [Header("Skrzynka")]
    [SerializeField] private float _health = 20f;
    [Tooltip("Jeżeli masz wersję zniszczoną skrzynki, przyporządkuj go tutaj w celu podmiany przy zniszczeniu")]
    [SerializeField] private GameObject _openCrateModel;
    [Tooltip("Przypisz tu model zamkniętej skrzyni (zniknie po jej otwarciu)")]
    [SerializeField] private GameObject _closedCrateModel;
    [Tooltip("Przypisz element daszka skrzynki do obracania przy otwieraniu")]
    [SerializeField] private Transform _crateLid;

    [Header("Loot")]
    [SerializeField] private GameObject _healthPrefab; // Prefab apteczki (plusika)
    [SerializeField] private GameObject _shieldPrefab; // Prefab tarczy
    [SerializeField] private int _healthItemsCount = 1;
    [SerializeField] private int _shieldItemsCount = 1;

    [Header("Ejection Target Point (Najprostsza opcja!)")]
    [Tooltip("Stwórz pusty obiekt, połóż go celowo tam gdzie chcesz żeby wylądował Loot. Przypisz go tutaj, a skrzynia zignoruje kierunki i celnie strzeli idealnie w ten obiekt!")]
    [SerializeField] private Transform _landingTarget;

    [Header("Ejection Physics (Skonfiguruj jeśli brakuje Landing Target)")]
    [SerializeField] private float _upwardForce = 6f;
    [SerializeField] private float _forwardForce = 2f;
    [SerializeField] private float _spread = 1.5f;
    [Tooltip("Jeśli domyślny przód wyrzutu celuje tam gdzie nie chcesz (bo model skrzyni jest np. zapisany tyłem), zmień tę wartość na np. Z: -1 ")]
    [SerializeField] private Vector3 _ejectionDirection = new Vector3(0, 0, 1);

    [Header("Ejection Offsets (Doprecyzowanie)")]
    [Tooltip("Precyzyjne ustawienie z jakiego punktu wylatuje loot (względem skrzyni)")]
    [SerializeField] private Vector3 _spawnOffset = new Vector3(0, 1.2f, 0);
    [Tooltip("Ustawienie podłogi (np -0.5, względem pivota skrzyni) jeśli nie używasz w ogóle Landing Target")]
    [SerializeField] private float _groundLevelOffset = 0f;

    [Header("FMOD Dźwięk / Efekty")]
    [SerializeField] private string _breakSoundEvent = "event:/Impacts/Wood_Break";
    [SerializeField] private GameObject _breakVFX;

    private bool _isOpened = false;

    private string GetCrateUniqueKey()
    {
        return $"Crate_{UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}_{gameObject.name}_{transform.position.x:F1}_{transform.position.y:F1}_{transform.position.z:F1}";
    }

    private void Start()
    {
        string key = GetCrateUniqueKey();
        if (PlayerPrefs.GetInt(key, 0) == 1)
        {
            _isOpened = true;
            if (_closedCrateModel != null) _closedCrateModel.SetActive(false);
            if (_openCrateModel != null) _openCrateModel.SetActive(true);
            
            if (_crateLid != null)
            {
                Vector3 rot = _crateLid.localEulerAngles;
                rot.x = 4.171f; // Target open angle
                _crateLid.localEulerAngles = rot;
            }
            
            Collider col = GetComponent<Collider>();
            if (col != null) col.enabled = false;
        }
        else
        {
            if (_crateLid != null)
            {
                Vector3 rot = _crateLid.localEulerAngles;
                rot.x = -55.82f;
                _crateLid.localEulerAngles = rot;
            }
        }
    }

    public static void ResetOpenedCrates()
    {
        string openedCratesList = PlayerPrefs.GetString("OpenedCratesList", "");
        if (!string.IsNullOrEmpty(openedCratesList))
        {
            string[] keys = openedCratesList.Split(',');
            foreach (string key in keys)
            {
                PlayerPrefs.DeleteKey(key);
            }
        }
        PlayerPrefs.DeleteKey("OpenedCratesList");
        PlayerPrefs.Save();
        Debug.Log("[LootCrate] Opened crates reset.");
    }

    private System.Collections.IEnumerator RotateLidCoroutine()
    {
        if (_crateLid == null) yield break;

        float elapsed = 0f;
        float duration = 0.5f;
        float startX = -55.82f;
        float targetX = 4.171f;

        Vector3 rot = _crateLid.localEulerAngles;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float smoothT = Mathf.Sin(t * Mathf.PI * 0.5f);
            float currentX = Mathf.Lerp(startX, targetX, smoothT);
            rot.x = currentX;
            _crateLid.localEulerAngles = rot;
            yield return null;
        }

        rot.x = targetX;
        _crateLid.localEulerAngles = rot;
    }

    public void TakeDamage(float amount)
    {
        if (_isOpened) return;

        _health -= amount;
        if (_health <= 0)
        {
            OpenCrate();
        }
    }

    private void OpenCrate()
    {
        _isOpened = true;

        string key = GetCrateUniqueKey();
        PlayerPrefs.SetInt(key, 1);
        
        string openedCratesList = PlayerPrefs.GetString("OpenedCratesList", "");
        if (!openedCratesList.Contains(key))
        {
            openedCratesList = string.IsNullOrEmpty(openedCratesList) ? key : openedCratesList + "," + key;
            PlayerPrefs.SetString("OpenedCratesList", openedCratesList);
        }
        PlayerPrefs.Save();

        if (!string.IsNullOrEmpty(_breakSoundEvent))
            FMODHelper.PlayOneShot(_breakSoundEvent, transform.position);

        if (_breakVFX != null)
        {
            Instantiate(_breakVFX, transform.position, Quaternion.identity);
        }

        // Animate the lid if assigned, otherwise fall back to model swapping
        if (_crateLid != null)
        {
            StartCoroutine(RotateLidCoroutine());
        }
        else
        {
            if (_closedCrateModel) _closedCrateModel.SetActive(false);
            if (_openCrateModel) _openCrateModel.SetActive(true);
        }

        // Wyłączamy kolizję skrzyni
        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        // Trigger card selection UI instead of spawning physical items
        CardUpgradeManager.ShowUpgradeScreen();
    }

    private void SpawnItems(GameObject prefab, int count, System.Collections.Generic.List<Collider> spawnedList)
    {
        if (prefab == null) return;

        for (int i = 0; i < count; i++)
        {
            // Dodajemy drobny miks pozycji startowej, żeby nie rodziły się w 100% zespawane ze sobą
            Vector3 randomOffset = new Vector3(Random.Range(-0.3f, 0.3f), 0, Random.Range(-0.3f, 0.3f));
            
            // Punkt początkowy wykorzystujący Twój własny offset wraz z kątem rotacji samej skrzyni!
            Vector3 spawnPoint = transform.position + (transform.rotation * _spawnOffset);

            // Spawniejemy na ustalonym wyżej punkcie
            GameObject item = Instantiate(prefab, spawnPoint + randomOffset, Quaternion.identity);
            
            Collider itemCollider = item.GetComponent<Collider>();
            if (itemCollider != null)
            {
                spawnedList.Add(itemCollider);
            }

            Rigidbody rb = item.GetComponent<Rigidbody>();
            if (rb != null)
            {
                Vector3 finalForce = Vector3.zero;

                // Jeżeli gracz zdefiniował precyzyjny TARGET - wyliczamy wzór celujący samemu!
                if (_landingTarget != null)
                {
                    float gravity = Mathf.Abs(Physics.gravity.y);
                    if (gravity == 0) gravity = 9.81f;
                    float h = spawnPoint.y - _landingTarget.position.y;
                    if (h < 0) h = 0f;
                    
                    float a = 0.5f * gravity;
                    float b = -_upwardForce;
                    float c = -h;
                    float delta = b * b - 4 * a * c;
                    
                    if (delta >= 0)
                    {
                        float t = (-b + Mathf.Sqrt(delta)) / (2f * a);
                        Vector3 toTarget = _landingTarget.position - spawnPoint;
                        toTarget.y = 0; // Pomijamy różnicę wysokości dla rzutu horyzontalnego
                        Vector3 horizontalVel = toTarget / t;
                        
                        Vector3 rSpread = new Vector3(Random.Range(-_spread, _spread), 0, Random.Range(-_spread, _spread));
                        finalForce = horizontalVel + (Vector3.up * _upwardForce) + rSpread;
                    }
                }
                else 
                {
                    // Wyrzut matematyczny po ustalonych wpisanymi osiami (strzał w ciemno ze starej metody)
                    Vector3 throwDir = transform.TransformDirection(_ejectionDirection.normalized);
                    Vector3 forwardEject = throwDir * _forwardForce;
                    Vector3 upwardEject = Vector3.up * _upwardForce;
                    Vector3 rSpread = new Vector3(Random.Range(-_spread, _spread), 0, Random.Range(-_spread, _spread));

                     finalForce = forwardEject + upwardEject + rSpread;
                }

                // ForceMode.VelocityChange w ogóle ignoruje masę elementu
                rb.AddForce(finalForce, ForceMode.VelocityChange);
            }
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        float gravity = Mathf.Abs(Physics.gravity.y);
        if (gravity == 0) gravity = 9.81f;

        float vy = _upwardForce;
        Vector3 spawnWorldPos = transform.position + (transform.rotation * _spawnOffset);

        if (_landingTarget != null)
        {
            // === WIZUALIZACJA 1: TARGET (DYNAMIKA) ===
            float h = spawnWorldPos.y - _landingTarget.position.y;
            if (h < 0) h = 0f; 
            
            float a = 0.5f * gravity;
            float b = -vy;
            float c = -h;
            
            float delta = b * b - 4 * a * c;
            if (delta >= 0)
            {
                float t = (-b + Mathf.Sqrt(delta)) / (2f * a);
                
                float spreadRadius = _spread * t * 1.41f;

                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(spawnWorldPos, _landingTarget.position);

                Gizmos.color = Color.cyan;
                Gizmos.DrawSphere(spawnWorldPos, 0.08f);

                UnityEditor.Handles.color = new Color(0f, 1f, 0.4f, 0.8f);
                UnityEditor.Handles.DrawWireDisc(_landingTarget.position, Vector3.up, spreadRadius);
                
                Gizmos.color = new Color(0f, 1f, 0.4f, 0.15f);
                Gizmos.DrawSphere(_landingTarget.position, spreadRadius);
            }
        }
        else
        {
            // === WIZUALIZACJA 2: TRADYCYJNA ===
            float groundWorldY = transform.position.y + _groundLevelOffset;
            float h = spawnWorldPos.y - groundWorldY;
            if (h < 0) h = 0f; 
            
            float a = 0.5f * gravity;
            float b = -vy;
            float c = -h;
            
            float delta = b * b - 4 * a * c;
            if (delta >= 0)
            {
                float t = (-b + Mathf.Sqrt(delta)) / (2f * a);
                
                Vector3 throwDir = transform.TransformDirection(_ejectionDirection.normalized);
                Vector3 forwardDist = throwDir * (_forwardForce * t);
                
                Vector3 landingCenter = spawnWorldPos + forwardDist;
                landingCenter.y = groundWorldY;
                
                float spreadRadius = _spread * t * 1.41f; 

                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(spawnWorldPos, landingCenter);

                Gizmos.color = Color.cyan;
                Gizmos.DrawSphere(spawnWorldPos, 0.08f);

                UnityEditor.Handles.color = new Color(0f, 1f, 0.4f, 0.8f);
                UnityEditor.Handles.DrawWireDisc(landingCenter, Vector3.up, spreadRadius);
                
                Gizmos.color = new Color(0f, 1f, 0.4f, 0.15f);
                Gizmos.DrawSphere(landingCenter, spreadRadius);
            }
        }
    }
#endif
}
