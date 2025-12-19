using UnityEngine;

public class WeaponPickup : MonoBehaviour, IInteractable
{
    [SerializeField] private GameObject _bulletPrefab;
    [SerializeField] private string _weaponName;

    public void Interact(Player player)
    {
        Debug.Log($"Picked up {_weaponName}");
        player.EquipWeapon(_bulletPrefab);
    }
}
