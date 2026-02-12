using UnityEngine;
using System;

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
    
    // --- Ammo System ---
    private readonly int _magazineSize;
    private int _currentAmmo;
    private readonly float _reloadTime;
    private float _reloadTimer;
    private bool _isReloading;
    
    /// <summary>
    /// Event fired when ammo count changes. Parameters: currentAmmo, maxAmmo
    /// </summary>
    public event Action<int, int> OnAmmoChanged;
    
    /// <summary>
    /// Event fired when reload starts/ends. Parameter: isReloading
    /// </summary>
    public event Action<bool> OnReloadStateChanged;

    /// <summary>
    /// Event fired when a shot is successfully fired.
    /// </summary>
    public event Action OnShoot;

    public int CurrentAmmo => _currentAmmo;
    public int MagazineSize => _magazineSize;
    public bool IsReloading => _isReloading;
    public bool IsEnabled { get; set; } = true;

    public PlayerShooting(Transform playerTransform, Transform firePoint, GameObject bulletPrefab, float bulletSpeed, GameObject[] fireVFXPrefabs, Animator weaponAnimator, string recoilTrigger, int magazineSize = 25, float reloadTime = 1.5f)
    {
        _playerTransform = playerTransform;
        _firePoint = firePoint;
        _bulletSpeed = bulletSpeed;
        _weapon = new BulletWeapon(_playerTransform, _firePoint, bulletPrefab, _bulletSpeed);
        _firePointVFXPrefabs = fireVFXPrefabs;
        _weaponAnimator = weaponAnimator;
        _recoilTrigger = recoilTrigger;
        
        // Initialize ammo
        _magazineSize = magazineSize;
        _reloadTime = reloadTime;
        _currentAmmo = _magazineSize;
    }

    public void ModifyFireRate(float multiplier)
    {
        // Lower is faster
        _fireRate /= multiplier;
    }

    public bool Tick(Vector3 aimDir)
    {
        if (!IsEnabled) return false;

        // Handle reload input
        if (Input.GetKeyDown(KeyCode.R) && !_isReloading && _currentAmmo < _magazineSize)
        {
            StartReload();
        }
        
        // Handle reload timer
        if (_isReloading)
        {
            _reloadTimer -= Time.deltaTime;
            if (_reloadTimer <= 0)
            {
                FinishReload();
            }
            return false; // Can't shoot while reloading
        }
        
        float angle = Vector3.Angle(_playerTransform.forward, aimDir);
        if (angle > _maxShootAngle)
        {
            return false;
        }

        // Check if can shoot - SEMI-AUTO (GetMouseButtonDown)
        if (Input.GetMouseButtonDown(0) && Time.time >= _nextFireTime && _currentAmmo > 0)
        {
            _nextFireTime = Time.time + _fireRate;
            _weapon.Fire(aimDir);
            
            // Play gunshot sound
            FMODHelper.PlayOneShot("event:/Gun_Shot_Player", _firePoint.position);
            
            // Consume ammo
            _currentAmmo--;
            OnAmmoChanged?.Invoke(_currentAmmo, _magazineSize);
            OnShoot?.Invoke();

            SpawnVFX();
            if (_weaponAnimator != null) _weaponAnimator.SetTrigger(_recoilTrigger);
            
            // Auto-reload when empty
            if (_currentAmmo <= 0)
            {
                StartReload();
            }
            
            return true;
        }
        return false;
    }
    
    private void StartReload()
    {
        _isReloading = true;
        _reloadTimer = _reloadTime;
        OnReloadStateChanged?.Invoke(true);
        
        // Play reload sound
        FMODHelper.PlayOneShot("event:/Gun_Auto_Reload", _playerTransform.position);
    }
    
    private void FinishReload()
    {
        _currentAmmo = _magazineSize;
        _isReloading = false;
        OnReloadStateChanged?.Invoke(false);
        OnAmmoChanged?.Invoke(_currentAmmo, _magazineSize);
    }

    public void FireImmediate(Vector3 aimDir)
    {
        if (_currentAmmo <= 0 || _isReloading) return;
        
        _weapon.Fire(aimDir);
        
        // Play gunshot sound
        FMODHelper.PlayOneShot("event:/Gun_Shot_Player", _firePoint.position);
        
        _currentAmmo--;
        OnAmmoChanged?.Invoke(_currentAmmo, _magazineSize);
        OnShoot?.Invoke();

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
            GameObject vfxInstance = UnityEngine.Object.Instantiate(prefab, _firePoint.position, correctedRotation, _firePoint);
            
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
            UnityEngine.Object.Destroy(vfxInstance, maxDuration + 0.5f);
        }
    }

    public void EquipWeapon(GameObject bulletPrefab)
    {
        _weapon = new BulletWeapon(_playerTransform, _firePoint, bulletPrefab, _bulletSpeed);
    }
}
