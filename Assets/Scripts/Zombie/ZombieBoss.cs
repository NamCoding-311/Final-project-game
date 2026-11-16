using System;
using System.Collections;
using UnityEngine;

// Điều khiển Trùm Zombie (Zombie Boss / Tank):
// - Tốc độ vũ bão, húc áp sát cực mạnh
// - Kỹ năng 1: Ném đá tầm xa (Parabol) - Ném chùm 3 viên khi Cuồng nộ
// - Kỹ năng 2: Húc tốc cận chiến (Charge Attack) - Húc kép (Double Dash) khi Cuồng nộ
// - Kỹ năng 3: Nhảy dậm địa chấn (Leap Slam) - Rung màn hình + Sóng chấn động AOE
// - Phase 2: Cuồng nộ (Enrage Mode) khi máu < 50% (Hóa đỏ rực, tăng tốc, bão kỹ năng)
public class ZombieBoss : MonoBehaviour
{
    [Header("Boss Identity & Stats")]
    [SerializeField] private string bossName = "TANK ZOMBIE";
    [SerializeField] private int maxHealth = 600;
    [SerializeField] private float moveSpeed = 4.5f;
    [SerializeField] private int baseAttackDamage = 30;

    [Header("Skill 1: Rock Throw (Ném Đá Tầm Xa)")]
    [SerializeField] private GameObject rockPrefab;
    [SerializeField] private Transform throwPoint;
    [SerializeField] private float throwCooldown = 4.0f;
    [SerializeField] private float minThrowDistance = 4.0f;
    [SerializeField] private float rockSpeed = 13f;
    [SerializeField] private float rockArcHeight = 2.8f;
    [SerializeField] private float throwReleaseDelay = 0.35f;

    [Header("Skill 2: Charge Attack (Húc Tốc Áp Sát)")]
    [SerializeField] private float chargeSpeed = 15f;
    [SerializeField] private float chargeDuration = 1.0f;
    [SerializeField] private float chargeCooldown = 5.5f;
    [SerializeField] private float chargeWindupTime = 0.4f;
    [SerializeField] private int chargeDamage = 45;

    [Header("Skill 3: Leap Slam (Nhảy Dậm Địa Chấn)")]
    [SerializeField] private bool allowLeapSlam = true;
    [SerializeField] private float leapCooldown = 7.0f;
    [SerializeField] private float leapDuration = 0.85f;
    [SerializeField] private float leapHeight = 4.0f;
    [SerializeField] private float slamRadius = 3.5f;
    [SerializeField] private int slamDamage = 40;

    [Header("Phase 2: Enrage Mode (Cuồng Nộ < 50% HP)")]
    [SerializeField] private bool allowEnragePhase = true;
    [SerializeField] private Color enrageColor = new Color(1.0f, 0.3f, 0.3f, 1.0f);
    [SerializeField] private float enrageSpeedMultiplier = 1.25f;

    [Header("Skill Activation Toggles")]
    [SerializeField] private bool allowRockThrow = true;
    [SerializeField] private bool allowChargeAttack = true;

    [Header("Visual & Facing Settings")]
    [SerializeField] private bool invertFacing = true;
    [SerializeField] private Color hitFlashColor = Color.red;
    [SerializeField] private Color chargeWarningColor = Color.yellow;

    [Header("Arena Clamping")]
    [SerializeField] private bool clampToArena = true;
    [SerializeField] private float minArenaX = 220f;
    [SerializeField] private float maxArenaX = 280f;
    [SerializeField] private float minArenaY = -1.2f;
    [SerializeField] private float maxArenaY = 3.2f;

    private int _currentHealth = 600;
    private Transform _playerTransform;
    private bool _isTargetingOnFootPlayer = false;

    private SpriteRenderer _spriteRenderer;
    private Animator _animator;
    private Color _originalColor;

    // Trạng thái kỹ năng
    private bool _isThrowing = false;
    private bool _isCharging = false;
    private bool _isWindingUpCharge = false;
    private bool _isLeaping = false;
    private bool _isEnraged = false;
    private bool _isDead = false;

    private float _nextThrowTime;
    private float _nextChargeTime;
    private float _nextLeapTime;

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

    public void SetClampToArena(bool clamp)
    {
        clampToArena = clamp;
    }

    // Tăng chỉ số Boss theo Wave
    public void ScaleStats(float hpMultiplier, float damageMultiplier, float speedMultiplier)
    {
        maxHealth = Mathf.RoundToInt(maxHealth * hpMultiplier);
        _currentHealth = maxHealth;
        baseAttackDamage = Mathf.RoundToInt(baseAttackDamage * damageMultiplier);
        chargeDamage = Mathf.RoundToInt(chargeDamage * damageMultiplier);
        slamDamage = Mathf.RoundToInt(slamDamage * damageMultiplier);
        moveSpeed *= Mathf.Min(1.35f, speedMultiplier);
    }

    // Cấu hình kỹ năng và máu cho Boss theo từng chặng
    public void ConfigureBossSkills(bool enableRock, bool enableCharge, float hpMultiplier = 1f, bool enableLeap = true)
    {
        allowRockThrow = enableRock;
        allowChargeAttack = enableCharge;
        allowLeapSlam = enableLeap;
        if (hpMultiplier > 1.01f)
        {
            ScaleStats(hpMultiplier, 1f, 1f);
        }
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
        if (ArenaManager.Instance != null)
        {
            clampToArena = false;
        }

        if (_playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                SetTarget(player.transform);
            }
        }

        // Thời gian khởi động trước khi tung kỹ năng đầu tiên
        _nextThrowTime = Time.time + 1.8f;
        _nextChargeTime = Time.time + 4.5f;
        _nextLeapTime = Time.time + 6.5f;

        OnBossHealthChanged?.Invoke(this, _currentHealth, maxHealth);
    }

    private void Update()
    {
        if (_isDead || _playerTransform == null) return;

        // 1. Kiểm tra kỹ năng Nhảy Dậm Địa Chấn (Leap Slam)
        CheckLeapSlamSkill();

        // 2. Kiểm tra kỹ năng Ném Đá (Rock Throw)
        CheckRockThrowSkill();

        // 3. Kiểm tra kỹ năng Húc Tốc (Charge Attack)
        CheckChargeAttackSkill();

        // 4. Di chuyển bình thường khi không bận tung chiêu
        if (!_isThrowing && !_isWindingUpCharge && !_isLeaping)
        {
            MoveTowardsPlayer();
        }

        // 5. Cập nhật hướng quay mặt
        if (!_isLeaping)
        {
            UpdateFacingDirection();
        }
    }

    // =========================================================
    // KỸ NĂNG 1: NÉM ĐÁ (ROCK THROW) - NÉM CHÙM 3 VIÊN KHI CUỒNG NỘ
    // =========================================================

    private void CheckRockThrowSkill()
    {
        if (!allowRockThrow) return;
        if (_isThrowing || _isCharging || _isWindingUpCharge || _isLeaping) return;
        if (rockPrefab == null) return;

        float distance = Vector2.Distance(transform.position, _playerTransform.position);

        if (Time.time >= _nextThrowTime && distance >= minThrowDistance)
        {
            StartCoroutine(PerformRockThrowRoutine());
        }
    }

    private IEnumerator PerformRockThrowRoutine()
    {
        _isThrowing = true;
        _nextThrowTime = Time.time + (_isEnraged ? throwCooldown * 0.65f : throwCooldown);

        if (_animator != null)
        {
            _animator.SetTrigger("ThrowRock");
        }

        yield return new WaitForSeconds(throwReleaseDelay);

        Vector3 spawnPos = throwPoint != null
            ? throwPoint.position
            : transform.position + new Vector3(transform.localScale.x > 0 ? 0.8f : -0.8f, 0.5f, 0f);

        if (!_isEnraged)
        {
            // Ném 1 viên đá ngắm thẳng vào người chơi
            SpawnSingleRock(spawnPos, _playerTransform.position);
        }
        else
        {
            // PHASE 2 CUỒNG NỘ: NÉM CHÙM 3 VIÊN ĐÁ HÌNH QUẠT!
            SpawnSingleRock(spawnPos, _playerTransform.position);
            SpawnSingleRock(spawnPos, _playerTransform.position + Vector3.up * 2.2f);
            SpawnSingleRock(spawnPos, _playerTransform.position + Vector3.down * 2.2f);

            CameraFollow cam = FindAnyObjectByType<CameraFollow>();
            if (cam != null) cam.TriggerShake(0.35f, 0.25f);
        }

        yield return new WaitForSeconds(0.35f);
        _isThrowing = false;
    }

    private void SpawnSingleRock(Vector3 spawnPos, Vector3 targetPos)
    {
        if (rockPrefab == null) return;
        GameObject rockObj = Instantiate(rockPrefab, spawnPos, Quaternion.identity);
        BossRock rock = rockObj.GetComponent<BossRock>();
        if (rock != null)
        {
            rock.Launch(targetPos, rockArcHeight, rockSpeed);
        }
    }

    // =========================================================
    // KỸ NĂNG 2: HÚC TỐC (CHARGE ATTACK) - HÚC KÉP KHI CUỒNG NỘ
    // =========================================================

    private void CheckChargeAttackSkill()
    {
        if (!allowChargeAttack) return;
        if (_isThrowing || _isCharging || _isWindingUpCharge || _isLeaping) return;

        if (Time.time >= _nextChargeTime)
        {
            StartCoroutine(PerformChargeAttackRoutine());
        }
    }

    private IEnumerator PerformChargeAttackRoutine()
    {
        _isWindingUpCharge = true;
        _nextChargeTime = Time.time + (_isEnraged ? chargeCooldown * 0.65f : chargeCooldown);

        // Giai đoạn 1: Lấy đà và đổi màu cảnh báo
        if (_spriteRenderer != null) _spriteRenderer.color = chargeWarningColor;
        yield return new WaitForSeconds(chargeWindupTime);

        _isWindingUpCharge = false;
        _isCharging = true;

        if (_animator != null)
        {
            _animator.SetTrigger("Dash");
        }

        // Giai đoạn 2: Phóng vọt với tốc độ cao
        float currentChargeSpeed = _isEnraged ? chargeSpeed * 1.15f : chargeSpeed;
        float chargeEndTime = Time.time + chargeDuration;
        while (Time.time < chargeEndTime)
        {
            MoveWithCustomSpeed(currentChargeSpeed);
            yield return null;
        }

        // Va chạm chấn động sau khi hết đà húc
        CameraFollow cam = FindAnyObjectByType<CameraFollow>();
        if (cam != null) cam.TriggerShake(0.4f, 0.3f);

        // PHASE 2 CUỒNG NỘ: HÚC KÉP (DOUBLE CHARGE)
        if (_isEnraged)
        {
            yield return new WaitForSeconds(0.2f);
            UpdateFacingDirection();
            if (_animator != null) _animator.SetTrigger("Dash");

            float secondEndTime = Time.time + (chargeDuration * 0.75f);
            while (Time.time < secondEndTime)
            {
                MoveWithCustomSpeed(currentChargeSpeed);
                yield return null;
            }

            if (cam != null) cam.TriggerShake(0.45f, 0.35f);
        }

        _isCharging = false;
        if (_spriteRenderer != null) _spriteRenderer.color = _originalColor;
    }

    // =========================================================
    // KỸ NĂNG 3: NHẢY DẬM ĐỊA CHẤN (LEAP SLAM) - RUNG MÀN HÌNH + AOE
    // =========================================================

    private void CheckLeapSlamSkill()
    {
        if (!allowLeapSlam) return;
        if (_isThrowing || _isCharging || _isWindingUpCharge || _isLeaping) return;

        float distance = Vector2.Distance(transform.position, _playerTransform.position);

        if (Time.time >= _nextLeapTime && distance >= 3.0f)
        {
            StartCoroutine(PerformLeapSlamRoutine());
        }
    }

    private IEnumerator PerformLeapSlamRoutine()
    {
        _isLeaping = true;
        _nextLeapTime = Time.time + (_isEnraged ? leapCooldown * 0.65f : leapCooldown);

        // Khựng lại 0.2s kích hoạt trigger Jump
        if (_animator != null)
        {
            _animator.SetTrigger("Jump");
        }
        yield return new WaitForSeconds(0.2f);

        Vector3 startPos = transform.position;
        Vector3 targetPos = _playerTransform.position;
        if (clampToArena)
        {
            targetPos.x = Mathf.Clamp(targetPos.x, minArenaX, maxArenaX);
            targetPos.y = Mathf.Clamp(targetPos.y, minArenaY, maxArenaY);
        }

        float elapsed = 0f;
        while (elapsed < leapDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / leapDuration);

            // Quỹ đạo nhảy Parabol
            Vector3 cur = Vector3.Lerp(startPos, targetPos, t);
            cur.y += 4f * leapHeight * t * (1f - t);
            transform.position = cur;
            UpdateFacingDirection();
            yield return null;
        }

        transform.position = targetPos;

        // KHI TIẾP ĐẤT: RUNG MÀN HÌNH MẠNH + SÓNG CHẤN ĐỘNG AOE + BỤI ĐẤT
        CameraFollow cam = FindAnyObjectByType<CameraFollow>();
        if (cam != null)
        {
            cam.TriggerShake(0.65f, 0.5f);
        }

        GroundShockwaveEffect.Spawn(transform.position, maxRadius: 5.5f, duration: 0.5f);

        // Gây sát thương nổ AOE xung quanh điểm tiếp đất
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, slamRadius);
        foreach (var h in hits)
        {
            if (h.gameObject == gameObject) continue;

            OnFootPlayerController onFoot = h.GetComponent<OnFootPlayerController>();
            if (onFoot != null)
            {
                onFoot.TakeDamage(slamDamage);
                continue;
            }

            if (h.CompareTag("Player"))
            {
                PlayerHealth health = h.GetComponent<PlayerHealth>();
                if (health != null)
                {
                    health.TakeDamage(slamDamage);
                }
            }
        }

        yield return new WaitForSeconds(0.35f);
        _isLeaping = false;
    }

    // =========================================================
    // DI CHUYỂN & LẬT MẶT
    // =========================================================

    private void MoveTowardsPlayer()
    {
        float speed = _isCharging ? chargeSpeed : (_isEnraged ? moveSpeed * enrageSpeedMultiplier : moveSpeed);
        MoveWithCustomSpeed(speed);
    }

    private void MoveWithCustomSpeed(float speed)
    {
        Vector3 targetPos = _isTargetingOnFootPlayer
            ? _playerTransform.position
            : new Vector3(_playerTransform.position.x + 3f, _playerTransform.position.y, transform.position.z);

        Vector3 nextPos = Vector3.MoveTowards(transform.position, targetPos, speed * Time.deltaTime);

        if (clampToArena)
        {
            nextPos.x = Mathf.Clamp(nextPos.x, minArenaX, maxArenaX);
            float halfH = Mathf.Abs(transform.localScale.y) * 0.22f;
            nextPos.y = Mathf.Clamp(nextPos.y, minArenaY + halfH, maxArenaY - halfH);
        }

        transform.position = nextPos;
        UpdateFacingDirection();
    }

    private void UpdateFacingDirection()
    {
        if (_playerTransform == null) return;
        bool playerIsOnRight = _playerTransform.position.x > transform.position.x;
        float absX = Mathf.Abs(transform.localScale.x);

        if (invertFacing)
        {
            transform.localScale = new Vector3(playerIsOnRight ? -absX : absX, transform.localScale.y, transform.localScale.z);
        }
        else
        {
            transform.localScale = new Vector3(playerIsOnRight ? absX : -absX, transform.localScale.y, transform.localScale.z);
        }
    }

    // =========================================================
    // NHẬN SÁT THƯƠNG & PHASE 2 CUỒNG NỘ
    // =========================================================

    public void TakeDamage(int damage)
    {
        if (_currentHealth <= 0 && !_isDead)
        {
            _currentHealth = maxHealth;
        }

        _currentHealth -= damage;
        OnBossHealthChanged?.Invoke(this, Mathf.Max(0, _currentHealth), maxHealth);

        // Nhấp nháy đỏ báo hiệu trúng đạn
        if (_spriteRenderer != null)
        {
            _spriteRenderer.color = hitFlashColor;
            CancelInvoke(nameof(ResetColor));
            Invoke(nameof(ResetColor), 0.1f);
        }

        // KÍCH HOẠT PHASE 2 CUỒNG NỘ KHI MÁU < 50%
        if (allowEnragePhase && !_isEnraged && _currentHealth <= maxHealth * 0.5f && _currentHealth > 0)
        {
            TriggerEnrageMode();
        }

        if (_currentHealth <= 0)
        {
            Die();
        }
    }

    private void TriggerEnrageMode()
    {
        _isEnraged = true;

        // Đổi màu vĩnh viễn sang đỏ rực
        _originalColor = enrageColor;
        if (_spriteRenderer != null) _spriteRenderer.color = _originalColor;

        // Rung chấn màn hình báo hiệu Cuồng Nộ
        CameraFollow cam = FindAnyObjectByType<CameraFollow>();
        if (cam != null)
        {
            cam.TriggerShake(0.85f, 0.5f);
        }

        GroundShockwaveEffect.Spawn(transform.position, maxRadius: 6.5f, duration: 0.6f);

        Debug.Log($"[ZombieBoss] {bossName} HAS ENTERED ENRAGE PHASE 2!");
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

    private void Die()
    {
        if (_isDead) return;
        _isDead = true;

        Debug.Log($"[ZombieBoss] {bossName} has been Defeated!");

        if (_animator != null)
        {
            _animator.SetTrigger("die");
        }

        Collider2D[] colliders = GetComponents<Collider2D>();
        foreach (var col in colliders)
        {
            col.enabled = false;
        }

        OnBossDied?.Invoke(this);

        if (ArenaManager.Instance != null)
        {
            ArenaManager.Instance.AddKill();
        }

        if (ArenaManager.Instance != null || BrotatoLevelSystem.Instance != null)
        {
            for (int i = 0; i < 3; i++)
            {
                Vector3 offset = new Vector3(UnityEngine.Random.Range(-1.2f, 1.2f), UnityEngine.Random.Range(-1.2f, 1.2f), 0f);
                MaterialGem.SpawnCoin(transform.position + offset, CoinTier.Red);
            }
            for (int i = 0; i < 3; i++)
            {
                Vector3 offset = new Vector3(UnityEngine.Random.Range(-1.2f, 1.2f), UnityEngine.Random.Range(-1.2f, 1.2f), 0f);
                MaterialGem.SpawnCoin(transform.position + offset, CoinTier.Gold);
            }
            ArenaPowerUp.TryDropRandom(transform.position, 100f);
        }

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
    public bool IsEnraged() => _isEnraged;
}
