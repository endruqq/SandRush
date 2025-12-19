using UnityEngine;

public class Spike : MonoBehaviour
{
    [SerializeField] private int _damage = 25;

    private bool _hasActivated = false;

    public void OnTriggerEnter(Collider other)
    {
        if (_hasActivated) return;

        if (other.CompareTag("Player"))
        {
            Player.TakeDamage(_damage);

            _hasActivated = true;
            Destroy(gameObject, 0.1f);
        }
    }
}
