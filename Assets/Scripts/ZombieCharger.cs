using System.Collections;
using UnityEngine;

// Zombie Charger (Kẻ Húc):
// 1. Bình thường di chuyển tiếp cận người chơi với tốc độ vừa phải.
// 2. Khi vào tầm ngắm (6-9m): Khựng lại lấy đà, nhấp nháy đỏ báo động (Windup).
// 3. Khóa hướng và lao thẳng như tên bắn với tốc độ cực cao (Charge Dash).
// 4. Người chơi có thể né sang một bên (side-step).
// 5. Sau cú húc hoặc khi đâm trúng mục tiêu/tường: Bị choáng kiệt sức trong 1 giây (Stunned) - tạo cơ hội phản công!
// 6. Tích hợp đầy đủ: Nhận sát thương, rơi ngọc MaterialGem (Arena), tính điểm Kills.
[RequireComponent(typeof(Collider2D))]
public class ZombieCharger : MonoBehaviour
{
    public enum ChargerState
    {
        Approaching,    // Đang đi bộ tiếp cận
        Windup,         // Khựng lại lấy đà, gầm gừ báo hiệu
        Charging,       // Đang húc lao thẳng về phía trước
        Stunned,        // Choáng kiệt sức sau cú húc
        Dead            // Đã bị tiêu diệt
    }

    [Header("Stats (Chỉ Số Cơ Bản)")]
    [SerializeField] private int maxHP = 75;
    [SerializeField] private float walkSpeed = 2.4f;
    [SerializeField] private int touchDamage = 10;

    [Header("Charge Attack (Kỹ Năng Húc)")]
    [Tooltip("Khoảng cách tối đa để bắt đầu kích hoạt cú húc")]
    [SerializeField] private float chargeTriggerDistance = 8f;

    [Tooltip("Thời gian hồi chiêu giữa 2 lần húc")]
    [SerializeField] private float chargeCooldown = 4.5f;

    [Tooltip("Thời gian khựng lại gầm gừ/nhấp nháy lấy đà trước khi lao")]
    [SerializeField] private float windupDuration = 0.65f;

    [Tooltip("Tốc độ phóng húc cực nhanh")]
    [SerializeField] private float chargeSpeed = 11.5f;

    [Tooltip("Thời gian tối đa của một cú húc (giây)")]
    [SerializeField] private float chargeDuration = 0.85f;

    [Tooltip("Sát thương cực mạnh khi húc trúng người chơi")]
    [SerializeField] private int chargeDamage = 28;

    [Tooltip("Thời gian bị choáng / thở dốc sau cú húc")]
    [SerializeField] private float stunDuration = 1.0f;

    [Header("Visual Feedback")]
    [SerializeField] private Color windupColor = new Color(1f, 0.25f, 0.2f, 1f); // Đỏ cam cảnh báo
    [SerializeField] private Color stunnedColor = new Color(0.6f, 0.6f, 0.7f, 1f); // Xám xanh kiệt sức

    private ChargerState _state = ChargerState.Approaching;
    private int _currentHP;
    private float _nextChargeReadyTime;
    private Vector2 _chargeDirection;

    private Transform _playerTransform;
    private PlayerHealth _playerHealth;
    private SpriteRenderer _spriteRenderer;
    private Animator _animator;
    private Rigidbody2D _rb;
    private Collider2D _col;
    private Color _originalColor = Color.white;

    private void Awake()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
        if (_spriteRenderer != null) _originalColor = _spriteRenderer.color;

        _animator = GetComponent<Animator>();
        _rb = GetComponent<Rigidbody2D>();
        _col = GetComponent<Collider2D>();

        // Đảm bảo không bị trọng lực hút rơi và không bị xoay tròn khi va chạm vật lý
        if (_rb != null)
        {
            _rb.gravityScale = 0f;
            _rb.freezeRotation = true;
        }

        // Tự động đảm bảo tag là Zombie để đạn và hệ thống ngắm tự động nhận diện
        if (!CompareTag("Zombie"))
        {
            gameObject.tag = "Zombie";
        }
    }

    private void Start()
    {
        _currentHP = maxHP;
        _nextChargeReadyTime = Time.time + Random.Range(1.0f, 2.5f); // Nhịp húc khởi đầu lệch nhau
        FindPlayerTarget();
    }

    private void Update()
    {
        if (_state == ChargerState.Dead) return;

        // Tìm lại người chơi nếu mục tiêu bị mất
        if (_playerTransform == null || !_playerTransform.gameObject.activeInHierarchy)
        {
            FindPlayerTarget();
            if (_playerTransform == null) return;
        }

        // Tự hủy nếu xe đã vượt qua quá xa trong màn lái xe
        if (_playerTransform.position.x - transform.position.x > 30f)
        {
            Destroy(gameObject);
            return;
        }

        // Xử lý logic theo từng trạng thái
        switch (_state)
        {
            case ChargerState.Approaching:
                HandleApproaching();
                break;

            case ChargerState.Windup:
            case ChargerState.Charging:
            case ChargerState.Stunned:
                // Được quản lý qua Coroutine
                break;
        }
    }

    // =========================================================================
    // 1. DI CHUYỂN TIẾP CẬN BÌNH THƯỜNG
    // =========================================================================
    private void HandleApproaching()
    {
        Vector2 toPlayer = (Vector2)_playerTransform.position - (Vector2)transform.position;
        float distance = toPlayer.magnitude;

        // Lật mặt theo hướng đi
        UpdateFacingDirection(toPlayer.x);

        // Kiểm tra điều kiện kích hoạt cú húc
        if (Time.time >= _nextChargeReadyTime && distance <= chargeTriggerDistance && distance > 1.5f)
        {
            StartCoroutine(ChargeRoutine());
            return;
        }

        // Di chuyển tiếp cận người chơi
        Vector2 moveDir = toPlayer.normalized;
        if (_rb != null && _rb.bodyType == RigidbodyType2D.Dynamic)
        {
            _rb.linearVelocity = moveDir * walkSpeed;
        }
        else
        {
            transform.position += (Vector3)(moveDir * walkSpeed * Time.deltaTime);
        }

        UpdateAnimator(moveDir, walkSpeed);
    }

    // =========================================================================
    // 2. CHUỖI KỸ NĂNG: WINDUP -> CHARGE DASH -> STUNNED
    // =========================================================================
    private IEnumerator ChargeRoutine()
    {
        // --- BƯỚC 1: WINDUP (LẤY ĐÀ & CẢNH BÁO) ---
        _state = ChargerState.Windup;
        if (_rb != null) _rb.linearVelocity = Vector2.zero;

        // Khóa hướng húc thẳng vào vị trí người chơi tại thời điểm này
        _chargeDirection = ((Vector2)_playerTransform.position - (Vector2)transform.position).normalized;
        UpdateFacingDirection(_chargeDirection.x);

        // Nhấp nháy màu đỏ cảnh báo người chơi
        float windupTimer = 0f;
        while (windupTimer < windupDuration)
        {
            windupTimer += Time.deltaTime;
            if (_spriteRenderer != null)
            {
                // Nhấp nháy giữa màu gốc và màu đỏ cảnh báo
                float pingPong = Mathf.PingPong(windupTimer * 8f, 1f);
                _spriteRenderer.color = Color.Lerp(_originalColor, windupColor, pingPong);
            }
            yield return null;
        }

        if (_spriteRenderer != null) _spriteRenderer.color = windupColor;

        // --- BƯỚC 2: CHARGING (HÚC LAO THẲNG) ---
        _state = ChargerState.Charging;
        UpdateAnimator(_chargeDirection, chargeSpeed);

        float chargeTimer = 0f;
        while (chargeTimer < chargeDuration && _state == ChargerState.Charging)
        {
            chargeTimer += Time.deltaTime;

            if (_rb != null && _rb.bodyType == RigidbodyType2D.Dynamic)
            {
                _rb.linearVelocity = _chargeDirection * chargeSpeed;
            }
            else
            {
                transform.position += (Vector3)(_chargeDirection * chargeSpeed * Time.deltaTime);
            }

            yield return null;
        }

        // --- BƯỚC 3: STUNNED (CHOÁNG & THỞ DỐC) ---
        if (_state != ChargerState.Dead)
        {
            yield return StartCoroutine(StunRoutine());
        }
    }

    private IEnumerator StunRoutine()
    {
        _state = ChargerState.Stunned;
        if (_rb != null) _rb.linearVelocity = Vector2.zero;
        if (_spriteRenderer != null) _spriteRenderer.color = stunnedColor;

        UpdateAnimator(Vector2.zero, 0f);

        // Đứng im kiệt sức
        yield return new WaitForSeconds(stunDuration);

        if (_state != ChargerState.Dead)
        {
            if (_spriteRenderer != null) _spriteRenderer.color = _originalColor;
            _nextChargeReadyTime = Time.time + chargeCooldown;
            _state = ChargerState.Approaching;
        }
    }

    // =========================================================================
    // 3. VA CHẠM & SÁT THƯƠNG
    // =========================================================================
    private void OnTriggerEnter2D(Collider2D other)
    {
        HandleImpact(other);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        HandleImpact(collision.collider);
    }

    private void HandleImpact(Collider2D other)
    {
        if (_state == ChargerState.Dead) return;

        // Đâm trúng Người Chơi (Xe hoặc Người đi bộ)
        if (other.CompareTag("Player") || other.GetComponent<OnFootPlayerController>() != null || other.GetComponent<PlayerHealth>() != null)
        {
            PlayerHealth health = other.GetComponent<PlayerHealth>() ?? _playerHealth;
            if (health != null)
            {
                int damageToDeal = (_state == ChargerState.Charging) ? chargeDamage : touchDamage;
                health.TakeDamage(damageToDeal);
            }

            // Húc trúng mục tiêu -> Dừng đà lao và chuyển sang trạng thái Stun
            if (_state == ChargerState.Charging)
            {
                _state = ChargerState.Stunned;
            }
        }
        // Đâm trúng Tường Đấu Trường hoặc Chướng ngại vật
        else if (_state == ChargerState.Charging && (other.CompareTag("Obstacle") || other.name.Contains("Wall")))
        {
            _state = ChargerState.Stunned;
        }
    }

    // =========================================================================
    // 4. NHẬN SÁT THƯƠNG & CHẾT
    // =========================================================================
    public void TakeDamage(int damage)
    {
        if (_state == ChargerState.Dead) return;

        _currentHP -= damage;
        StartCoroutine(DamageFlashRoutine());

        if (_currentHP <= 0)
        {
            Die();
        }
    }

    private IEnumerator DamageFlashRoutine()
    {
        if (_spriteRenderer != null)
        {
            _spriteRenderer.color = Color.white;
            yield return new WaitForSeconds(0.08f);
            if (_state == ChargerState.Charging || _state == ChargerState.Windup)
            {
                _spriteRenderer.color = windupColor;
            }
            else if (_state == ChargerState.Stunned)
            {
                _spriteRenderer.color = stunnedColor;
            }
            else
            {
                _spriteRenderer.color = _originalColor;
            }
        }
    }

    private void Die()
    {
        _state = ChargerState.Dead;
        StopAllCoroutines();

        // Tắt collider và vật lý
        if (_col != null) _col.enabled = false;
        if (_rb != null)
        {
            _rb.linearVelocity = Vector2.zero;
            _rb.simulated = false;
        }

        // Tăng Kill Count nếu có DistanceTracker
        DistanceTrackerUI tracker = FindAnyObjectByType<DistanceTrackerUI>();
        if (tracker != null)
        {
            tracker.AddZombieKill();
        }

        // Rơi ngọc kinh nghiệm nếu ở chế độ Đấu trường (Brotato)
        if (ArenaManager.Instance != null || BrotatoLevelSystem.Instance != null)
        {
            GameObject gem = new GameObject("MaterialGem");
            gem.transform.position = transform.position;
            gem.AddComponent<MaterialGem>();
        }

        // Animation chết hoặc biến mất
        if (_animator != null)
        {
            _animator.SetTrigger("knock");
            Destroy(gameObject, 0.7f);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // =========================================================================
    // 5. HELPER FUNCTIONS
    // =========================================================================
    private void FindPlayerTarget()
    {
        // 1. Ưu tiên tìm nhân vật đi bộ OnFootPlayer (Arena hoặc Boss)
        OnFootPlayerController onFoot = FindAnyObjectByType<OnFootPlayerController>();
        if (onFoot != null && onFoot.gameObject.activeInHierarchy)
        {
            _playerTransform = onFoot.transform;
            _playerHealth = onFoot.GetComponent<PlayerHealth>();
            return;
        }

        // 2. Tìm xe Player Tag trong màn lái xe
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null && playerObj.activeInHierarchy)
        {
            _playerTransform = playerObj.transform;
            _playerHealth = playerObj.GetComponent<PlayerHealth>();
        }
    }

    private void UpdateFacingDirection(float dirX)
    {
        if (_spriteRenderer != null && Mathf.Abs(dirX) > 0.05f)
        {
            _spriteRenderer.flipX = dirX < 0f;
        }
    }

    private void UpdateAnimator(Vector2 moveDir, float speed)
    {
        if (_animator == null) return;
        _animator.SetFloat("MoveX", moveDir.x);
        _animator.SetFloat("MoveY", moveDir.y);
        _animator.SetFloat("Speed", speed);
    }

    public ChargerState GetCurrentState() => _state;
}
