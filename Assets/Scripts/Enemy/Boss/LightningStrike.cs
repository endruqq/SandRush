using UnityEngine;
using System.Collections;

public class LightningStrike : MonoBehaviour
{
    [Header("Ustawienia Obrażeń")]
    [SerializeField] private float _damage = 30f;
    [Tooltip("Promień rażenia pioruna wokół punktu uderzenia")]
    [SerializeField] private float _damageRadius = 2.5f;

    [Header("Czas i Opóźnienie")]
    [Tooltip("Czas przez jaki wyświetla się strefa (ostrzeżenie) zanim piorun uderzy")]
    [SerializeField] private float _warningTime = 1.25f;
    [Tooltip("Czas, po jakim całkowicie kasujemy Particle System błyskawicy! Dostosuj by nie ucinało chmury iskierek przed końcem!")]
    [SerializeField] private float _lightningVfxDuration = 2f;

    [Header("Efekty")]
    [Tooltip("Paczka (Prefab) z celownikiem pojawiającym się na początku na ziemi")]
    [SerializeField] private GameObject _warningVfxPrefab;
    [Tooltip("Właściwa paczka (Prefab) samego uderzenia pioruna")]
    [SerializeField] private GameObject _lightningVfxPrefab;
    [SerializeField] private string _strikeSoundEvent = "event:/Impacts/Lightning"; 

    private GameObject _spawnedWarningIndicator;

    private void Start()
    {
        // Wpierw na samym początku pojawia się Twój pierwszy prefab (z celownikiem)
        if (_warningVfxPrefab != null)
        {
            // Powołujemy go u samej podstawy, przypinając do tego skryptu jako dziecko by z nim "żył"
            _spawnedWarningIndicator = Instantiate(_warningVfxPrefab, transform.position, Quaternion.identity, transform);
        }
        
        StartCoroutine(StrikeRoutine());
    }

    private IEnumerator StrikeRoutine()
    {
        // 1. Oczekiwanie by zagrała się np. animacja Twojego celownika
        yield return new WaitForSeconds(_warningTime);

        // 2. Usunięcie ostrzeżenia (Gasimy pożar celownika)
        if (_spawnedWarningIndicator != null)
        {
            Destroy(_spawnedWarningIndicator); 
        }

        // 3. Właściwe uderzenie (VFX i dźwięk)
        if (_lightningVfxPrefab != null)
        {
            GameObject lightningInstance = Instantiate(_lightningVfxPrefab, transform.position, Quaternion.identity);
            
            // Usunięcie po dokładnie odliczonym czasie
            Destroy(lightningInstance, _lightningVfxDuration);
        }
        
        if (!string.IsNullOrEmpty(_strikeSoundEvent))
        {
            FMODHelper.PlayOneShot(_strikeSoundEvent, transform.position);
        }

        // 4. Detekcja obrażeń (Czy gracz stał w polu?)
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, _damageRadius);
        foreach (var col in hitColliders)
        {
            if (col.CompareTag("Player") || col.GetComponentInParent<Player>() != null)
            {
                // Odpychamy troszeczkę lub przekazujemy wektor dla efektu "odrzutu" kamery
                Vector3 pushDir = (col.transform.position - transform.position).normalized;
                pushDir.y = 0;
                
                Player.SetLastHitDirection(pushDir);
                Player.TakeDamage((int)_damage); // Aplikujemy Damage
            }
        }

        Player.TriggerHeavyCameraShake(); 

        Destroy(gameObject, 0.5f);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
        Gizmos.DrawSphere(transform.position, _damageRadius);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, _damageRadius);
    }
}
