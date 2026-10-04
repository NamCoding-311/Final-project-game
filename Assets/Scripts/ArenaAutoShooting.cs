using System.Collections.Generic;
using UnityEngine;

// Hệ thống Điều Khiển Nhiều Súng Tự Động (Multi-Weapon Auto-Aim System) chuẩn Brotato:
// 1. Cho phép người chơi mang tối đa 6 khẩu súng cùng lúc bay lơ lửng xoay quanh nhân vật.
// 2. Mỗi khẩu súng tự động tìm kiếm mục tiêu riêng biệt và xả đạn độc lập 360 độ.
// 3. Tự động dàn đều vị trí các súng thành vòng tròn xung quanh nhân vật theo số lượng súng đang có.
// 4. Đồng bộ các chỉ số nâng cấp Brotato: Bonus Damage, Attack Speed.
public class ArenaAutoShooting : MonoBehaviour
{
    public static ArenaAutoShooting Instance { get; private set; }

    [Header("Initial Weapons")]
    [Tooltip("Khẩu súng khởi đầu")]
    [SerializeField] private WeaponData currentWeapon;

    [Tooltip("Danh sách súng trang bị thêm lúc bắt đầu (tùy chọn)")]
    [SerializeField] private List<WeaponData> extraStartingWeapons = new List<WeaponData>();

    [Header("Multi-Gun Orbit Settings")]
    [SerializeField] private int maxWeapons = 6;
    [SerializeField] private float orbitRadius = 1.2f; // Khoảng cách súng bay cách tâm nhân vật
    [SerializeField] private float orbitFloatSpeed = 3.0f; // Nhịp bập bùng của súng

    [Header("Auto-Aim Settings")]
    [Tooltip("Bán kính tối đa để súng phát hiện và khóa mục tiêu")]
    [SerializeField] private float detectionRange = 10f;
    [SerializeField] private LayerMask enemyLayer;

    [Header("Brotato Modifiers (Dùng cho Nâng cấp / Shop)")]
    [SerializeField] private int bonusDamage = 0;
    [SerializeField] private float attackSpeedMultiplier = 1.0f;

    [Header("Debug / Testing Keys")]
    [SerializeField] private bool enableTestHotkeys = true;
    [SerializeField] private List<WeaponData> testWeaponsPool = new List<WeaponData>();

    // Template mẫu ban đầu nếu có
    [SerializeField] private Transform baseGunHolder;

    [System.Serializable]
    public class GunSlot
    {
        public WeaponData weaponData;
        public GameObject slotObject;
        public Transform gunTransform;
        public Transform firePoint;
        public SpriteRenderer spriteRenderer;
        public float nextFireTime;
        public Transform currentTarget;
        public float baseAngleOffset;
    }

    private List<GunSlot> _activeGuns = new List<GunSlot>();
    private Sprite _defaultGunSprite;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        // Tự động tìm template GunHolder ban đầu
        if (baseGunHolder == null)
        {
            Transform found = transform.Find("GunHolder") ?? transform.Find("Gun");
            if (found != null) baseGunHolder = found;
        }

        if (baseGunHolder != null)
        {
            SpriteRenderer sr = baseGunHolder.GetComponentInChildren<SpriteRenderer>();
            if (sr != null) _defaultGunSprite = sr.sprite;
            // Ẩn template gốc đi để hệ thống slot tự quản lý
            baseGunHolder.gameObject.SetActive(false);
        }

        // Trang bị súng ban đầu
        if (currentWeapon != null)
        {
            EquipNewWeapon(currentWeapon);
        }

        foreach (var extraWeapon in extraStartingWeapons)
        {
            if (extraWeapon != null)
            {
                EquipNewWeapon(extraWeapon);
            }
        }
    }

    private void Update()
    {
        // 1. Phím tắt test trang bị nhanh súng (Dành cho nhà phát triển test)
        if (enableTestHotkeys)
        {
            HandleTestingKeys();
        }

        // 2. Cập nhật vị trí bập bùng quanh nhân vật
        UpdateGunOrbitPositions();

        // 3. Mỗi súng độc lập quét mục tiêu và xả đạn
        UpdateGunsCombat();
    }

    // Trang bị thêm 1 khẩu súng mới (Dùng cho Shop hoặc Thẻ bài Level Up)
    public bool EquipNewWeapon(WeaponData newWeapon)
    {
        if (newWeapon == null) return false;
        if (_activeGuns.Count >= maxWeapons)
        {
            return false; // Đã đầy 6 súng
        }

        GunSlot slot = CreateGunSlot(newWeapon);
        _activeGuns.Add(slot);

        // Sắp xếp lại góc của toàn bộ súng thành vòng tròn cân đối
        RecalculateGunAngles();

        return true;
    }

    public int GetCurrentWeaponCount() => _activeGuns.Count;
    public int GetMaxWeaponCount() => maxWeapons;

    // Tạo GameObject slot súng mới
    private GunSlot CreateGunSlot(WeaponData weapon)
    {
        GameObject slotObj = new GameObject($"GunSlot_{_activeGuns.Count + 1}_{weapon.weaponName}");
        slotObj.transform.SetParent(transform);

        // Tạo nòng súng
        SpriteRenderer sr = slotObj.AddComponent<SpriteRenderer>();
        sr.sprite = weapon.weaponSprite != null ? weapon.weaponSprite : _defaultGunSprite;
        sr.color = weapon.weaponColor;
        sr.sortingOrder = 16; // Hiển thị đè lên nhân vật

        // Tạo FirePoint ở đầu nòng
        GameObject fpObj = new GameObject("FirePoint");
        fpObj.transform.SetParent(slotObj.transform);
        fpObj.transform.localPosition = new Vector3(0.6f, 0f, 0f);

        GunSlot slot = new GunSlot
        {
            weaponData = weapon,
            slotObject = slotObj,
            gunTransform = slotObj.transform,
            firePoint = fpObj.transform,
            spriteRenderer = sr,
            nextFireTime = Time.time + Random.Range(0f, 0.2f),
            currentTarget = null,
            baseAngleOffset = 0f
        };

        return slot;
    }

    // Sắp xếp góc của các súng chia đều 360 độ quanh người
    private void RecalculateGunAngles()
    {
        int count = _activeGuns.Count;
        if (count == 0) return;

        float angleStep = 360f / count;
        for (int i = 0; i < count; i++)
        {
            _activeGuns[i].baseAngleOffset = i * angleStep;
        }
    }

    // Cập nhật vị trí của các súng bay lơ lửng quanh nhân vật
    private void UpdateGunOrbitPositions()
    {
        float time = Time.time * orbitFloatSpeed;

        for (int i = 0; i < _activeGuns.Count; i++)
        {
            GunSlot slot = _activeGuns[i];
            if (slot.slotObject == null) continue;

            float angleRad = (slot.baseAngleOffset) * Mathf.Deg2Rad;
            // Bập bùng nhẹ theo hình sin
            float currentRadius = orbitRadius + Mathf.Sin(time + i) * 0.08f;

            Vector3 offset = new Vector3(Mathf.Cos(angleRad) * currentRadius, Mathf.Sin(angleRad) * currentRadius, 0f);
            slot.slotObject.transform.position = transform.position + offset;
        }
    }

    // Xử lý ngắm và bắn độc lập cho từng khẩu súng
    private void UpdateGunsCombat()
    {
        // Lấy tất cả quái vật xung quanh người chơi
        Collider2D[] allNearbyEnemies = enemyLayer != 0
            ? Physics2D.OverlapCircleAll(transform.position, detectionRange, enemyLayer)
            : Physics2D.OverlapCircleAll(transform.position, detectionRange);

        List<Transform> validEnemies = new List<Transform>();
        foreach (var col in allNearbyEnemies)
        {
            if (col == null || !col.gameObject.activeInHierarchy) continue;
            if (col.CompareTag("Zombie") || col.GetComponent<Zombie>() != null || col.GetComponent<ZombieJumper>() != null || col.GetComponent<ZombieBoss>() != null || col.GetComponent<ZombieCharger>() != null)
            {
                validEnemies.Add(col.transform);
            }
        }

        // Mỗi súng chọn mục tiêu thông minh (ưu tiên chia đều mục tiêu để không bắn trùng)
        for (int i = 0; i < _activeGuns.Count; i++)
        {
            GunSlot slot = _activeGuns[i];
            if (slot.slotObject == null || slot.weaponData == null) continue;

            // Tìm quái gần nhất với khẩu súng đó
            slot.currentTarget = FindBestTargetForGun(slot.gunTransform.position, validEnemies, i);

            if (slot.currentTarget != null)
            {
                // Xoay súng khóa mục tiêu
                AimGunAt(slot, slot.currentTarget.position);

                // Tự động xả đạn
                if (Time.time >= slot.nextFireTime)
                {
                    FireGun(slot);
                }
            }
            else
            {
                // Khi không có quái: súng hướng nhẹ ra ngoài theo góc xoay
                slot.gunTransform.rotation = Quaternion.Euler(0f, 0f, slot.baseAngleOffset);
                if (slot.spriteRenderer != null)
                {
                    slot.spriteRenderer.flipY = Mathf.Cos(slot.baseAngleOffset * Mathf.Deg2Rad) < 0f;
                }
            }
        }
    }

    // Tìm quái tốt nhất cho từng khẩu súng
    private Transform FindBestTargetForGun(Vector3 gunPos, List<Transform> enemies, int gunIndex)
    {
        if (enemies.Count == 0) return null;

        // Nếu có nhiều quái hơn số súng: súng phân tán mục tiêu
        if (enemies.Count >= _activeGuns.Count)
        {
            // Sắp xếp theo khoảng cách
            enemies.Sort((a, b) => {
                float distA = (a.position - gunPos).sqrMagnitude;
                float distB = (b.position - gunPos).sqrMagnitude;
                return distA.CompareTo(distB);
            });

            int targetIdx = Mathf.Clamp(gunIndex % enemies.Count, 0, enemies.Count - 1);
            return enemies[targetIdx];
        }

        // Nếu ít quái: tất cả súng cùng tập trung bắn con gần nhất
        Transform closest = null;
        float minDist = float.MaxValue;
        foreach (var e in enemies)
        {
            float dist = (e.position - gunPos).sqrMagnitude;
            if (dist < minDist)
            {
                minDist = dist;
                closest = e;
            }
        }

        return closest;
    }

    private void AimGunAt(GunSlot slot, Vector3 targetPos)
    {
        Vector2 aimDirection = (targetPos - slot.gunTransform.position);
        float angle = Mathf.Atan2(aimDirection.y, aimDirection.x) * Mathf.Rad2Deg;
        slot.gunTransform.rotation = Quaternion.Euler(0f, 0f, angle);

        if (slot.spriteRenderer != null)
        {
            slot.spriteRenderer.flipY = targetPos.x < slot.gunTransform.position.x;
        }
    }

    private void FireGun(GunSlot slot)
    {
        WeaponData weapon = slot.weaponData;
        if (weapon.bulletPrefab == null) return;

        // Tính tốc độ bắn của súng kèm hệ số Brotato
        float rate = Mathf.Max(0.05f, weapon.fireRate / Mathf.Max(0.1f, attackSpeedMultiplier));
        slot.nextFireTime = Time.time + rate;

        Vector3 spawnPos = slot.firePoint != null ? slot.firePoint.position : slot.gunTransform.position;
        Vector2 baseDir = slot.gunTransform.right;

        int finalDmg = weapon.damage + bonusDamage;

        for (int i = 0; i < weapon.pelletsPerShot; i++)
        {
            float spread = Random.Range(-weapon.spreadAngle * 0.5f, weapon.spreadAngle * 0.5f);
            Vector2 finalDir = RotateVector(baseDir, spread);

            float bulletAngle = Mathf.Atan2(finalDir.y, finalDir.x) * Mathf.Rad2Deg;
            Quaternion rot = Quaternion.Euler(0f, 0f, bulletAngle);

            GameObject bulletObj = Instantiate(weapon.bulletPrefab, spawnPos, rot);
            Bullet bullet = bulletObj.GetComponent<Bullet>();
            if (bullet != null)
            {
                bullet.SetDirection(finalDir);
                bullet.SetDamage(finalDmg);
            }
        }
    }

    private Vector2 RotateVector(Vector2 v, float degrees)
    {
        float rad = degrees * Mathf.Deg2Rad;
        float cos = Mathf.Cos(rad);
        float sin = Mathf.Sin(rad);
        return new Vector2(v.x * cos - v.y * sin, v.x * sin + v.y * cos);
    }

    // Phím tắt bàn phím để test trang bị nhanh nhiều súng (Bấm 1, 2, 3, 4 để thêm súng)
    private void HandleTestingKeys()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1))
        {
            // Thử thêm súng từ danh sách test
            if (testWeaponsPool.Count > 0)
            {
                int rnd = Random.Range(0, testWeaponsPool.Count);
                EquipNewWeapon(testWeaponsPool[rnd]);
            }
            else if (currentWeapon != null)
            {
                EquipNewWeapon(currentWeapon); // Thêm nhân bản súng hiện tại
            }
        }
    }

    // Các hàm nâng cấp chỉ số Brotato
    public void AddBonusDamage(int extraDmg) => bonusDamage += extraDmg;
    public void AddAttackSpeed(float percent) => attackSpeedMultiplier += percent;

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, orbitRadius);
    }
}
