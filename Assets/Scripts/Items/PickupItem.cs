using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]
public class PickupItem : MonoBehaviour
{
    public enum PickupType { Health, Shield }
    
    [Header("Settings")]
    public PickupType type;
    public int amount = 25;
    
    [Header("Sound Effect")]
    [SerializeField] private string _pickupSoundEvent = "event:/UI/Select";

    [Header("Visual Effects")]
    [Tooltip("Przypnij tu dowolny komponent Trail Renderer, który sam automatycznie przestanie rysować smugę po uderzeniu w ziemię!")]
    [SerializeField] private TrailRenderer _trailRenderer;

    private bool _collected = false;
    private Rigidbody _rb;
    private Collider _col;

    private bool _canFreeze = false;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _col = GetComponent<Collider>();
        
        if (_rb != null) 
        {
            _rb.isKinematic = false;
            // Bardzo ważny element: Blokujemy rotację i obroty, dzięki temu Tarcza i Plusik
            // lecą równym ułożeniem bez koziołkowania, i wylądują też na płasko:
            _rb.constraints = RigidbodyConstraints.FreezeRotation; 
        }

        // WYBITNA ZMIANA: Przedmiot rodzi się jako DUCH (Trigger). 
        // Dzięki temu nie może przywalić głową w ścianę ani wieko skrzyni kiedy wyskakuje.
        if (_col != null)
        {
            _col.isTrigger = true;
        }
        
        StartCoroutine(GhostEscapeRoutine());
    }

    private IEnumerator GhostEscapeRoutine()
    {
        // 0.2 sekundy lotu w górę jako nieskazitelny Duch (przelatuje przez ścianki skrzyni)
        yield return new WaitForSeconds(0.2f);

        // Kiedy znajdzie się bezpiecznie na zewnątrz powierza, staje się twardy fizycznie, by opaść na dno.
        if (_col != null)
        {
            _col.isTrigger = false;
        }

        // Dajemy mu znać, że gdy dotknie po tym wszystkim podłogi - ma się zamrozić do zebrania.
        _canFreeze = true;

        // Awaryjne zamrożenie
        yield return new WaitForSeconds(5f);
        FreezeItem();
    }

    private void FreezeItem()
    {
        if (_rb != null)
        {
            _rb.linearVelocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
            _rb.isKinematic = true; 
        }
        
        if (_col != null)
        {
            _col.isTrigger = true;
        }

        if (_trailRenderer != null)
        {
            // Odetnij wysypywanie smugi precyzyjnie w momencie wylądowania na dno!
            _trailRenderer.emitting = false;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_collected) return;
        TryPickup(other.gameObject);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (_collected) return;

        bool isPlayer = collision.gameObject.CompareTag("Player") || collision.gameObject.GetComponentInParent<Player>() != null;

        if (isPlayer)
        {
            TryPickup(collision.gameObject);
        }
        else if (_canFreeze)
        {
            // Możemy zamrozić układ DOPIERO wtedy, kiedy wyleci ze skrzyni i bezpiecznie spadnie.
            FreezeItem();
        }
    }

    private void TryPickup(GameObject otherObj)
    {
        if (otherObj.CompareTag("Player") || otherObj.GetComponentInParent<Player>() != null)
        {
            _collected = true;

            if (type == PickupType.Health)
            {
                Player.Heal(amount);
            }
            else if (type == PickupType.Shield)
            {
                Player.AddShield(amount);
            }

            if (!string.IsNullOrEmpty(_pickupSoundEvent))
            {
                 FMODHelper.PlayOneShot(_pickupSoundEvent, transform.position);
            }
            
            Destroy(gameObject);
        }
    }
}
