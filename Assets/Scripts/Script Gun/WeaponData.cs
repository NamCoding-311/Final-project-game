using UnityEngine;

[CreateAssetMenu(fileName = "NewWeapon", menuName = "Weapon/WeaponData")]
public class WeaponData : ScriptableObject
{
    [Header("Combat Stats")]
    public string weaponName;
    public GameObject bulletPrefab;
    public float fireRate = 0.3f;      // AR: nhanh, Sniper: chậm
    public int pelletsPerShot = 1;     // Shotgun: nhiều viên/lần, AR/Sniper: 1
    public float spreadAngle = 0f;     // độ tản đạn (độ) - Shotgun cao, Sniper = 0
    public int damage = 10;
    public bool isAutomatic = true;    // AR: giữ bắn liên tục, Sniper/Shotgun: mỗi lần click 1 phát

    [Header("Visuals")]
    [Tooltip("Sprite hình ảnh súng ở trạng thái tĩnh (Idle)")]
    public Sprite weaponSprite;
    public Color weaponColor = Color.white;

    [Header("Shoot Animation (Hoạt ảnh bắn & Giật nòng)")]
    [Tooltip("Danh sách các frame hình Sprite Sheet khi bắn (Lóe lửa nòng + Giật khóa nòng + Văng vỏ đạn)")]
    public Sprite[] shootAnimationFrames;

    [Tooltip("Tốc độ khung hình của animation bắn (FPS)")]
    public float animationFps = 28f;

    [Tooltip("Vị trí đầu nòng súng so với tâm súng để đạn bay ra chuẩn điểm lửa")]
    public Vector2 firePointOffset = new Vector2(0.65f, 0.05f);

    [Tooltip("Khoảng cách súng giật lùi (Recoil Kickback) khi bắn")]
    public float recoilDistance = 0.12f;
}
