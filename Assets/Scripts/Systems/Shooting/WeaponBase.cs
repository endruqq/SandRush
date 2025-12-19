using UnityEngine;

public abstract class WeaponBase
{
    protected Transform Owner;

    protected WeaponBase(Transform owner)
    {
        Owner = owner;
    }

    public abstract void Fire(Vector3 direction);
}
