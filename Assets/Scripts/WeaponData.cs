using UnityEngine;

[CreateAssetMenu(fileName = "NewWeapon", menuName = "Weapon/WeaponData")]
public class WeaponData : ScriptableObject
{
    public string weaponName;
    public GameObject bulletPrefab;
    public float fireRate = 0.3f;      // AR: nhanh, Sniper: chậm
    public int pelletsPerShot = 1;     // Shotgun: nhiều viên/lần, AR/Sniper: 1
    public float spreadAngle = 0f;     // độ tản đạn (độ) - Shotgun cao, Sniper = 0
    public int damage = 10;
    public bool isAutomatic = true;    // AR: giữ bắn liên tục, Sniper/Shotgun: mỗi lần click 1 phát
}