using UnityEngine;

// Điều khiển hành vi Zombie: Di chuyển rượt đuổi Player và tự động đổi hình ảnh/animation theo 4 hướng hoặc 8 hướng
public class Zombie : MonoBehaviour
{
    [Header("Stats")]
    // Lượng máu tối đa của Zombie
    [SerializeField] private int maxHP = 50;

    // Tốc độ di chuyển khi rượt đuổi người chơi
    [SerializeField] private float moveSpeed = 2.5f;

    // Sát thương gây ra cho Player khi cắn trúng
    [SerializeField] private int attackDamage = 15;

    [Header("Directional Sprites (Tùy chọn không cần Animator)")]
    // Mảng 8 Sprite tương ứng 8 hướng (0: Nam/Dưới, 1: Tây Nam, 2: Tây/Trái, 3: Tây Bắc, 4: Bắc/Trên, 5: Đông Bắc, 6: Đông/Phải, 7: Đông Nam)
    // Nếu chỉ có 4 hướng: 0: Dưới (Down), 1: Trái (Left), 2: Trên (Up), 3: Phải (Right)
    [SerializeField] private Sprite[] directionalSprites;

    [Header("Cleanup")]
    // Khoảng cách phía sau Player để tự hủy Zombie tránh rác bộ nhớ
    [SerializeField] private float despawnDistanceBehind = 25f;

    private int _currentHP;
    private Transform _playerTransform;
    private PlayerHealth _playerHealth;
    private SpriteRenderer _spriteRenderer;
    private Animator _animator;
    private Vector2 _moveDirection;

    private void Awake()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
        _animator = GetComponent<Animator>();
    }

    private void Start()
    {
        _currentHP = maxHP;

        // Tìm Player trong Scene
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            _playerTransform = playerObj.transform;
            _playerHealth = playerObj.GetComponent<PlayerHealth>();
        }
    }

    private void Update()
    {
        if (_playerTransform == null) return;

        // 1. Tính toán hướng và di chuyển rượt đuổi Player
        ChasePlayer();

        // 2. Cập nhật hướng nhìn (Animation / Sprite 4 hướng hoặc 8 hướng)
        UpdateDirectionVisuals();

        // 3. Tự hủy nếu xe đã chạy qua và bỏ xa phía sau
        if (_playerTransform.position.x - transform.position.x > despawnDistanceBehind)
        {
            Destroy(gameObject);
        }
    }

    // Di chuyển Zombie hướng về phía người chơi
    private void ChasePlayer()
    {
        _moveDirection = (_playerTransform.position - transform.position).normalized;
        transform.position += (Vector3)(_moveDirection * moveSpeed * Time.deltaTime);
    }

    // Cập nhật hình ảnh/Animation theo góc di chuyển thực tế
    private void UpdateDirectionVisuals()
    {
        // Cách 1: Nếu sử dụng Animator Controller (Blend Tree 4 hướng)
        if (_animator != null)
        {
            _animator.SetFloat("MoveX", _moveDirection.x);
            _animator.SetFloat("MoveY", _moveDirection.y);
            _animator.SetFloat("Speed", _moveDirection.sqrMagnitude);
            return;
        }

        // Cách 2: Nếu sử dụng mảng Sprite trực tiếp (nhẹ, nhanh và không cần setup Animator)
        if (_spriteRenderer != null && directionalSprites != null && directionalSprites.Length > 0)
        {
            // Tính góc xoay từ vector hướng (Góc từ -180 đến +180 độ)
            float angle = Mathf.Atan2(_moveDirection.y, _moveDirection.x) * Mathf.Rad2Deg;

            if (directionalSprites.Length >= 8)
            {
                // Quy đổi sang 8 hướng: 0: Down, 1: Down-Left, 2: Left, 3: Up-Left, 4: Up, 5: Up-Right, 6: Right, 7: Down-Right
                int dirIndex = Mathf.RoundToInt((angle + 90f) / 45f);
                dirIndex = (dirIndex % 8 + 8) % 8;
                _spriteRenderer.sprite = directionalSprites[dirIndex];
            }
            else if (directionalSprites.Length >= 4)
            {
                // Quy đổi sang 4 hướng: 0: Down (Dưới), 1: Left (Trái), 2: Up (Trên), 3: Right (Phải)
                int dirIndex = Mathf.RoundToInt((angle + 90f) / 90f);
                dirIndex = (dirIndex % 4 + 4) % 4;
                _spriteRenderer.sprite = directionalSprites[dirIndex];
            }
        }
    }

    // Nhận sát thương từ đạn bắn hoặc từ cú đâm của xe
    public void TakeDamage(int amount)
    {
        _currentHP -= amount;

        if (_currentHP <= 0)
        {
            Die();
        }
    }

    // Xử lý khi Zombie bị tiêu diệt
    private void Die()
    {
        // Báo cho UI tăng số đếm Zombie Kill nếu có
        DistanceTrackerUI tracker = FindAnyObjectByType<DistanceTrackerUI>();
        if (tracker != null)
        {
            tracker.AddZombieKill();
        }

        Destroy(gameObject);
    }

    // Gây sát thương cho Player nếu va chạm mà không bị húc chết
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            if (_playerHealth != null)
            {
                _playerHealth.TakeDamage(attackDamage);
            }
        }
    }
}