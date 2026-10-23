using UnityEngine;

// Điều khiển khẩu súng xoay 360 độ theo con trỏ chuột, hỗ trợ hoạt ảnh bắn (Muzzle Flash, vỏ đạn, giật nòng) và bắn đạn từ FirePoint
public class PlayerShooting : MonoBehaviour
{
    [Header("Weapon Configuration")]
    // Dữ liệu loại súng đang cầm (AR, Shotgun, Sniper, Pistol...)
    [SerializeField] private WeaponData currentWeapon;

    [Header("Gun Transforms")]
    // Transform của tay cầm/báng súng (xoay quanh điểm này)
    [SerializeField] private Transform gunHolder;

    // Vị trí đầu nòng súng nơi đạn sinh ra (nằm bên trong GunHolder)
    [SerializeField] private Transform firePoint;

    // SpriteRenderer của khẩu súng (để hiển thị và lật súng khi quay sang trái)
    [SerializeField] private SpriteRenderer gunSpriteRenderer;

    // Bộ điều khiển hoạt ảnh lóe lửa, vỏ đạn và giật nòng của súng
    [SerializeField] private GunVisualAnimator gunVisualAnimator;

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

            if (gunVisualAnimator == null)
            {
                gunVisualAnimator = gunHolder.GetComponentInChildren<GunVisualAnimator>();
            }
        }

        // Tự động khởi tạo hoạt ảnh và nòng súng ban đầu nếu có dữ liệu vũ khí
        if (gunVisualAnimator != null && currentWeapon != null)
        {
            gunVisualAnimator.Initialize(currentWeapon, gunSpriteRenderer);
        }

        // Cập nhật vị trí FirePoint theo cấu hình nòng súng của WeaponData
        UpdateFirePointOffset(false);
    }

    private void Update()
    {
        if (_cam == null) _cam = Camera.main;

        // 1. Xoay khẩu súng và FirePoint theo con trỏ chuột
        RotateGun();

        // 2. Xử lý bắn đạn khi người chơi bấm chuột
        HandleShooting();
    }

    // Xoay súng hướng về vị trí con trỏ chuột và lật dọc khi ngắm bên trái
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

        // Kiểm tra xem chuột đang ở bên trái nhân vật hay không để lật súng
        bool isFlipped = mouseWorld.x < transform.position.x;
        if (gunVisualAnimator != null)
        {
            gunVisualAnimator.SetFlipped(isFlipped);
        }
        else if (gunSpriteRenderer != null)
        {
            gunSpriteRenderer.flipY = isFlipped;
        }

        // Cập nhật vị trí đầu nòng tương thích khi súng bị lật trục Y
        UpdateFirePointOffset(isFlipped);
    }

    // Căn chỉnh vị trí đầu nòng FirePoint chuẩn xác theo độ cao nòng khi lật
    private void UpdateFirePointOffset(bool isFlipped)
    {
        if (firePoint == null || currentWeapon == null) return;

        Vector2 offset = currentWeapon.firePointOffset != Vector2.zero
            ? currentWeapon.firePointOffset
            : new Vector2(0.65f, 0.05f);

        firePoint.localPosition = new Vector3(offset.x, isFlipped ? -offset.y : offset.y, 0f);
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

    // Sinh đạn bay thẳng theo hướng nòng súng đang chỉ và kích hoạt hoạt ảnh bắn
    private void FireBullets()
    {
        if (currentWeapon.bulletPrefab == null) return;

        // Kích hoạt hoạt ảnh bắn (Muzzle Flash lóe lửa, văng vỏ đạn, giật nòng Recoil)
        if (gunVisualAnimator != null)
        {
            gunVisualAnimator.PlayShootAnimation();
        }

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

                // Truyền vận tốc hiện tại của xe để đạn bay cộng hưởng theo đà xe
                Player3LaneMovement movement = GetComponent<Player3LaneMovement>();
                if (movement != null)
                {
                    bulletScript.SetInheritedSpeed(movement.GetCurrentSpeed());
                }
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

    // Thay đổi vũ khí mới và đồng bộ lại hình ảnh hoạt ảnh
    public void EquipWeapon(WeaponData newWeapon)
    {
        currentWeapon = newWeapon;
        if (gunVisualAnimator != null && currentWeapon != null)
        {
            gunVisualAnimator.Initialize(currentWeapon, gunSpriteRenderer);
        }
        UpdateFirePointOffset(false);
    }
}
