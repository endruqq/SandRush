using UnityEngine;

public class BulletWeapon : WeaponBase
{
    private readonly Transform _firePoint;
    private readonly float _bulletSpeed;
    private readonly ObjectPool<Bullet> _bulletPool;
    private readonly ObjectPool<Transform> _hitEffectPool;

    public BulletWeapon(Transform owner, Transform firePoint, GameObject bulletPrefab, float speed, int poolSize = 20) : base(owner)
    {
        _bulletSpeed = speed;
        _firePoint = firePoint;

        Bullet bulletComp = bulletPrefab.GetComponent<Bullet>();
        _bulletPool = new ObjectPool<Bullet>(bulletComp, poolSize);

        if (bulletComp != null && bulletComp.HitEffectPrefab != null)
        {
            Transform hitFxPrefab = bulletComp.HitEffectPrefab.transform;
            _hitEffectPool = new ObjectPool<Transform>(hitFxPrefab, poolSize);
        }
    }

    public override void Fire(Vector3 direction)
    {
        Bullet bullet = _bulletPool.GetObject();

        bullet.transform.SetPositionAndRotation(_firePoint.position, Quaternion.LookRotation(direction));
        bullet.Init(_bulletPool, _hitEffectPool);
        bullet.SetOwner(Owner); // Prevents self-damage using Transform
        bullet.Fire(direction, _bulletSpeed);
    }
}
