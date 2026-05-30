using UnityEngine;
using System;

public class PlayerShooting
{
    private readonly Transform _playerTransform;
    private readonly Transform _firePoint;
    private float _fireRate = 0.25f;
    private float _bulletSpeed;
    private GameObject[] _firePointVFXPrefabs;
    private bool _isAutomatic;

    private float _nextFireTime = 0f;
    
    private readonly float _maxShootAngle = 60f;

    private WeaponBase _weapon;
    private Animator _weaponAnimator;
    private string _recoilTrigger;
    private System.Collections.Generic.List<ObjectPool<Transform>> _vfxPools;
    
    // --- Ammo System ---
    private int _magazineSize;
    private int _currentAmmo;
    private float _reloadTime;
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

    public PlayerShooting(Transform playerTransform, Transform firePoint, GameObject bulletPrefab, float bulletSpeed, GameObject[] fireVFXPrefabs, Animator weaponAnimator, string recoilTrigger, float fireRate = 0.25f, bool isAutomatic = false, int magazineSize = 25, float reloadTime = 1.5f)
    {
        _playerTransform = playerTransform;
        _firePoint = firePoint;
        _bulletSpeed = bulletSpeed;
        _fireRate = fireRate;
        _isAutomatic = isAutomatic;
        _weapon = new BulletWeapon(_playerTransform, _firePoint, bulletPrefab, _bulletSpeed, Mathf.Max(magazineSize * 2, 50));
        _firePointVFXPrefabs = fireVFXPrefabs;
        _weaponAnimator = weaponAnimator;
        _recoilTrigger = recoilTrigger;
        
        // Initialize ammo
        _magazineSize = magazineSize;
        _reloadTime = reloadTime;
        _currentAmmo = _magazineSize;

        // Initialize VFX pools
        InitializeVFXPools();

        // Preload gameplay critical FMOD events to prevent I/O lag spikes
        FMODHelper.PreloadEvent("event:/Gun_Shot_Player");
        FMODHelper.PreloadEvent("event:/Gun_Auto_Reload");
    }

    private void InitializeVFXPools()
    {
        if (_vfxPools != null)
        {
            foreach (var pool in _vfxPools)
            {
                if (pool != null)
                {
                    pool.Clear();
                }
            }
        }

        _vfxPools = new System.Collections.Generic.List<ObjectPool<Transform>>();
        if (_firePointVFXPrefabs != null)
        {
            foreach (var prefab in _firePointVFXPrefabs)
            {
                if (prefab != null)
                {
                    // CRITICAL SAFEGUARD: If the developer mistakenly assigned the FirePoint itself
                    // or any Player GameObject as the VFX prefab, skip it to prevent infinite recursion/RAM explosion!
                    if (prefab == _firePoint.gameObject || prefab.transform.IsChildOf(_playerTransform))
                    {
                        Debug.LogWarning($"[PlayerShooting] Skipping invalid VFX prefab '{prefab.name}' because it is part of the player hierarchy. This prevents infinite recursion.");
                        _vfxPools.Add(null);
                        continue;
                    }

                    _vfxPools.Add(new ObjectPool<Transform>(prefab.transform, 10, _firePoint));
                }
                else
                {
                    _vfxPools.Add(null);
                }
            }
        }
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

        // Check if can shoot
        bool inputDetected = _isAutomatic ? Input.GetMouseButton(0) : Input.GetMouseButtonDown(0);

        if (inputDetected && Time.time >= _nextFireTime && _currentAmmo > 0)
        {
            _nextFireTime = Time.time + (_fireRate / (Player.Instance != null ? Player.Instance.GetTotalFireRateMultiplier() : 1f));
            _weapon.Fire(aimDir);
            
            // Play gunshot sound with random pitch variation
            float randomPitch = UnityEngine.Random.Range(0.9f, 1.1f);
            FMODHelper.PlayOneShot("event:/Gun_Shot_Player", _firePoint.position, randomPitch);
            
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
        
        // Play gunshot sound with random pitch variation
        float randomPitch = UnityEngine.Random.Range(0.9f, 1.1f);
        FMODHelper.PlayOneShot("event:/Gun_Shot_Player", _firePoint.position, randomPitch);
        
        _currentAmmo--;
        OnAmmoChanged?.Invoke(_currentAmmo, _magazineSize);
        OnShoot?.Invoke();

        SpawnVFX();
        if (_weaponAnimator != null) _weaponAnimator.SetTrigger(_recoilTrigger);
    }

    private void SpawnVFX()
    {
        if (_vfxPools == null || _firePointVFXPrefabs == null) return;
        
        for (int i = 0; i < _firePointVFXPrefabs.Length; i++)
        {
            var prefab = _firePointVFXPrefabs[i];
            if (prefab == null) continue;
            
            var pool = _vfxPools[i];
            if (pool != null)
            {
                Transform vfxInstance = pool.GetObject();
                
                // Position at fire point with corrected rotation
                Quaternion correctedRotation = _firePoint.rotation * Quaternion.Euler(0, 180, 0);
                vfxInstance.SetPositionAndRotation(_firePoint.position, correctedRotation);
                
                ParticleSystem rootPS = vfxInstance.GetComponent<ParticleSystem>();
                if (rootPS != null)
                {
                    rootPS.Play(true);
                }
                else
                {
                    ParticleSystem childPS = vfxInstance.GetComponentInChildren<ParticleSystem>();
                    if (childPS != null)
                    {
                        childPS.Play(true);
                    }
                }
                
                PoolObjectCleanup cleanup = vfxInstance.GetComponent<PoolObjectCleanup>();
                if (cleanup == null)
                {
                    cleanup = vfxInstance.gameObject.AddComponent<PoolObjectCleanup>();
                    cleanup.Init(pool, 1.0f);
                }
                else
                {
                    cleanup.ResetTimer();
                }
            }
        }
    }

    public void UpdateWeaponStats(GameObject bulletPrefab, float fireRate, float bulletSpeed, int magazineSize, float reloadTime, bool isAutomatic, Animator weaponAnimator, string recoilTrigger, GameObject[] firePointVFX)
    {
        _fireRate = fireRate;
        _bulletSpeed = bulletSpeed;
        _magazineSize = magazineSize;
        _reloadTime = reloadTime;
        _isAutomatic = isAutomatic;
        _weaponAnimator = weaponAnimator;
        _recoilTrigger = recoilTrigger;
        _firePointVFXPrefabs = firePointVFX;

        // Dispose of the old weapon to cleanup its pools and prevent GameObject leak
        if (_weapon is System.IDisposable disposable)
        {
            disposable.Dispose();
        }

        _weapon = new BulletWeapon(_playerTransform, _firePoint, bulletPrefab, _bulletSpeed, Mathf.Max(magazineSize * 2, 50));
        
        // Reinitialize VFX pools for the new weapon
        InitializeVFXPools();

        // Reset/Refill ammo
        _currentAmmo = _magazineSize;
        _isReloading = false;
        _reloadTimer = 0f;
        
        OnAmmoChanged?.Invoke(_currentAmmo, _magazineSize);
        OnReloadStateChanged?.Invoke(false);
    }

    public void EquipWeapon(GameObject bulletPrefab)
    {
        if (_weapon is System.IDisposable disposable)
        {
            disposable.Dispose();
        }
        _weapon = new BulletWeapon(_playerTransform, _firePoint, bulletPrefab, _bulletSpeed, Mathf.Max(_magazineSize * 2, 50));
    }
}
