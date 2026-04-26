using UnityEngine;

public class ExplosiveBarrel : MonoBehaviour
{
    [Header("Stats")]
    [SerializeField] private float _health = 20f;
    [SerializeField] private float _explosionDamage = 100f;
    [SerializeField] private float _explosionRadius = 6f;
    [SerializeField] private float _explosionForce = 10f;
    
    [Header("Visuals")]
    [SerializeField] private GameObject[] _explosionVFXs;

    [Header("FMOD Sound")]
    [SerializeField] private string _explosionSound = "event:/Explosions";

    private bool _exploded = false;

    // Called by Bullet via SendMessage
    public void TakeDamage(float amount)
    {
        if (_exploded) return;

        _health -= amount;
        if (_health <= 0)
        {
            Explode();
        }
    }

    private void Explode()
    {
        _exploded = true;

        // Play explosion sound
        if (!string.IsNullOrEmpty(_explosionSound))
            FMODHelper.PlayOneShot(_explosionSound, transform.position);

        if (_explosionVFXs != null && _explosionVFXs.Length > 0)
        {
            foreach (var vfx in _explosionVFXs)
            {
                if (vfx != null)
                {
                    Instantiate(vfx, transform.position, vfx.transform.rotation);
                }
            }
        }

        Collider[] hits = Physics.OverlapSphere(transform.position, _explosionRadius);
        foreach (var hit in hits)
        {
            if (hit.gameObject == gameObject) continue;

            // Damage functionality
            EnemyManager enemy = hit.GetComponentInParent<EnemyManager>();
            if (enemy != null)
            {
                enemy.TakeDamage(_explosionDamage);
            }
            else
            {
                hit.SendMessage("TakeDamage", _explosionDamage, SendMessageOptions.DontRequireReceiver);
            }

            // Physics force
            Rigidbody rb = hit.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.AddExplosionForce(_explosionForce, transform.position, _explosionRadius);
            }
        }

        Destroy(gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, _explosionRadius);
    }
}
