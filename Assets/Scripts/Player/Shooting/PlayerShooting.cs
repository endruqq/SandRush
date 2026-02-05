using UnityEngine;

public class PlayerShooting
{
    private readonly Transform _playerTransform;
    private readonly Transform _firePoint;
    private float _fireRate = 0.25f;
    private readonly float _bulletSpeed;
    private readonly GameObject[] _firePointVFXPrefabs;

    private float _nextFireTime = 0f;
    
    private readonly float _maxShootAngle = 60f;

    private WeaponBase _weapon;
    private readonly Animator _weaponAnimator;
    private readonly string _recoilTrigger;

    public PlayerShooting(Transform playerTransform, Transform firePoint, GameObject bulletPrefab, float bulletSpeed, GameObject[] fireVFXPrefabs, Animator weaponAnimator, string recoilTrigger)
    {
        _playerTransform = playerTransform;
        _firePoint = firePoint;
        _bulletSpeed = bulletSpeed;
        _weapon = new BulletWeapon(_playerTransform, _firePoint, bulletPrefab, _bulletSpeed);
        _firePointVFXPrefabs = fireVFXPrefabs;
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

            SpawnVFX();
            if (_weaponAnimator != null) _weaponAnimator.SetTrigger(_recoilTrigger);
            return true;
        }
        return false;
    }

    public void FireImmediate(Vector3 aimDir)
    {
        _weapon.Fire(aimDir);
        SpawnVFX();
        if (_weaponAnimator != null) _weaponAnimator.SetTrigger(_recoilTrigger);
    }

    private void SpawnVFX()
    {
        if (_firePointVFXPrefabs == null) return;
        
        foreach (var prefab in _firePointVFXPrefabs)
        {
            if (prefab == null) continue;
            
            // Instantiate at fire point position with corrected rotation (180 flip) and parent to firePoint
            Quaternion correctedRotation = _firePoint.rotation * Quaternion.Euler(0, 180, 0);
            GameObject vfxInstance = Object.Instantiate(prefab, _firePoint.position, correctedRotation, _firePoint);
            
            // Get root particle system and play with all children
            ParticleSystem rootPS = vfxInstance.GetComponent<ParticleSystem>();
            if (rootPS != null)
            {
                // Stop everything first, then play all together
                rootPS.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                rootPS.Play(true); // true = play all children simultaneously
            }
            else
            {
                // No root PS, play each child individually
                foreach (var ps in vfxInstance.GetComponentsInChildren<ParticleSystem>())
                {
                    ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                    ps.Play(true);
                }
            }
            
            // Calculate max duration for auto-destroy
            float maxDuration = 0f;
            foreach (var ps in vfxInstance.GetComponentsInChildren<ParticleSystem>())
            {
                float duration = ps.main.duration + ps.main.startLifetime.constantMax;
                if (duration > maxDuration) maxDuration = duration;
            }
            
            // Auto-destroy after VFX finishes (with small buffer)
            Object.Destroy(vfxInstance, maxDuration + 0.5f);
        }
    }

    public void EquipWeapon(GameObject bulletPrefab)
    {
        _weapon = new BulletWeapon(_playerTransform, _firePoint, bulletPrefab, _bulletSpeed);
    }
}
