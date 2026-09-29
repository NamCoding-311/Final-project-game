using UnityEngine;

// Hệ thống bắn súng tự động (Auto-Aim & Auto-Fire) chuẩn phong cách Brotato / Vampire Survivors.
// Dành riêng cho chế độ phụ Arena Survival: Tự động lia nòng súng vào con Zombie gần nhất và xả đạn liên tục.
public class ArenaAutoShooting : MonoBehaviour
{
    [Header("Weapon Configuration")]
    [SerializeField] private WeaponData currentWeapon;

    [Header("Gun Transforms")]
    [SerializeField] private Transform gunHolder;
    [SerializeField] private Transform firePoint;
    [SerializeField] private SpriteRenderer gunSpriteRenderer;

    [Header("Auto-Aim Settings")]
    [Tooltip("Bán kính tối đa để súng phát hiện và khóa mục tiêu")]
    [SerializeField] private float detectionRange = 10f;

    [Tooltip("Layer chứa Zombie để tối ưu quét tìm kiếm (nếu để mặc định sẽ quét theo Tag)")]
    [SerializeField] private LayerMask enemyLayer;

    [Header("Brotato Modifiers (Dùng cho Nâng cấp / Shop)")]
    [SerializeField] private int bonusDamage = 0;
    [SerializeField] private float attackSpeedMultiplier = 1.0f; // Càng cao bắn càng nhanh

    private float _nextFireTime;
    private Transform _currentTarget;

    private void Start()
    {
        // Tự động tìm GunHolder, FirePoint, GunSpriteRenderer nếu chưa gán thủ công
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
        // 1. Quét tìm con quái gần nhất trong bán kính
        FindClosestEnemy();

        // 2. Nhắm bắn
        if (_currentTarget != null)
        {
            AimAtTarget(_currentTarget.position);

            // 3. Tự động xả đạn nếu đến lượt bắn
            if (Time.time >= _nextFireTime)
            {
                FireBullets();
            }
        }
        else
        {
            // Khi không có quái xung quanh: giữ súng xuôi theo hướng nhân vật
            ResetGunAim();
        }
    }

    // Quét tìm Zombie gần nhất
    private void FindClosestEnemy()
    {
        // Lấy tất cả Collider trong bán kính quét
        Collider2D[] hits;
        if (enemyLayer != 0)
        {
            hits = Physics2D.OverlapCircleAll(transform.position, detectionRange, enemyLayer);
        }
        else
        {
            hits = Physics2D.OverlapCircleAll(transform.position, detectionRange);
        }

        Transform closest = null;
        float minSqrDist = float.MaxValue;
        Vector2 myPos = transform.position;

        foreach (var col in hits)
        {
            if (col == null || !col.gameObject.activeInHierarchy) continue;

            // Kiểm tra xem có đúng là Zombie hay Boss không
            if (col.CompareTag("Zombie") || col.GetComponent<Zombie>() != null || col.GetComponent<ZombieJumper>() != null || col.GetComponent<ZombieBoss>() != null)
            {
                float sqrDist = ((Vector2)col.transform.position - myPos).sqrMagnitude;
                if (sqrDist < minSqrDist)
                {
                    minSqrDist = sqrDist;
                    closest = col.transform;
                }
            }
        }

        _currentTarget = closest;
    }

    // Xoay súng khóa chặt vào mục tiêu
    private void AimAtTarget(Vector3 targetPos)
    {
        if (gunHolder == null) return;

        Vector2 aimDirection = (targetPos - gunHolder.position);
        float angle = Mathf.Atan2(aimDirection.y, aimDirection.x) * Mathf.Rad2Deg;
        gunHolder.rotation = Quaternion.Euler(0f, 0f, angle);

        // Lật súng theo trục Y khi ngắm sang bên trái
        if (gunSpriteRenderer != null)
        {
            gunSpriteRenderer.flipY = targetPos.x < transform.position.x;
        }
    }

    // Khi không có quái: súng quay về hướng mặc định
    private void ResetGunAim()
    {
        if (gunHolder == null) return;
        
        // Nhìn sang bên phải hoặc giữ nguyên
        if (gunSpriteRenderer != null && gunSpriteRenderer.flipY)
        {
            gunHolder.rotation = Quaternion.Euler(0f, 0f, 180f);
        }
    }

    // Tự động xả đạn
    private void FireBullets()
    {
        if (currentWeapon == null || currentWeapon.bulletPrefab == null) return;

        // Tính toán tốc độ bắn (kèm hệ số nhân tốc độ từ nâng cấp Brotato)
        float currentFireRate = Mathf.Max(0.05f, currentWeapon.fireRate / Mathf.Max(0.1f, attackSpeedMultiplier));
        _nextFireTime = Time.time + currentFireRate;

        Vector3 spawnPos = firePoint != null ? firePoint.position : transform.position;
        Vector2 baseDir = gunHolder != null ? (Vector2)gunHolder.right : Vector2.right;

        int finalDamage = currentWeapon.damage + bonusDamage;

        for (int i = 0; i < currentWeapon.pelletsPerShot; i++)
        {
            // Tính độ tản đạn
            float spread = Random.Range(-currentWeapon.spreadAngle / 2f, currentWeapon.spreadAngle / 2f);
            Vector2 finalDir = RotateVector(baseDir, spread);

            float bulletAngle = Mathf.Atan2(finalDir.y, finalDir.x) * Mathf.Rad2Deg;
            Quaternion bulletRotation = Quaternion.Euler(0f, 0f, bulletAngle);

            GameObject bulletObj = Instantiate(currentWeapon.bulletPrefab, spawnPos, bulletRotation);
            Bullet bulletScript = bulletObj.GetComponent<Bullet>();
            if (bulletScript != null)
            {
                bulletScript.SetDirection(finalDir);
                bulletScript.SetDamage(finalDamage);
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

    // Các hàm nâng cấp chỉ số Brotato (Shop & Level up)
    public void EquipWeapon(WeaponData newWeapon) => currentWeapon = newWeapon;
    public void AddBonusDamage(int extraDmg) => bonusDamage += extraDmg;
    public void AddAttackSpeed(float percent) => attackSpeedMultiplier += percent;

    // Vẽ vòng bán kính quét mục tiêu trong Scene View để dễ căn chỉnh
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
    }
}
