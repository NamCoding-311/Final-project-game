using System;
using System.Collections;
using UnityEngine;

// Điều khiển Trùm Zombie (Zombie Boss / Tank):
// - Kỹ năng 1: Ném đá tầm xa (Parabol)
// - Kỹ năng 2: Húc tốc cận chiến (Charge Attack)
// - Lật mặt chuẩn xác theo hướng mục tiêu
public class ZombieBoss : MonoBehaviour
{
    [Header("Boss Identity & Stats")]
    [SerializeField] private string bossName = "TANK ZOMBIE";
    [SerializeField] private int maxHealth = 500;
    [SerializeField] private float moveSpeed = 4f;
    [SerializeField] private int baseAttackDamage = 25;

    [Header("Skill 1: Rock Throw (Ném Đá Tầm Xa)")]
    [SerializeField] private GameObject rockPrefab;
    [SerializeField] private Transform throwPoint;
    [SerializeField] private float throwCooldown = 5f;
    [SerializeField] private float minThrowDistance = 4.5f;
    [SerializeField] private float rockSpeed = 12f;
    [SerializeField] private float rockArcHeight = 2.5f;
    [SerializeField] private float throwReleaseDelay = 0.35f; // Thời gian chờ vung tay ném ra đá

    [Header("Skill 2: Charge Attack (Húc Tốc Áp Sát)")]
    [SerializeField] private float chargeSpeed = 14f;
    [SerializeField] private float chargeDuration = 1.0f;
    [SerializeField] private float chargeCooldown = 7f;
    [SerializeField] private float chargeWindupTime = 0.5f; // Khựng lại lấy đà trước khi phóng
    [SerializeField] private int chargeDamage = 45;

    [Header("Visual & Facing Settings")]
    // Bật nếu sprite gốc vẽ quay sang Trái (mặc định của bộ Tank)
    [SerializeField] private bool invertFacing = true;
    [SerializeField] private Color hitFlashColor = Color.red;
    [SerializeField] private Color chargeWarningColor = Color.yellow;

    [Header("Arena Clamping")]
    [SerializeField] private bool clampToArena = true;
    [SerializeField] private float minArenaX = 220f;
    [SerializeField] private float maxArenaX = 280f;
    [SerializeField] private float minArenaY = -1.2f;
    [SerializeField] private float maxArenaY = 3.2f;

    private int _currentHealth;
    private Transform _playerTransform;
    private bool _isTargetingOnFootPlayer = false;

    private SpriteRenderer _spriteRenderer;
    private Animator _animator;
    private Color _originalColor;

    // Trạng thái kỹ năng
    private bool _isThrowing = false;
    private bool _isCharging = false;
    private bool _isWindingUpCharge = false;

    private float _nextThrowTime;
    private float _nextChargeTime;

    // Sự kiện thông báo khi Boss nhận sát thương và khi Boss chết
    public static event Action<ZombieBoss, int, int> OnBossHealthChanged;
    public static event Action<ZombieBoss> OnBossDied;

    public void SetTarget(Transform newTarget)
    {
        _playerTransform = newTarget;
        _isTargetingOnFootPlayer = newTarget != null && newTarget.GetComponent<OnFootPlayerController>() != null;
    }

    public void SetArenaBounds(float minX, float maxX, float minY, float maxY)
    {
        minArenaX = minX;
        maxArenaX = maxX;
        minArenaY = minY;
        maxArenaY = maxY;
        clampToArena = true;
    }

    private void Awake()
    {
        _currentHealth = maxHealth;
        _spriteRenderer = GetComponent<SpriteRenderer>();
        _animator = GetComponent<Animator>();

        if (_spriteRenderer != null)
        {
            _originalColor = _spriteRenderer.color;
        }
    }

    private void Start()
    {
        if (_playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                SetTarget(player.transform);
            }
        }

        // Cho Boss đi bộ một lúc trước khi tung chiêu đầu tiên
        _nextThrowTime = Time.time + 2.5f;
        _nextChargeTime = Time.time + 6.0f;

        OnBossHealthChanged?.Invoke(this, _currentHealth, maxHealth);
    }

    private void Update()
    {
        if (_isDead || _playerTransform == null) return;


        // 1. Kiểm tra kỹ năng Ném Đá (ưu tiên khi ở xa)
        CheckRockThrowSkill();

        // 2. Kiểm tra kỹ năng Húc Tốc
        CheckChargeAttackSkill();

        // 3. Di chuyển (chỉ khi không đang ném đá hoặc đang gồng lấy đà)
        if (!_isThrowing && !_isWindingUpCharge)
        {
            MoveTowardsPlayer();
        }

        // 4. Luôn cập nhật hướng quay mặt chuẩn xác (kể cả khi dừng lại ném đá)
        UpdateFacingDirection();
    }

    // =========================================================
    // KỸ NĂNG 1: NÉM ĐÁ (ROCK THROW)
    // =========================================================

    private void CheckRockThrowSkill()
    {
        if (_isThrowing || _isCharging || _isWindingUpCharge) return;
        if (rockPrefab == null) return;

        float distance = Vector2.Distance(transform.position, _playerTransform.position);

        // Đủ thời gian hồi và ở khoảng cách tầm trung - xa
        if (Time.time >= _nextThrowTime && distance >= minThrowDistance)
        {
            StartCoroutine(PerformRockThrowRoutine());
        }
    }

    private IEnumerator PerformRockThrowRoutine()
    {
        _isThrowing = true;
        _nextThrowTime = Time.time + throwCooldown;

        // Bật animation ném đá
        if (_animator != null)
        {
            _animator.SetTrigger("ThrowRock");
        }

        // Chờ đúng thời điểm tay vung ra để sinh viên đá
        yield return new WaitForSeconds(throwReleaseDelay);

        // Sinh viên đá tại điểm ném (hoặc tự tính nếu chưa gán throwPoint)
        Vector3 spawnPos = throwPoint != null
            ? throwPoint.position
            : transform.position + new Vector3(transform.localScale.x > 0 ? 0.8f : -0.8f, 0.5f, 0f);

        GameObject rockObj = Instantiate(rockPrefab, spawnPos, Quaternion.identity);
        BossRock rock = rockObj.GetComponent<BossRock>();
        if (rock != null)
        {
            // Nhắm thẳng vào vị trí hiện tại của người chơi
            rock.Launch(_playerTransform.position, rockArcHeight, rockSpeed);
        }

        // Chờ nốt phần còn lại của animation ném đá rồi tiếp tục bước đi
        yield return new WaitForSeconds(0.4f);
        _isThrowing = false;
    }

    // =========================================================
    // KỸ NĂNG 2: HÚC TỐC (CHARGE ATTACK)
    // =========================================================

    private void CheckChargeAttackSkill()
    {
        if (_isThrowing || _isCharging || _isWindingUpCharge) return;

        if (Time.time >= _nextChargeTime)
        {
            StartCoroutine(PerformChargeAttackRoutine());
        }
    }

    private IEnumerator PerformChargeAttackRoutine()
    {
        _isWindingUpCharge = true;
        _nextChargeTime = Time.time + chargeCooldown;

        // Giai đoạn 1: Khựng lại 0.5s lấy đà và đổi màu cảnh báo
        if (_spriteRenderer != null) _spriteRenderer.color = chargeWarningColor;
        yield return new WaitForSeconds(chargeWindupTime);

        _isWindingUpCharge = false;
        _isCharging = true;

        if (_animator != null)
        {
            _animator.SetTrigger("Dash");
        }

        // Giai đoạn 2: Phóng vọt với tốc độ cao
        float chargeEndTime = Time.time + chargeDuration;
        while (Time.time < chargeEndTime)
        {
            MoveTowardsPlayer();
            yield return null;
        }

        _isCharging = false;
        if (_spriteRenderer != null) _spriteRenderer.color = _originalColor;
    }

    // =========================================================
    // DI CHUYỂN & LẬT MẶT
    // =========================================================

    private void MoveTowardsPlayer()
    {
        float currentSpeed = _isCharging ? chargeSpeed : moveSpeed;

        Vector3 targetPos = _isTargetingOnFootPlayer
            ? _playerTransform.position
            : new Vector3(_playerTransform.position.x + 3f, _playerTransform.position.y, transform.position.z);

        Vector3 nextPos = Vector3.MoveTowards(transform.position, targetPos, currentSpeed * Time.deltaTime);

        if (clampToArena)
        {
            nextPos.x = Mathf.Clamp(nextPos.x, minArenaX, maxArenaX);
            nextPos.y = Mathf.Clamp(nextPos.y, minArenaY, maxArenaY);
        }

        transform.position = nextPos;

        // Xử lý lật mặt Boss hướng về phía mục tiêu
        UpdateFacingDirection();
    }

    private void UpdateFacingDirection()
    {
        bool playerIsOnRight = _playerTransform.position.x > transform.position.x;
        float absX = Mathf.Abs(transform.localScale.x);


        if (invertFacing)
        {
            // Sprite gốc quay sang Trái:
            // Người chơi ở bên Phải -> Scale âm (-absX) để lật mặt sang Phải
            // Người chơi ở bên Trái -> Scale dương (+absX) để giữ mặt sang Trái
            transform.localScale = new Vector3(playerIsOnRight ? -absX : absX, transform.localScale.y, transform.localScale.z);
        }
        else
        {
            // Sprite gốc quay sang Phải:
            transform.localScale = new Vector3(playerIsOnRight ? absX : -absX, transform.localScale.y, transform.localScale.z);
        }
    }

    // =========================================================
    // NHẬN SÁT THƯƠNG
    // =========================================================

    public void TakeDamage(int damage)
    {
        _currentHealth -= damage;
        OnBossHealthChanged?.Invoke(this, Mathf.Max(0, _currentHealth), maxHealth);

        // Nhấp nháy màu đỏ báo hiệu trúng đạn
        if (_spriteRenderer != null)
        {
            _spriteRenderer.color = hitFlashColor;
            CancelInvoke(nameof(ResetColor));
            Invoke(nameof(ResetColor), 0.1f);
        }

        if (_currentHealth <= 0)
        {
            Die();
        }
    }

    private void ResetColor()
    {
        if (_spriteRenderer != null)
        {
            if (_isCharging || _isWindingUpCharge)
            {
                _spriteRenderer.color = chargeWarningColor;
            }
            else
            {
                _spriteRenderer.color = _originalColor;
            }
        }
    }
    private bool _isDead = false;

    private void Die()
    {
        if (_isDead) return;
        _isDead = true;

        Debug.Log($"[ZombieBoss] {bossName} has been Defeated!");

        // 1. Kích hoạt animation chết
        if (_animator != null)
        {
            _animator.SetTrigger("die");
        }

        // 2. Vô hiệu hóa va chạm để không cản đường người chơi
        Collider2D[] colliders = GetComponents<Collider2D>();
        foreach (var col in colliders)
        {
            col.enabled = false;
        }

        // 3. Thông báo Boss bị hạ gục
        OnBossDied?.Invoke(this);

        // 4. Chờ 1.5 giây sau mới xóa GameObject
        Destroy(gameObject, 1.5f);
    }

    // =========================================================
    // VA CHẠM CẬN CHIẾN
    // =========================================================

    private void OnTriggerEnter2D(Collider2D other)
    {
        int damageToDeal = _isCharging ? chargeDamage : baseAttackDamage;

        // 1. Va chạm với người đi bộ (Trashcan)
        OnFootPlayerController onFoot = other.GetComponent<OnFootPlayerController>();
        if (onFoot != null)
        {
            onFoot.TakeDamage(damageToDeal);
            return;
        }

        // 2. Va chạm với xe Player
        if (other.CompareTag("Player"))
        {
            Player3LaneMovement movement = other.GetComponent<Player3LaneMovement>();
            PlayerHealth health = other.GetComponent<PlayerHealth>();

            // Nếu người chơi đang bấm SHIFT (Dash): Xe húc Boss mất 150 máu!
            if (movement != null && movement.IsDashing())
            {
                TakeDamage(150);
            }
            else
            {
                if (health != null)
                {
                    health.TakeDamage(damageToDeal);
                }
            }
        }
    }

    public string GetBossName() => bossName;
    public int GetCurrentHealth() => _currentHealth;
    public int GetMaxHealth() => maxHealth;
}
