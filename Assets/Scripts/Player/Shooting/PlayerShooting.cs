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
    private readonly Animator _weaponAnimator;
    private readonly string _recoilTrigger;

    public PlayerShooting(Transform playerTransform, Transform firePoint, GameObject bulletPrefab, ParticleSystem fireParticles, Animator weaponAnimator, string recoilTrigger)
    {
        _playerTransform = playerTransform;
        _firePoint = firePoint;
        _weapon = new BulletWeapon(_playerTransform, _firePoint, bulletPrefab);
        _firePointParticles = fireParticles;
        _weaponAnimator = weaponAnimator;
        _recoilTrigger = recoilTrigger;
    }

    public void ModifyFireRate(float multiplier)
    {
        // Lower is faster
        _fireRate /= multiplier;
    }

    public bool Tick(Vector3 aimDir)
    {
        float angle = Vector3.Angle(_playerTransform.forward, aimDir);
        if (angle > _maxShootAngle)
        {
            return false;
        }

        if (Input.GetMouseButton(0) && Time.time >= _nextFireTime)
        {
            _nextFireTime = Time.time + _fireRate;
            _weapon.Fire(aimDir);

            if (_firePointParticles != null) _firePointParticles.Play();
            if (_weaponAnimator != null) _weaponAnimator.SetTrigger(_recoilTrigger);
            return true;
        }
        return false;
    }

    public void FireImmediate(Vector3 aimDir)
    {
        _weapon.Fire(aimDir);
        if (_firePointParticles != null) _firePointParticles.Play();
        if (_weaponAnimator != null) _weaponAnimator.SetTrigger(_recoilTrigger);
    }

    public void EquipWeapon(GameObject bulletPrefab)
    {
        _weapon = new BulletWeapon(_playerTransform, _firePoint, bulletPrefab);
    }
}
