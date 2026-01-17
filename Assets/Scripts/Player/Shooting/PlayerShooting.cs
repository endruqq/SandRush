using UnityEngine;

public class PlayerShooting
{
    private readonly Transform _playerTransform;
    private readonly Transform _firePoint;
    private float _fireRate = 0.25f;
    private readonly ParticleSystem _firePointParticles;

    private float _nextFireTime = 0f;
    
    private readonly float _maxShootAngle = 60f;

    private WeaponBase _weapon;

    public PlayerShooting(Transform playerTransform, Transform firePoint, GameObject bulletPrefab, ParticleSystem fireParticles)
    {
        _playerTransform = playerTransform;
        _firePoint = firePoint;
        _weapon = new BulletWeapon(_playerTransform, _firePoint, bulletPrefab);
        _firePointParticles = fireParticles;
    }

    public void ModifyFireRate(float multiplier)
    {
        // Lower is faster
        _fireRate /= multiplier;
    }

    public void Tick(Vector3 aimDir)
    {
        float angle = Vector3.Angle(_playerTransform.forward, aimDir);
        if (angle > _maxShootAngle)
        {
            return;
        }

        if (Input.GetMouseButton(0) && Time.time >= _nextFireTime)
        {
            _nextFireTime = Time.time + _fireRate;
            _weapon.Fire(aimDir);

            if (_firePointParticles != null) _firePointParticles.Play();
        }
    }

    public void FireImmediate(Vector3 aimDir)
    {
        _weapon.Fire(aimDir);
        if (_firePointParticles != null) _firePointParticles.Play();
    }

    public void EquipWeapon(GameObject bulletPrefab)
    {
        _weapon = new BulletWeapon(_playerTransform, _firePoint, bulletPrefab);
    }
}
