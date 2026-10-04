
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
    [SerializeField] private float attackRange = 1.2f;
    [SerializeField] private float attackCooldown = 1.5f;

    [Header("Directional Sprites")]
    [SerializeField] private Sprite[] directionalSprites;

    [Header("Road Boundaries")]
    [SerializeField] private bool clampToRoad = true;
    [SerializeField] private float minRoadY = -1.1f;
    [SerializeField] private float maxRoadY = 3.0f;

    [Header("Cleanup")]
    [SerializeField] private float despawnDistanceBehind = 25f;

    private int _currentHP;

    private Transform _playerTransform;
    private PlayerHealth _playerHealth;

    private SpriteRenderer _spriteRenderer;
    private Animator _animator;

    private Vector2 _moveDirection;

    private bool _isDead;

    // Attack state
    private bool _isAttacking;
    private bool _hasDealtAttackDamage;

    private float _nextAttackTime;

    private void Awake()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
        _animator = GetComponent<Animator>();
    }

    private void Start()
    {
        _currentHP = maxHP;
        _isDead = false;

        _isAttacking = false;
        _hasDealtAttackDamage = false;

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

        if (target != null)
        {
            _playerHealth =
                target.GetComponent<PlayerHealth>();

            if (_playerHealth == null)
            {
                _playerHealth =
                    target.GetComponentInParent<PlayerHealth>();
            }

            if (_playerHealth == null)
            {
                _playerHealth =
                    target.GetComponentInChildren<PlayerHealth>();
            }
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
            // Phai trung voi Trigger "attack"
            // trong Animator Controller
            _animator.SetTrigger("attack");
        }
        else
        {
            // Khong co Animator thi khong the
            // phat Animation Event.
            Debug.LogWarning(
                "Zombie khong co Animator!",
                gameObject
            );

            _isAttacking = false;
        }
    }

    // Goi bang Animation Event tai frame tay zombie
    // cham trung Player.
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
                _playerTransform.GetComponent<PlayerHealth>();

            if (_playerHealth == null)
            {
                _playerHealth =
                    _playerTransform.GetComponentInParent<PlayerHealth>();
            }

            if (_playerHealth == null)
            {
                _playerHealth =
                    _playerTransform.GetComponentInChildren<PlayerHealth>();
            }
        }

        if (_playerHealth != null)
        {
            _playerHealth.TakeDamage(attackDamage);

            _hasDealtAttackDamage = true;

            Debug.Log(
                "Basic Zombie attack! Damage: " + attackDamage
            );
        }
        else
        {
            Debug.LogWarning(
                "Khong tim thay PlayerHealth tren Player!",
                gameObject
            );
        }
    }

    // Goi bang Animation Event o frame cuoi animation Attack
    public void EndAttack()
    {
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

            _animator.SetFloat(
                "Speed",
                _moveDirection.sqrMagnitude
            );

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

        // Huy attack dang chay
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
            gem.AddComponent<MaterialGem>();
        }
    }
}