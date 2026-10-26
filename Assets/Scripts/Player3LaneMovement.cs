using UnityEngine;

// Điều khiển xe chạy 3 làn đường: Chuyển làn bằng W/S hoặc Lên/Xuống, lướt Dash húc bay Zombie bằng phím SHIFT
public class Player3LaneMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    // Tốc độ chạy thẳng cơ bản về phía trước theo trục X (đã tối ưu để người chơi kịp ngắm bắn)
    [SerializeField] private float forwardSpeed = 6f;

    // Tốc độ tối đa khi tăng tốc tự nhiên
    [SerializeField] private float maxSpeed = 8.5f;

    // Gia tốc tự động tăng dần theo thời gian (giữ mức nhẹ để xe không bị vọt đi quá nhanh)
    [SerializeField] private float acceleration = 0.02f;

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
    // Số lượng làn đường xe có thể chạy (Mặc định 3 hoặc chuyển thành 4, 5 làn tùy ý)
    [Range(2, 6)]
    [SerializeField] private int numberOfLanes = 4;

    // Độ cao Y của tâm đường (điểm mốc chính giữa)
    [SerializeField] private float baseCenterY = 1.0f;

    // Khoảng cách giữa các làn đường theo trục Y
    [SerializeField] private float laneDistance = 1.0f;

    // Tốc độ chuyển đổi giữa các làn
    [SerializeField] private float laneChangeSpeed = 14f;

    [Header("Custom Lane Heights (Tùy chọn chỉnh tay toạ độ từng làn)")]
    // Bật lên nếu bạn muốn tự gõ tay chính xác toạ độ Y của 4 làn cho khớp với vạch kẻ đường
    [SerializeField] private bool useCustomLaneY = false;
    [SerializeField] private float[] customLaneY = new float[] { -0.5f, 0.4f, 1.4f, 2.3f };

    [Header("Hard Road Boundaries (Giới hạn mép vỉa hè)")]
    // Bật chặn cứng để xe tuyệt đối không bao giờ trôi ra ngoài vỉa hè
    [SerializeField] private bool clampToRoadBoundaries = true;
    [SerializeField] private float minYLimit = -1.2f; // Lề đường dưới
    [SerializeField] private float maxYLimit = 3.3f;  // Lề đường trên (giáp bờ sông)

    [Header("Combat & Collision")]
    // Tốc độ của xe khi bị Zombie cản đường lúc chạy bình thường (không Dash)
    [SerializeField] private float zombieBlockSpeed = 1.8f;

    // Khoảng cách từ tâm xe đến cản trước (mũi xe) để ủi Zombie dính trước mũi xe
    [SerializeField] private float frontBumperOffset = 1.75f;

    // Sát thương gây ra khi đâm Zombie lúc chạy bình thường (0 = không đâm chết, để Zombie cản xe)
    [SerializeField] private int normalRamDamage = 0;

    // Mức giảm tốc độ khi va chạm Zombie lúc chạy bình thường
    [SerializeField] private float speedLossOnRam = 1.0f;

    // Index làn hiện tại (0 là làn dưới cùng, tăng dần lên làn trên)
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

    // Lấy toạ độ Y của một làn theo chỉ số
    public float GetLaneY(int index)
    {
        if (useCustomLaneY && customLaneY != null && customLaneY.Length > 0)
        {
            index = Mathf.Clamp(index, 0, customLaneY.Length - 1);
            return customLaneY[index];
        }

        int count = Mathf.Max(2, numberOfLanes);
        index = Mathf.Clamp(index, 0, count - 1);
        float offset = (index - (count - 1) / 2f) * laneDistance;
        return baseCenterY + offset;
    }

    // Lấy tổng số lượng làn đang hoạt động
    public int GetTotalLanes()
    {
        if (useCustomLaneY && customLaneY != null && customLaneY.Length > 0)
        {
            return customLaneY.Length;
        }
        return Mathf.Max(2, numberOfLanes);
    }

    // Lấy danh sách toạ độ tất cả các làn (để Spawner rải vật cản/quái đúng làn)
    public float[] GetAllLanePositions()
    {
        int total = GetTotalLanes();
        float[] result = new float[total];
        for (int i = 0; i < total; i++)
        {
            result[i] = GetLaneY(i);
        }
        return result;
    }

    private void Awake()
    {
        Time.timeScale = 1f;

        _rb = GetComponent<Rigidbody2D>();
        if (_rb != null)
        {
            _rb.bodyType = RigidbodyType2D.Kinematic;
            _rb.gravityScale = 0f;
            _rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        }
    }

    private void Start()
    {
        _currentSpeed = 0f;

        // Tìm chỉ số làn gần nhất với vị trí đặt xe ban đầu
        float initialY = transform.position.y;
        float bestDiff = float.MaxValue;
        int bestLane = 0;
        int totalLanes = GetTotalLanes();

        for (int i = 0; i < totalLanes; i++)
        {
            float laneY = GetLaneY(i);
            float diff = Mathf.Abs(initialY - laneY);
            if (diff < bestDiff)
            {
                bestDiff = diff;
                bestLane = i;
            }
        }

        _currentLaneIndex = bestLane;
        _targetY = GetLaneY(_currentLaneIndex);
        transform.position = new Vector3(transform.position.x, _targetY, transform.position.z);
    }

    private void Update()
    {
        HandleLaneInput();
        HandleDashInput();
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

    // Xe tự động chạy đều về phía trước; phím Shift dùng để lướt Dash húc quái
    private void HandleDashInput()
    {
        // 1. Nhấn phím Shift (trái hoặc phải) để kích hoạt Dash
        bool pressShift = Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift);

        if (pressShift && Time.time >= _nextDashTime)
        {
            StartDash();
        }

        // 2. Nếu đang trong trạng thái Dash
        if (_isDashing)
        {
            if (Time.time >= _dashEndTime)
            {
                _isDashing = false;
            }
        }
        else
        {
            // Xe luôn tự động chạy đều với tốc độ forwardSpeed mà không cần nhấn phím A/D
            _currentSpeed = Mathf.MoveTowards(_currentSpeed, forwardSpeed, 6f * Time.deltaTime);
        }
    
        // Tăng tốc dần theo thời gian nếu có cài đặt gia tốc tự nhiên
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

    // Chuyển làn giữa các làn từ 0 đến GetTotalLanes() - 1
    private void ChangeLane(int direction)
    {
        int totalLanes = GetTotalLanes();
        int newLane = Mathf.Clamp(_currentLaneIndex + direction, 0, totalLanes - 1);

        if (newLane != _currentLaneIndex)
        {
            _currentLaneIndex = newLane;
            _targetY = GetLaneY(_currentLaneIndex);

            if (clampToRoadBoundaries)
            {
                _targetY = Mathf.Clamp(_targetY, minYLimit, maxYLimit);
            }
        }
    }

    // Di chuyển xe theo trục X và trượt làn theo trục Y đồng bộ theo từng khung hình (Update)
    private void MovePlayer()
    {
        float dt = Time.deltaTime;
        float newX = transform.position.x + (_currentSpeed * dt);
        float newY = Mathf.MoveTowards(transform.position.y, _targetY, laneChangeSpeed * dt);

        if (clampToRoadBoundaries)
        {
            newY = Mathf.Clamp(newY, minYLimit, maxYLimit);
        }

        Vector3 targetPos = new Vector3(newX, newY, transform.position.z);
        transform.position = targetPos;

        if (_rb != null)
        {
            _rb.position = targetPos;
        }
    }

    // Xử lý va chạm với Zombie (khi mới chạm vào)
    private void OnTriggerEnter2D(Collider2D other)
    {
        HandleZombieCollision(other);
    }

    // Xử lý cản trở khi Zombie vẫn đang đứng chắn trước đầu xe
    private void OnTriggerStay2D(Collider2D other)
    {
        HandleZombieCollision(other);
    }

    // Cơ chế cản trở và húc tiêu diệt Zombie theo trạng thái Dash
    private void HandleZombieCollision(Collider2D other)
    {
        Zombie zombie = other.GetComponent<Zombie>();
        if (zombie != null)
        {
            if (_isDashing)
            {
                // Khi đang Dash: Đủ xung lực và sát thương cực đại để đâm chết Zombie ngay lập tức
                zombie.TakeDamage(dashRamDamage);
            }
            else
            {
                // Khi chạy bình thường: KHÔNG đâm chết Zombie, Zombie cản lại làm xe khựng chậm
                _currentSpeed = Mathf.Min(_currentSpeed, zombieBlockSpeed);

                // Ghim Zombie trước cản trước mũi xe, tuyệt đối không cho chui lọt vào lòng xe
                PinZombieToFrontBumper(other.transform, other.attachedRigidbody);
            }
            return;
        }

        ZombieCharger charger = other.GetComponent<ZombieCharger>();
        if (charger != null)
        {
            if (_isDashing)
            {
                charger.TakeDamage(dashRamDamage);
            }
            else
            {
                _currentSpeed = Mathf.Min(_currentSpeed, zombieBlockSpeed * 0.75f);
                PinZombieToFrontBumper(other.transform, other.attachedRigidbody);
            }
            return;
        }

        ZombieJumper jumper = other.GetComponent<ZombieJumper>();
        if (jumper != null)
        {
            if (_isDashing)
            {
                jumper.TakeDamage(dashRamDamage);
            }
            else
            {
                _currentSpeed = Mathf.Min(_currentSpeed, zombieBlockSpeed);
                PinZombieToFrontBumper(other.transform, other.attachedRigidbody);
            }
            return;
        }
    }

    // Đẩy ghim Zombie luôn nằm ngay phía trước cản trước của xe
    private void PinZombieToFrontBumper(Transform zombieTransform, Rigidbody2D zombieRb)
    {
        if (zombieTransform == null) return;

        float frontBumperX = transform.position.x + frontBumperOffset;
        if (zombieTransform.position.x < frontBumperX)
        {
            Vector3 pinnedPos = new Vector3(frontBumperX, zombieTransform.position.y, zombieTransform.position.z);
            zombieTransform.position = pinnedPos;

            if (zombieRb != null)
            {
                zombieRb.position = new Vector2(frontBumperX, zombieRb.position.y);
            }
        }
    }

    public void SetSpeed(float newSpeed) => _currentSpeed = newSpeed;
    public float GetCurrentSpeed() => _currentSpeed;

    // Kiem tra xem xe co dang trong trang thai Dash khong
    public bool IsDashing() => _isDashing;

    public float GetMinYLimit() => minYLimit;
    public float GetMaxYLimit() => maxYLimit;

    // Vẽ các đường làn màu xanh và mép đường màu đỏ trên Scene view
    private void OnDrawGizmosSelected()
    {
        Vector3 currentPos = transform.position;

        Gizmos.color = Color.green;
        int totalLanes = GetTotalLanes();
        for (int i = 0; i < totalLanes; i++)
        {
            float laneY = GetLaneY(i);
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
