using UnityEngine;

// Điều khiển nhân vật người đi bộ khi xuống xe: Di chuyển 8 hướng (WASD),
// giới hạn trong sàn đấu Boss (Arena), xoay súng theo chuột và nhận sát thương.
public class OnFootPlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    // Tốc độ di chuyển chạy bộ của nhân vật (mét/giây)
    [SerializeField] private float moveSpeed = 6f;

    [Header("Health & Combat Reference")]
    // Tham chiếu đến hệ thống máu chung của người chơi
    [SerializeField] private PlayerHealth playerHealth;

    [Header("Visuals")]
    [SerializeField] private SpriteRenderer characterSpriteRenderer;
    [SerializeField] private Transform gunHolderTransform;
    [SerializeField] private Color hitFlashColor = Color.red;

    [Header("Arena Boundaries (Giới hạn di chuyển trong sàn đấu Boss)")]
    // Mặc định tắt để có thể di chuyển tự do, chỉ kích hoạt khi vào sàn đấu Boss
    [SerializeField] private bool useArenaBounds = false;
    [SerializeField] private float minX = -1000f;
    [SerializeField] private float maxX = 1000f;
    [SerializeField] private float minY = -1.2f;
    [SerializeField] private float maxY = 3.2f;

    private Rigidbody2D _rb;
    private Vector2 _input;
    private Camera _cam;
    private Color _originalColor = Color.white;
    private bool _isInvulnerable = false;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        if (_rb != null)
        {
            _rb.bodyType = RigidbodyType2D.Kinematic;
            _rb.gravityScale = 0f;
        }

        if (characterSpriteRenderer == null)
        {
            characterSpriteRenderer = GetComponent<SpriteRenderer>();
        }

        if (characterSpriteRenderer != null)
        {
            _originalColor = characterSpriteRenderer.color;
        }

        _cam = Camera.main;
    }

    private void Start()
    {
        // Ưu tiên sử dụng PlayerHealth gắn riêng trên người đi bộ (Trashcan)
        if (playerHealth == null)
        {
            playerHealth = GetComponent<PlayerHealth>();
        }

        // Bỏ qua va chạm vật lý với xe để người chơi không bao giờ bị kẹt dính vào xe
        Collider2D myCol = GetComponent<Collider2D>();
        Player3LaneMovement car = FindAnyObjectByType<Player3LaneMovement>();
        if (car != null && myCol != null)
        {
            Collider2D[] carCols = car.GetComponents<Collider2D>();
            foreach (var c in carCols)
            {
                if (c != null && c != myCol)
                {
                    Physics2D.IgnoreCollision(myCol, c, true);
                }
            }
        }
    }

    private void Update()
    {
        HandleMovementInput();
        HandleFacingDirection();
    }

    private void FixedUpdate()
    {
        MoveCharacter();
    }

    // Đọc phím di chuyển WASD hoặc Mũi tên (trục ngang A/D hoặc Mũi tên Trái/Phải; trục dọc W/S hoặc Lên/Xuống)
    private void HandleMovementInput()
    {
        _input.x = Input.GetAxisRaw("Horizontal");
        _input.y = Input.GetAxisRaw("Vertical");
        _input.Normalize();
    }

    // Di chuyển nhân vật 8 hướng mượt mà và kẹp trong ranh giới
    private void MoveCharacter()
    {
        Vector2 nextPos = (Vector2)transform.position + (_input * moveSpeed * Time.fixedDeltaTime);

        // Luôn giữ nhân vật nằm trong mặt đường (trục Y)
        nextPos.y = Mathf.Clamp(nextPos.y, minY, maxY);

        // Chỉ kẹp trục X khi đang trong sàn đấu Boss (Arena)
        if (useArenaBounds)
        {
            nextPos.x = Mathf.Clamp(nextPos.x, minX, maxX);
        }

        if (_rb != null)
        {
            _rb.MovePosition(nextPos);
        }
        else
        {
            transform.position = nextPos;
        }
    }

    // Lật mặt nhân vật hướng theo con trỏ chuột
    private void HandleFacingDirection()
    {
        if (_cam == null) _cam = Camera.main;
        if (_cam == null || characterSpriteRenderer == null) return;

        Vector3 mouseScreen = Input.mousePosition;
        mouseScreen.z = -_cam.transform.position.z;
        Vector3 mouseWorld = _cam.ScreenToWorldPoint(mouseScreen);

        bool aimRight = mouseWorld.x >= transform.position.x;
        characterSpriteRenderer.flipX = !aimRight;
    }

    // Cài đặt ranh giới đấu trường khi bước xuống xe
    public void SetArenaBounds(float leftX, float rightX, float bottomY, float topY)
    {
        minX = leftX;
        maxX = rightX;
        minY = bottomY;
        maxY = topY;
        useArenaBounds = true;
    }

    // Nhận sát thương khi va chạm đòn đánh của Boss
    public void TakeDamage(int damage)
    {
        if (_isInvulnerable) return;

        if (playerHealth != null)
        {
            playerHealth.TakeDamage(damage);
        }

        // Chớp đỏ cảnh báo
        if (characterSpriteRenderer != null)
        {
            characterSpriteRenderer.color = hitFlashColor;
            CancelInvoke(nameof(ResetFlashColor));
            Invoke(nameof(ResetFlashColor), 0.15f);
        }

        // Bất tử nhẹ 0.4s tránh bị dính nhiều hit cùng lúc
        _isInvulnerable = true;
        Invoke(nameof(ResetInvulnerable), 0.4f);
    }

    private void ResetFlashColor()
    {
        if (characterSpriteRenderer != null)
        {
            characterSpriteRenderer.color = _originalColor;
        }
    }

    private void ResetInvulnerable()
    {
        _isInvulnerable = false;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Nhận sát thương khi va chạm Boss
        ZombieBoss boss = other.GetComponent<ZombieBoss>();
        if (boss != null)
        {
            TakeDamage(20);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (useArenaBounds)
        {
            Gizmos.color = Color.yellow;
            Vector3 center = new Vector3((minX + maxX) * 0.5f, (minY + maxY) * 0.5f, 0f);
            Vector3 size = new Vector3(maxX - minX, maxY - minY, 0f);
            Gizmos.DrawWireCube(center, size);
        }
    }
}
