using UnityEngine;

/// <summary>
/// Singleton bullet pool shared by all enemies.
/// Place this on a GameObject in the scene.
/// </summary>
public class EnemyBulletPool : MonoBehaviour
{
    public static EnemyBulletPool Instance { get; private set; }
    
    [SerializeField] private GameObject _bulletPrefab;
    [SerializeField] private int _poolSize = 50;
    
    private ObjectPool<Bullet> _pool;
    
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        
        if (_bulletPrefab != null)
        {
            Bullet bulletComp = _bulletPrefab.GetComponent<Bullet>();
            if (bulletComp != null)
            {
                _pool = new ObjectPool<Bullet>(bulletComp, _poolSize, transform);
            }
        }
    }
    
    /// <summary>
    /// Get a bullet from the pool and fire it.
    /// </summary>
    public void FireBullet(Vector3 position, Vector3 direction, float speed, float damage = 25f, Transform owner = null)
    {
        if (_pool == null)
        {
            Debug.LogWarning("EnemyBulletPool: Pool not initialized. Check if bullet prefab is assigned.");
            return;
        }
        
        Bullet bullet = _pool.GetObject();
        bullet.transform.SetPositionAndRotation(position, Quaternion.LookRotation(direction));
        bullet.Init(_pool);
        bullet.SetDamage(damage); 
        bullet.SetOwner(owner); // Set owner transform
        bullet.Fire(direction, speed);
    }
}
