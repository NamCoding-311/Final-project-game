
using UnityEngine;

// Dieu khien Basic Zombie:
// - Rượt duoi Player
// - Run animation
// - Attack theo khoang cach va cooldown
// - Gây damage bang Animation Event
// - Nhan damage, knockback/death animation
public class Zombie : MonoBehaviour
{
    [Header("Stats")]
    [SerializeField] private int maxHP = 50;
    [SerializeField] private float moveSpeed = 2.5f;
    [SerializeField] private int attackDamage = 15;

    [Header("Attack Settings")]
    [Tooltip("Khoảng cách tiếp cận để bắt đầu đánh và gây sát thương (mét)")]
    [SerializeField] private float attackRange = 0.35f;
    [SerializeField] private float attackCooldown = 1.5f;

    [Header("Directional Sprites")]
    [SerializeField] private Sprite[] directionalSprites;

    [Header("Road Boundaries")]
    [SerializeField] private bool clampToRoad = true;
    [SerializeField] private float minRoadY = -1.1f;
    [SerializeField] private float maxRoadY = 3.0f;

    [Header("Cleanup")]
    [SerializeField] private float despawnDistanceBehind = 25f;

    // Su kien toan cuc khi Zombie bi tieu diet
    public static event System.Action<Zombie> OnZombieDied;

    public static void NotifyZombieDeath(Zombie zombie = null)
    {
        OnZombieDied?.Invoke(zombie);
    }

    private int _currentHP;

    private Transform _playerTransform;
    private PlayerHealth _playerHealth;

    private SpriteRenderer _spriteRenderer;
    private Animator _animator;
    private Collider2D _collider;
    private Collider2D _playerCollider;
    private Coroutine _attackRoutine;

    private Vector2 _moveDirection;

    private bool _isDead;

    // Attack state
    private bool _isAttacking;
    private bool _hasDealtAttackDamage;

    private float _nextAttackTime;
    private bool _hasSpeedParam = false;

    private void Awake()
    {
        _collider = GetComponent<Collider2D>();
        _spriteRenderer = GetComponent<SpriteRenderer>();
        _animator = GetComponent<Animator>();

        if (_animator != null)
        {
            foreach (var p in _animator.parameters)
            {
                if (p.name == "Speed")
                {
                    _hasSpeedParam = true;
                    break;
                }
            }
        }
    }

    private void Start()
    {
        _currentHP = maxHP;
        _isDead = false;

        _isAttacking = false;
        _hasDealtAttackDamage = false;

        // Tu dong dong bo ranh gioi mat duong tu ZombieSpawner cua Scene hien tai
        ZombieSpawner spawner = FindAnyObjectByType<ZombieSpawner>();
        if (spawner != null)
        {
            minRoadY = spawner.GetRoadMinY();
            maxRoadY = spawner.GetRoadMaxY();
        }

        FindPlayerTarget();
    }

    private void Update()
    {
        // Zombie chet thi dung hoan toan
        if (_isDead)
            return;

        // Tim lai Player neu target cu khong con hoat dong
        if (_playerTransform == null ||
            !_playerTransform.gameObject.activeInHierarchy)
        {
            FindPlayerTarget();

            if (_playerTransform == null)
                return;
        }

        // Destroy neu bi bo lai phia sau qua xa
        if (_playerTransform.position.x - transform.position.x >
            despawnDistanceBehind)
        {
            Destroy(gameObject);
            return;
        }

        // Cap nhat huong nhin theo Player
        UpdateMoveDirection();

        // Neu dang attack thi dung di chuyen
        if (_isAttacking)
        {
            UpdateDirectionVisuals();
            return;
        }

        // Neu Player nam trong tam danh
        if (IsPlayerInAttackRange())
        {
            UpdateDirectionVisuals();
            TryAttack();
            return;
        }

        // Ngoai tam danh: tiep tuc ruot duoi
        ChasePlayer();

        UpdateDirectionVisuals();
    }

    // =========================================================
    // FIND PLAYER
    // =========================================================

    private void FindPlayerTarget()
    {
        OnFootPlayerController onFoot =
            FindAnyObjectByType<OnFootPlayerController>();

        if (onFoot != null &&
            onFoot.gameObject.activeInHierarchy)
        {
            SetTarget(onFoot.transform);
            return;
        }

        GameObject playerObj =
            GameObject.FindGameObjectWithTag("Player");

        if (playerObj != null)
        {
            SetTarget(playerObj.transform);
        }
    }

    // =========================================================
    // SET TARGET
    // =========================================================

    public void SetTarget(Transform target)
    {
        _playerTransform = target;
        _playerHealth = null;
        _playerCollider = null;

        if (target != null)
        {
            _playerHealth =
                target.GetComponent<PlayerHealth>()
                ?? target.GetComponentInParent<PlayerHealth>()
                ?? target.GetComponentInChildren<PlayerHealth>();

            _playerCollider =
                target.GetComponent<Collider2D>()
                ?? target.GetComponentInParent<Collider2D>()
                ?? target.GetComponentInChildren<Collider2D>();
        }
    }

    public void SetClampToRoad(bool clamp)
    {
        clampToRoad = clamp;
    }

    // Thiet lap ranh gioi mat duong dong bo voi tung Map
    public void SetRoadBoundaries(float minY, float maxY)
    {
        minRoadY = minY;
        maxRoadY = maxY;
    }

    private bool _isElite = false;

    // Tăng chỉ số theo Wave (Độ khó tăng dần)
    public void ScaleStats(float hpMultiplier, float damageMultiplier, float speedMultiplier)
    {
        maxHP = Mathf.RoundToInt(maxHP * hpMultiplier);
        _currentHP = maxHP;
        attackDamage = Mathf.RoundToInt(attackDamage * damageMultiplier);
        moveSpeed *= speedMultiplier;
    }

    // Biến thành Quái Tinh Anh (to hơn, máu trâu gấp 2.5x, rơi ngọc tím xịn)
    public void MakeElite(float bonusHp = 2.5f, float bonusDamage = 1.3f)
    {
        _isElite = true;
        maxHP = Mathf.RoundToInt(maxHP * bonusHp);
        _currentHP = maxHP;
        attackDamage = Mathf.RoundToInt(attackDamage * bonusDamage);
        transform.localScale *= 1.35f;

        if (_spriteRenderer != null)
        {
            _spriteRenderer.color = new Color(1f, 0.35f, 0.35f, 1f); // Đỏ cam đột biến
        }
    }


    // =========================================================
    // UPDATE DIRECTION
    // =========================================================

    private void UpdateMoveDirection()
    {
        if (_playerTransform == null)
            return;

        Vector2 direction =
            (Vector2)(_playerTransform.position - transform.position);

        if (direction.sqrMagnitude > 0.001f)
        {
            _moveDirection = direction.normalized;
        }
    }

    // =========================================================
    // ATTACK RANGE
    // =========================================================

    private bool IsPlayerInAttackRange()
    {
        if (_playerTransform == null)
            return false;

        if (_collider == null)
        {
            _collider = GetComponent<Collider2D>();
        }

        if (_playerCollider == null)
        {
            _playerCollider =
                _playerTransform.GetComponent<Collider2D>()
                ?? _playerTransform.GetComponentInParent<Collider2D>()
                ?? _playerTransform.GetComponentInChildren<Collider2D>();
        }

        // 1. Ưu tiên đo khoảng cách va chạm thực giữa 2 Collider (chính xác tuyệt đối khi Player lái xe to hoặc đi bộ)
        if (_collider != null && _playerCollider != null)
        {
            ColliderDistance2D colDist = _collider.Distance(_playerCollider);
            if (colDist.isOverlapped || colDist.distance <= attackRange)
            {
                return true;
            }
        }

        // 2. Dự phòng theo tâm Transform nếu không tìm thấy Collider
        float distance =
            Vector2.Distance(
                transform.position,
                _playerTransform.position
            );

        return distance <= attackRange;
    }

    // =========================================================
    // ATTACK
    // =========================================================

    private void TryAttack()
    {
        if (_isDead || _isAttacking)
            return;

        if (Time.time < _nextAttackTime)
            return;

        StartAttack();
    }

    private void StartAttack()
    {
        if (_isDead)
            return;

        _isAttacking = true;
        _hasDealtAttackDamage = false;

        // Bat dau cooldown tu luc khoi dong attack
        _nextAttackTime = Time.time + attackCooldown;

        if (_animator != null)
        {
            // Phai trung voi Trigger "attack" trong Animator Controller
            _animator.SetTrigger("attack");
        }
        else
        {
            _isAttacking = false;
            return;
        }

        // Coroutine du phong: Tu dong gay damage va ket thuc don danh neu Animation Event chua duoc cau hinh trong clip
        if (_attackRoutine != null)
        {
            StopCoroutine(_attackRoutine);
        }
        _attackRoutine = StartCoroutine(AttackFallbackRoutine());
    }

    private System.Collections.IEnumerator AttackFallbackRoutine()
    {
        // Cho den thoi diem vung tay trung dich (khoang 0.45s tren clip 0.9s)
        yield return new WaitForSeconds(0.45f);

        if (_isAttacking && !_hasDealtAttackDamage && !_isDead)
        {
            DealAttackDamage();
        }

        // Cho het thoi luong clip animation don danh (them 0.45s)
        yield return new WaitForSeconds(0.45f);

        if (_isAttacking && !_isDead)
        {
            EndAttack();
        }

        _attackRoutine = null;
    }

    // Goi bang Animation Event tai frame tay zombie cham trung Player
    public void DealAttackDamage()
    {
        if (_isDead)
            return;

        if (!_isAttacking)
            return;

        // Dam bao moi animation chi gay damage 1 lan
        if (_hasDealtAttackDamage)
            return;

        if (_playerTransform == null)
            return;

        // Player chay ra khoi tam danh thi khong bi hit
        if (!IsPlayerInAttackRange())
            return;

        // Tim lai PlayerHealth neu chua co
        if (_playerHealth == null)
        {
            _playerHealth =
                _playerTransform.GetComponent<PlayerHealth>()
                ?? _playerTransform.GetComponentInParent<PlayerHealth>()
                ?? _playerTransform.GetComponentInChildren<PlayerHealth>();
        }

        if (_playerHealth != null)
        {
            _playerHealth.TakeDamage(attackDamage);
            _hasDealtAttackDamage = true;

            Debug.Log("Basic Zombie attack! Damage: " + attackDamage);
        }
    }

    // Goi bang Animation Event o frame cuoi animation Attack hoac qua Coroutine fallback
    public void EndAttack()
    {
        if (_attackRoutine != null)
        {
            StopCoroutine(_attackRoutine);
            _attackRoutine = null;
        }

        if (_isDead)
            return;

        _isAttacking = false;
    }

    // =========================================================
    // CHASE
    // =========================================================

    private void ChasePlayer()
    {
        OnFootPlayerController onFoot =
            FindAnyObjectByType<OnFootPlayerController>();

        // Neu Player dang di bo thi uu tien target nay
        if (onFoot != null &&
            onFoot.gameObject.activeInHierarchy)
        {
            if (_playerTransform != onFoot.transform)
            {
                SetTarget(onFoot.transform);
            }
        }

        if (_playerTransform == null)
            return;

        _moveDirection =
            (_playerTransform.position - transform.position)
            .normalized;

        Vector3 newPos =
            transform.position +
            (Vector3)(
                _moveDirection *
                moveSpeed *
                Time.deltaTime
            );

        if (clampToRoad)
        {
            newPos.y =
                Mathf.Clamp(
                    newPos.y,
                    minRoadY,
                    maxRoadY
                );
        }

        transform.position = newPos;
    }

    // =========================================================
    // RUN ANIMATION / DIRECTION
    // =========================================================

    private void UpdateDirectionVisuals()
    {
        // Anh goc quay sang Phai
        if (_spriteRenderer != null)
        {
            if (_moveDirection.x < -0.05f)
            {
                _spriteRenderer.flipX = true;
            }
            else if (_moveDirection.x > 0.05f)
            {
                _spriteRenderer.flipX = false;
            }
        }

        if (_animator != null)
        {
            _animator.SetFloat(
                "MoveX",
                _moveDirection.x
            );

            _animator.SetFloat(
                "MoveY",
                _moveDirection.y
            );

            if (_hasSpeedParam)
            {
                _animator.SetFloat(
                    "Speed",
                    _moveDirection.sqrMagnitude
                );
            }

            return;
        }

        // Fallback neu khong co Animator
        if (_spriteRenderer != null &&
            directionalSprites != null &&
            directionalSprites.Length > 0)
        {
            float angle =
                Mathf.Atan2(
                    _moveDirection.y,
                    _moveDirection.x
                ) * Mathf.Rad2Deg;

            if (directionalSprites.Length >= 8)
            {
                int dirIndex =
                    Mathf.RoundToInt(
                        (angle + 90f) / 45f
                    );

                dirIndex =
                    (dirIndex % 8 + 8) % 8;

                _spriteRenderer.sprite =
                    directionalSprites[dirIndex];
            }
            else if (directionalSprites.Length >= 4)
            {
                int dirIndex =
                    Mathf.RoundToInt(
                        (angle + 90f) / 90f
                    );

                dirIndex =
                    (dirIndex % 4 + 4) % 4;

                _spriteRenderer.sprite =
                    directionalSprites[dirIndex];
            }
        }
    }

    // =========================================================
    // TAKE DAMAGE
    // =========================================================

    public void TakeDamage(int amount)
    {
        if (_isDead)
            return;

        _currentHP -= amount;

        Debug.Log(
            "Zombie HP: " + _currentHP
        );

        if (_currentHP <= 0)
        {
            Die();
        }
    }

    // =========================================================
    // DEATH / KNOCKDOWN
    // =========================================================

    private void Die()
    {
        if (_isDead)
            return;

        _isDead = true;
        OnZombieDied?.Invoke(this);

        // Huy attack dang chay
        if (_attackRoutine != null)
        {
            StopCoroutine(_attackRoutine);
            _attackRoutine = null;
        }
        _isAttacking = false;

        // Vo hieu hoa Collider va Rigidbody ngay de khong can tro dan hay nguoi choi
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null) rb.simulated = false;

        // Tang kill count
        DistanceTrackerUI tracker =
            FindAnyObjectByType<DistanceTrackerUI>();

        if (tracker != null)
        {
            tracker.AddZombieKill();
        }

        if (ArenaManager.Instance != null)
        {
            ArenaManager.Instance.AddKill();
        }

        // Roi ngoc Material/XP
        SpawnMaterialGem();

        // Chay Knockdown animation roi lam bien mat xac zombie sau 0.8s
        if (_animator != null)
        {
            _animator.ResetTrigger("attack");
            _animator.SetTrigger("knock");
            Destroy(gameObject, 0.8f);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // =========================================================
    // PLAYER DAMAGE
    // =========================================================

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Khong gay damage bang va cham nua.
        // Damage duoc xu ly tai Animation Event
        // DealAttackDamage().
    }

    // =========================================================
    // GIZMOS
    // =========================================================

    private void SpawnMaterialGem()
    {
        // Chỉ sinh ngọc nếu đang ở chế độ Đấu trường (Arena)
        if (ArenaManager.Instance != null || BrotatoLevelSystem.Instance != null)
        {
            GameObject gem = new GameObject("MaterialGem");
            gem.transform.position = transform.position;
            MaterialGem mg = gem.AddComponent<MaterialGem>();
            if (_isElite)
            {
                mg.SetGemData(4, new Color(0.85f, 0.25f, 1f), 1.1f);
            }
            float dropChance = _isElite ? 22f : 3.5f;
            ArenaPowerUp.TryDropRandom(transform.position, dropChance);
        }
    }
}