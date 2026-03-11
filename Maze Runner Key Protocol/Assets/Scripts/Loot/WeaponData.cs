using UnityEngine;

public enum FireMode { Single, Automatic }

[CreateAssetMenu(fileName = "NewWeapon", menuName = "MazeRunner/WeaponData")]
public class WeaponData : ScriptableObject
{
    public string weaponName = "Weapon";
    public float damage = 10f;
    public float fireRate = 3f; // shots per second
    public int ammoCapacity = 30; // magazine size
    public int maxAmmo = 90; // total carrying capacity, -1 for infinite
    public float spreadAngle = 0f; // degrees
    public float effectiveRange = 50f; // Unity units
    public FireMode fireMode = FireMode.Single;
    public float reloadTime = 1.5f; // seconds
    public int pelletCount = 1; // >1 for shotgun
}
