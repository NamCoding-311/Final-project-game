using UnityEngine;

// Điều khiển hành vi Zombie:
// - Rượt đuổi Player
// - Chạy animation Run
// - Nhận damage
// - HP = 0 → Knockdown
public class Zombie : MonoBehaviour
{
    [Header("Stats")]
    [SerializeField] private int maxHP = 50;
    [SerializeField] private float moveSpeed = 2.5f;
    [SerializeField] private int attackDamage = 15;

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

    // Zombie đã chết
    private bool _isDead;

    private void Awake()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
        _animator = GetComponent<Animator>();
    }

    private void Start()
    {
        _currentHP = maxHP;
        _isDead = false;

        FindPlayerTarget();
    }

    private void Update()
    {
        // Zombie chết thì dừng hoàn toàn
        if (_isDead)
            return;

        if (_playerTransform == null ||
            !_playerTransform.gameObject.activeInHierarchy)
        {
            FindPlayerTarget();

            if (_playerTransform == null)
                return;
        }

        // Rượt Player
        ChasePlayer();

        // Cập nhật Run animation
        UpdateDirectionVisuals();

        // Destroy nếu bị bỏ lại phía sau quá xa
        if (_playerTransform.position.x - transform.position.x >
            despawnDistanceBehind)
        {
            Destroy(gameObject);
        }
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
            _playerTransform = onFoot.transform;

            _playerHealth =
                onFoot.GetComponent<PlayerHealth>();

            if (_playerHealth == null)
            {
                _playerHealth =
                    FindAnyObjectByType<PlayerHealth>();
            }

            return;
        }

        GameObject playerObj =
            GameObject.FindGameObjectWithTag("Player");

        if (playerObj != null)
        {
            _playerTransform = playerObj.transform;

            _playerHealth =
                playerObj.GetComponent<PlayerHealth>();
        }
    }

    // =========================================================
    // SET TARGET
    // =========================================================

    public void SetTarget(Transform target)
    {
        _playerTransform = target;

        if (target != null)
        {
            _playerHealth =
                target.GetComponent<PlayerHealth>();

            if (_playerHealth == null)
            {
                _playerHealth =
                    target.GetComponentInParent<PlayerHealth>();
            }
        }
    }

    // =========================================================
    // CHASE
    // =========================================================

    private void ChasePlayer()
    {
        OnFootPlayerController onFoot =
            FindAnyObjectByType<OnFootPlayerController>();

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
    // RUN ANIMATION
    // =========================================================

    private void UpdateDirectionVisuals()
    {
        // Tự động lật mặt Zombie theo hướng di chuyển (ảnh gốc quay sang Phải)
        if (_spriteRenderer != null)
        {
            if (_moveDirection.x < -0.05f)
            {
                _spriteRenderer.flipX = true; // Lao sang Trái (đón đầu xe) -> Lật mặt sang Trái
            }
            else if (_moveDirection.x > 0.05f)
            {
                _spriteRenderer.flipX = false; // Rượt sang Phải (đuổi theo xe) -> Giữ mặt sang Phải
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
        // Đã chết thì không nhận damage nữa
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

        // Tăng kill count
        DistanceTrackerUI tracker =
            FindAnyObjectByType<DistanceTrackerUI>();

        if (tracker != null)
        {
            tracker.AddZombieKill();
        }

        // Chạy Knockdown animation
        if (_animator != null)
        {
            _animator.SetTrigger("knock");
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
        // Zombie chết không thể gây damage
        if (_isDead)
            return;

        if (other.CompareTag("Player"))
        {
            if (_playerHealth != null)
            {
                _playerHealth.TakeDamage(attackDamage);
            }
        }
    }
}