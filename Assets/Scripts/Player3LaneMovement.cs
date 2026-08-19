using UnityEngine;

// Điều khiển xe hoặc nhân vật chạy ngang vô tận với 3 làn đường (Top, Middle, Bottom)
// Hỗ trợ tự động chạy thẳng theo trục X, chuyển làn bằng W/S hoặc Lên/Xuống
public class Player3LaneMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    // Tốc độ chạy thẳng về phía trước theo trục X
    [SerializeField] private float forwardSpeed = 8f;

    // Tốc độ tối đa khi tăng tốc
    [SerializeField] private float maxSpeed = 15f;

    // Gia tốc tăng dần theo thời gian (0 nếu muốn tốc độ cố định)
    [SerializeField] private float acceleration = 0.1f;

    [Header("Lane Settings")]
    // Khoảng cách giữa các làn đường theo trục Y (Ví dụ: Làn trên +2, Làn giữa 0, Làn dưới -2)
    [SerializeField] private float laneDistance = 2f;

    // Tốc độ chuyển đổi giữa các làn
    [SerializeField] private float laneChangeSpeed = 15f;

    [Header("Combat & Collision")]
    // Sát thương gây ra khi đâm trực diện vào Zombie
    [SerializeField] private int ramDamage = 100;

    // Giảm tốc độ khi đâm phải vật cản/zombie
    [SerializeField] private float speedLossOnRam = 1.5f;

    // Index làn hiện tại: -1 (Dưới), 0 (Giữa), 1 (Trên)
    private int _currentLaneIndex = 0;

    // Tọa độ Y mục tiêu đang di chuyển tới
    private float _targetY = 0f;

    // Tốc độ hiện tại
    private float _currentSpeed;

    private Rigidbody2D _rb;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
    }

    private void Start()
    {
        _currentSpeed = forwardSpeed;
        _targetY = transform.position.y;
    }

    private void Update()
    {
        HandleLaneInput();
        UpdateSpeed();
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

    // Thay đổi chỉ số làn đường và tính toán vị trí Y tương ứng
    private void ChangeLane(int direction)
    {
        // Giới hạn trong khoảng 3 làn: -1 (Dưới), 0 (Giữa), 1 (Trên)
        int newLane = Mathf.Clamp(_currentLaneIndex + direction, -1, 1);

        if (newLane != _currentLaneIndex)
        {
            _currentLaneIndex = newLane;
            _targetY = _currentLaneIndex * laneDistance;
        }
    }

    // Tăng tốc dần theo thời gian nếu có cấu hình gia tốc
    private void UpdateSpeed()
    {
        if (_currentSpeed < maxSpeed && acceleration > 0f)
        {
            _currentSpeed += acceleration * Time.deltaTime;
        }
    }

    // Di chuyển nhân vật về phía trước và trượt mượt mà sang làn mới
    private void MovePlayer()
    {
        float newX = transform.position.x + _currentSpeed * Time.fixedDeltaTime;
        float newY = Mathf.MoveTowards(transform.position.y, _targetY, laneChangeSpeed * Time.fixedDeltaTime);

        if (_rb != null)
        {
            _rb.MovePosition(new Vector2(newX, newY));
        }
        else
        {
            transform.position = new Vector3(newX, newY, transform.position.z);
        }
    }

    // Xử lý khi xe húc vào Zombie hoặc Chướng ngại vật trên đường
    private void OnTriggerEnter2D(Collider2D other)
    {
        // Nếu đâm phải Zombie
        Zombie zombie = other.GetComponent<Zombie>();
        if (zombie != null)
        {
            zombie.TakeDamage(ramDamage);

            // Giảm nhẹ tốc độ khi va chạm
            _currentSpeed = Mathf.Max(forwardSpeed * 0.5f, _currentSpeed - speedLossOnRam);
        }
    }

    // Hàm cho phép các script khác điều chỉnh tốc độ (ví dụ ăn vật phẩm Nitro)
    public void SetSpeed(float newSpeed)
    {
        _currentSpeed = newSpeed;
    }

    // Trả về tốc độ hiện tại của xe
    public float GetCurrentSpeed()
    {
        return _currentSpeed;
    }
}
