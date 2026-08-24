using UnityEngine;

// Điều khiển xe chạy 3 làn đường: Chuyển làn bằng W/S hoặc Lên/Xuống, lướt Dash húc bay Zombie bằng phím SHIFT
public class Player3LaneMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    // Tốc độ chạy thẳng cơ bản về phía trước theo trục X
    [SerializeField] private float forwardSpeed = 9f;

    // Tốc độ tối đa khi tăng tốc tự nhiên
    [SerializeField] private float maxSpeed = 16f;

    // Gia tốc tự động tăng dần theo thời gian
    [SerializeField] private float acceleration = 0.05f;

    [Header("Dash Settings (Kỹ năng Lướt bằng phím Shift)")]
    // Tốc độ cực đại khi nhấn Shift để Dash
    [SerializeField] private float dashSpeed = 7f;

    // Thời gian của cú lướt Dash (giây)
    [SerializeField] private float dashDuration = 0.25f;

    // Thời gian hồi chiêu giữa 2 lần Dash (giây)
    [SerializeField] private float dashCooldown = 1.5f;

    // Sát thương húc siêu mạnh khi đang Dash (húc bay Zombie ngay lập tức)
    [SerializeField] private int dashRamDamage = 300;

    [Header("Lane Configuration (Cài đặt Làn Đường)")]
    // Độ cao Y của Làn Giữa (tâm đường)
    [SerializeField] private float baseCenterY = -0.5f;

    // Khoảng cách giữa các làn đường theo trục Y
    [SerializeField] private float laneDistance = 1.1f;

    // Tốc độ chuyển đổi giữa các làn
    [SerializeField] private float laneChangeSpeed = 14f;

    // Giới hạn số làn (minLane = -1, maxLane = 1 -> 3 làn)
    [SerializeField] private int minLaneIndex = -1;
    [SerializeField] private int maxLaneIndex = 1;

    [Header("Hard Road Boundaries (Giới hạn mép vỉa hè)")]
    // Bật chặn cứng để xe tuyệt đối không bao giờ trôi ra ngoài vỉa hè
    [SerializeField] private bool clampToRoadBoundaries = true;
    [SerializeField] private float minYLimit = -2.2f; // Lề đường dưới
    [SerializeField] private float maxYLimit = 1.0f;  // Lề đường trên (giáp bờ sông)

    [Header("Combat & Collision")]
    // Sát thương gây ra khi đâm trực diện Zombie lúc bình thường
    [SerializeField] private int normalRamDamage = 80;

    // Giảm nhẹ tốc độ khi va chạm Zombie lúc chạy bình thường
    [SerializeField] private float speedLossOnRam = 1.0f;

    // Index làn hiện tại
    private int _currentLaneIndex = 0;

    // Tọa độ Y mục tiêu đang di chuyển tới
    private float _targetY;

    // Tốc độ hiện tại
    private float _currentSpeed;

    // Quản lý trạng thái Dash
    private bool _isDashing = false;
    private float _dashEndTime = 0f;
    private float _nextDashTime = 0f;

    private Rigidbody2D _rb;

    private void Awake()
    {
        Time.timeScale = 1f;

        _rb = GetComponent<Rigidbody2D>();
        if (_rb != null)
        {
            _rb.bodyType = RigidbodyType2D.Kinematic;
            _rb.gravityScale = 0f;
        }
    }

    private void Start()
    {
        _currentSpeed = forwardSpeed;

        // Tìm chỉ số làn gần nhất với vị trí đặt xe ban đầu
        float initialY = transform.position.y;
        float bestDiff = float.MaxValue;
        int bestLane = 0;

        for (int i = minLaneIndex; i <= maxLaneIndex; i++)
        {
            float laneY = baseCenterY + (i * laneDistance);
            float diff = Mathf.Abs(initialY - laneY);
            if (diff < bestDiff)
            {
                bestDiff = diff;
                bestLane = i;
            }
        }

        _currentLaneIndex = bestLane;
        _targetY = baseCenterY + (_currentLaneIndex * laneDistance);
        transform.position = new Vector3(transform.position.x, _targetY, transform.position.z);
    }

    private void Update()
    {
        HandleLaneInput();
        HandleDashInput();
    }

    private void FixedUpdate()
    {
        MovePlayer();
    }

    // Xử lý phím bấm chuyển làn (W/S hoặc Mũi tên Lên/Xuống)
    private void HandleLaneInput()
    {
        if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
        {
            ChangeLane(1); // Lên làn trên
        }
        else if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
        {
            ChangeLane(-1); // Xuống làn dưới
        }
    }

    // Xử lý kích hoạt kỹ năng Dash bằng phím SHIFT (đã bỏ phím A giảm tốc)
    private void HandleDashInput()
    {
        // 1. Nhấn phím Shift (trái hoặc phải) để kích hoạt Dash
        bool pressShift = Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift);

        if (pressShift && Time.time >= _nextDashTime)
        {
            StartDash();
        }

        // 2. Kiểm tra khi thời gian Dash kết thúc
        if (_isDashing)
        {
            if (Time.time >= _dashEndTime)
            {
                _isDashing = false;
            }
        }
        else
        {
            // Trở về tốc độ chạy bình thường khi không Dash
            _currentSpeed = Mathf.MoveTowards(_currentSpeed, forwardSpeed, 8f * Time.deltaTime);
        }

        // Tăng tốc dần theo thời gian nếu có cài đặt gia tốc
        if (acceleration > 0f && forwardSpeed < maxSpeed)
        {
            forwardSpeed += acceleration * Time.deltaTime;
        }
    }

    // Bắt đầu cú lướt Dash
    private void StartDash()
    {
        _isDashing = true;
        _currentSpeed = dashSpeed;
        _dashEndTime = Time.time + dashDuration;
        _nextDashTime = Time.time + dashCooldown;
    }

    // Chuyển làn có giới hạn minLaneIndex và maxLaneIndex
    private void ChangeLane(int direction)
    {
        int newLane = Mathf.Clamp(_currentLaneIndex + direction, minLaneIndex, maxLaneIndex);

        if (newLane != _currentLaneIndex)
        {
            _currentLaneIndex = newLane;
            _targetY = baseCenterY + (_currentLaneIndex * laneDistance);

            if (clampToRoadBoundaries)
            {
                _targetY = Mathf.Clamp(_targetY, minYLimit, maxYLimit);
            }
        }
    }

    // Di chuyển xe theo trục X và trượt làn theo trục Y
    private void MovePlayer()
    {
        float newX = transform.position.x + (_currentSpeed * Time.fixedDeltaTime);
        float newY = Mathf.MoveTowards(transform.position.y, _targetY, laneChangeSpeed * Time.fixedDeltaTime);

        if (clampToRoadBoundaries)
        {
            newY = Mathf.Clamp(newY, minYLimit, maxYLimit);
        }

        Vector2 targetPos = new Vector2(newX, newY);

        if (_rb != null)
        {
            _rb.MovePosition(targetPos);
        }
        else
        {
            transform.position = new Vector3(newX, newY, transform.position.z);
        }
    }

    // Xử lý va chạm húc Zombie
    private void OnTriggerEnter2D(Collider2D other)
    {
        Zombie zombie = other.GetComponent<Zombie>();
        if (zombie != null)
        {
            if (_isDashing)
            {
                // Khi đang Dash: Sát thương cực đại và KHÔNG bị giảm tốc độ
                zombie.TakeDamage(dashRamDamage);
            }
            else
            {
                // Khi chạy bình thường: Gây sát thương cơ bản và giảm nhẹ tốc độ
                zombie.TakeDamage(normalRamDamage);
                _currentSpeed = Mathf.Max(forwardSpeed * 0.6f, _currentSpeed - speedLossOnRam);
            }
        }
    }

    public void SetSpeed(float newSpeed) => _currentSpeed = newSpeed;
    public float GetCurrentSpeed() => _currentSpeed;

    // Kiểm tra xem xe có đang trong trạng thái Dash không
    public bool IsDashing() => _isDashing;

    // Vẽ các đường làn màu xanh và mép đường màu đỏ trên Scene view
    private void OnDrawGizmosSelected()
    {
        Vector3 currentPos = transform.position;

        Gizmos.color = Color.green;
        for (int i = minLaneIndex; i <= maxLaneIndex; i++)
        {
            float laneY = baseCenterY + (i * laneDistance);
            Vector3 startPoint = new Vector3(currentPos.x - 10f, laneY, 0f);
            Vector3 endPoint = new Vector3(currentPos.x + 30f, laneY, 0f);
            Gizmos.DrawLine(startPoint, endPoint);
        }

        if (clampToRoadBoundaries)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(new Vector3(currentPos.x - 10f, minYLimit, 0f), new Vector3(currentPos.x + 30f, minYLimit, 0f));
            Gizmos.DrawLine(new Vector3(currentPos.x - 10f, maxYLimit, 0f), new Vector3(currentPos.x + 30f, maxYLimit, 0f));
        }
    }
}
