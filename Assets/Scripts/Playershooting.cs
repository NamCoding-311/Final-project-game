using UnityEngine;

// Điều khiển khẩu súng xoay 360 độ theo con trỏ chuột và bắn đạn từ FirePoint
public class PlayerShooting : MonoBehaviour
{
    [Header("Weapon Configuration")]
    // Dữ liệu loại súng đang cầm (AR, Shotgun, Sniper, Pistol)
    [SerializeField] private WeaponData currentWeapon;

    [Header("Gun Transforms")]
    // Transform của tay cầm/báng súng (xoay quanh điểm này)
    [SerializeField] private Transform gunHolder;

    // Vị trí đầu nòng súng nơi đạn sinh ra (nằm bên trong GunHolder)
    [SerializeField] private Transform firePoint;

    // SpriteRenderer của khẩu súng (để lật súng khi quay sang trái)
    [SerializeField] private SpriteRenderer gunSpriteRenderer;

    private float _nextFireTime;
    private Camera _cam;

    private void Start()
    {
        _cam = Camera.main;

        // Tự động tìm các đối tượng con nếu chưa kéo trong Inspector
        if (gunHolder == null)
        {
            Transform found = transform.Find("GunHolder") ?? transform.Find("Gun");
            if (found != null) gunHolder = found;
        }

        if (gunHolder != null)
        {
            if (gunSpriteRenderer == null)
            {
                gunSpriteRenderer = gunHolder.GetComponentInChildren<SpriteRenderer>();
            }

            if (firePoint == null)
            {
                Transform foundFp = gunHolder.Find("FirePoint");
                if (foundFp != null) firePoint = foundFp;
            }
        }
    }

    private void Update()
    {
        if (_cam == null) _cam = Camera.main;

        // 1. Xoay khẩu súng và FirePoint theo con trỏ chuột
        RotateGun();

        // 2. Xử lý bắn đạn
        HandleShooting();
    }

    // Xoay súng hướng về vị trí con trỏ chuột
    private void RotateGun()
    {
        if (gunHolder == null || _cam == null) return;

        // Lấy tọa độ chuột trong không gian thế giới 2D
        Vector3 mouseScreen = Input.mousePosition;
        mouseScreen.z = -_cam.transform.position.z;
        Vector3 mouseWorld = _cam.ScreenToWorldPoint(mouseScreen);

        // Tính hướng từ báng súng đến con trỏ chuột
        Vector2 aimDirection = (mouseWorld - gunHolder.position);

        // Tính góc xoay (Angle) theo độ
        float angle = Mathf.Atan2(aimDirection.y, aimDirection.x) * Mathf.Rad2Deg;
        gunHolder.rotation = Quaternion.Euler(0f, 0f, angle);

        // Lật súng theo trục Y khi ngắm sang bên trái để súng không bị lộn ngược đầu
        if (gunSpriteRenderer != null)
        {
            gunSpriteRenderer.flipY = mouseWorld.x < transform.position.x;
        }
    }

    // Xử lý bắn đạn khi người chơi bấm chuột trái
    private void HandleShooting()
    {
        if (currentWeapon == null) return;

        bool wantsToFire = currentWeapon.isAutomatic
            ? Input.GetMouseButton(0)       // Giữ chuột để sấy liên thanh (AR/AK47)
            : Input.GetMouseButtonDown(0);  // Bấm từng phát

        if (wantsToFire && Time.time >= _nextFireTime)
        {
            _nextFireTime = Time.time + currentWeapon.fireRate;
            FireBullets();
        }
    }

    // Sinh đạn bay thẳng theo hướng nòng súng đang chỉ
    private void FireBullets()
    {
        if (currentWeapon.bulletPrefab == null) return;

        // Vị trí xuất phát của đạn là FirePoint ở đầu nòng
        Vector3 spawnPos = firePoint != null ? firePoint.position : transform.position;

        // Hướng bắn chính là hướng trục đỏ (Right) của súng sau khi xoay
        Vector2 baseDir = gunHolder != null ? (Vector2)gunHolder.right : Vector2.right;

        for (int i = 0; i < currentWeapon.pelletsPerShot; i++)
        {
            // Tính độ tản đạn ngẫu nhiên
            float randomSpread = Random.Range(-currentWeapon.spreadAngle / 2f, currentWeapon.spreadAngle / 2f);
            Vector2 finalDir = RotateVector(baseDir, randomSpread);

            // Xoay đầu đạn theo đúng hướng bay
            float bulletAngle = Mathf.Atan2(finalDir.y, finalDir.x) * Mathf.Rad2Deg;
            Quaternion bulletRotation = Quaternion.Euler(0f, 0f, bulletAngle);

            // Sinh đạn
            GameObject bulletObj = Instantiate(currentWeapon.bulletPrefab, spawnPos, bulletRotation);
            Bullet bulletScript = bulletObj.GetComponent<Bullet>();
            if (bulletScript != null)
            {
                bulletScript.SetDirection(finalDir);
                bulletScript.SetDamage(currentWeapon.damage);
            }
        }
    }

    // Hàm toán học xoay vector hướng
    private Vector2 RotateVector(Vector2 v, float degrees)
    {
        float rad = degrees * Mathf.Deg2Rad;
        float cos = Mathf.Cos(rad);
        float sin = Mathf.Sin(rad);
        return new Vector2(v.x * cos - v.y * sin, v.x * sin + v.y * cos);
    }

    public void EquipWeapon(WeaponData newWeapon) => currentWeapon = newWeapon;
}