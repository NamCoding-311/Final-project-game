using System;
using UnityEngine;

// Điều khiển Trùm Zombie (Zombie Boss): Máu trâu, có thanh máu riêng, rượt đuổi xe và kích hoạt chuyển chặng khi bị hạ gục
public class ZombieBoss : MonoBehaviour
{
    [Header("Boss Identity & Stats")]
    [SerializeField] private string bossName = "TRÙM ĐỘT BIẾN";
    [SerializeField] private int maxHealth = 500;
    [SerializeField] private float moveSpeed = 10f;
    [SerializeField] private int attackDamage = 30;

    [Header("Special Ability: Charge Attack (Húc Tốc)")]
    // Tốc độ phóng vọt áp sát xe
    [SerializeField] private float chargeSpeed = 18f;
    [SerializeField] private float chargeDuration = 0.8f;
    [SerializeField] private float chargeCooldown = 4f;

    [Header("Arena Clamping")]
    [SerializeField] private bool clampToArena = true;
    [SerializeField] private float minArenaX = 220f;
    [SerializeField] private float maxArenaX = 280f;
    [SerializeField] private float minArenaY = -1.2f;
    [SerializeField] private float maxArenaY = 3.2f;

    [Header("Visual Effects")]
    [SerializeField] private Color hitFlashColor = Color.red;

    private int _currentHealth;
    private Transform _playerTransform;
    private bool _isTargetingOnFootPlayer = false;
    private SpriteRenderer _spriteRenderer;
    private Color _originalColor;
    private bool _isCharging = false;
    private float _nextChargeTime;
    private float _chargeEndTime;

    // Sự kiện thông báo khi Boss nhận sát thương và khi Boss chết
    public static event Action<ZombieBoss, int, int> OnBossHealthChanged;
    public static event Action<ZombieBoss> OnBossDied;

    // Gán mục tiêu theo đuổi cho Boss (Xe hoặc Nhân vật đi bộ)
    public void SetTarget(Transform newTarget)
    {
        _playerTransform = newTarget;
        _isTargetingOnFootPlayer = newTarget != null && newTarget.GetComponent<OnFootPlayerController>() != null;
    }

    // Cài đặt ranh giới sàn đấu cho Boss
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

        _nextChargeTime = Time.time + chargeCooldown;

        // Báo hiệu Boss xuất hiện để cập nhật UI
        OnBossHealthChanged?.Invoke(this, _currentHealth, maxHealth);
    }

    private void Update()
    {
        if (_playerTransform == null) return;

        HandleChargeAttack();
        MoveTowardsPlayer();
    }

    // Rượt đuổi theo mục tiêu (Xe hoặc Nhân vật đi bộ)
    private void MoveTowardsPlayer()
    {
        float currentSpeed = _isCharging ? chargeSpeed : moveSpeed;

        // Nếu mục tiêu là người đi bộ: Áp sát trực diện để tấn công
        // Nếu mục tiêu là xe: Đi song song lệch 3m
        Vector3 targetPos = _isTargetingOnFootPlayer
            ? _playerTransform.position
            : new Vector3(_playerTransform.position.x + 3f, _playerTransform.position.y, transform.position.z);

        Vector3 nextPos = Vector3.MoveTowards(transform.position, targetPos, currentSpeed * Time.deltaTime);

        // Giữ Boss trong ranh giới đấu trường
        if (clampToArena)
        {
            nextPos.x = Mathf.Clamp(nextPos.x, minArenaX, maxArenaX);
            nextPos.y = Mathf.Clamp(nextPos.y, minArenaY, maxArenaY);
        }

        transform.position = nextPos;

        // Lật mặt Boss theo hướng người chơi
        if (_playerTransform.position.x > transform.position.x)
        {
            transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
        }
        else
        {
            transform.localScale = new Vector3(-Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
        }
    }

    // Kỹ năng húc tốc định kỳ
    private void HandleChargeAttack()
    {
        if (!_isCharging && Time.time >= _nextChargeTime)
        {
            _isCharging = true;
            _chargeEndTime = Time.time + chargeDuration;
            _nextChargeTime = Time.time + chargeCooldown;

            // Đổi màu cảnh báo húc
            if (_spriteRenderer != null) _spriteRenderer.color = Color.yellow;
        }

        if (_isCharging && Time.time >= _chargeEndTime)
        {
            _isCharging = false;
            if (_spriteRenderer != null) _spriteRenderer.color = _originalColor;
        }
    }

    // Nhận sát thương khi bị bắn hoặc xe húc
    public void TakeDamage(int damage)
    {
        _currentHealth -= damage;
        OnBossHealthChanged?.Invoke(this, Mathf.Max(0, _currentHealth), maxHealth);

        // Nhấp nháy màu đỏ khi dính đòn
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
            _spriteRenderer.color = _isCharging ? Color.yellow : _originalColor;
        }
    }

    // Khi Boss bị tiêu diệt
    private void Die()
    {
        Debug.Log($"[ZombieBoss] {bossName} đã bị tiêu diệt!");
        OnBossDied?.Invoke(this);

        // Hủy Boss (có thể thêm particle nổ tung ở đây)
        Destroy(gameObject);
    }

    // Va chạm với xe hoặc nhân vật người chơi đi bộ
    private void OnTriggerEnter2D(Collider2D other)
    {
        // 1. Nếu va chạm với nhân vật người đi bộ
        OnFootPlayerController onFoot = other.GetComponent<OnFootPlayerController>();
        if (onFoot != null)
        {
            onFoot.TakeDamage(attackDamage);
            return;
        }

        // 2. Nếu va chạm với xe Player
        if (other.CompareTag("Player"))
        {
            Player3LaneMovement movement = other.GetComponent<Player3LaneMovement>();
            PlayerHealth health = other.GetComponent<PlayerHealth>();

            // Nếu người chơi đang bấm SHIFT (Dash): Xe húc Boss mất 150 máu
            if (movement != null && movement.IsDashing())
            {
                TakeDamage(150);
            }
            else
            {
                // Nếu đâm thường: Gây sát thương nặng cho xe
                if (health != null)
                {
                    health.TakeDamage(attackDamage);
                }
            }
        }
    }

    public string GetBossName() => bossName;
    public int GetCurrentHealth() => _currentHealth;
    public int GetMaxHealth() => maxHealth;
}
