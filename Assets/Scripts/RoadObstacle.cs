using UnityEngine;

// Quản lý các chướng ngại vật tĩnh trên đường (Rào chắn, Thùng phuy, Xe hỏng, Cọc tiêu, Vết dầu)
// Đâm phải chướng ngại vật CHỈ LÀM GIẢM TỐC ĐỘ XE, HOÀN TOÀN KHÔNG BỊ TRỪ MÁU (HP).
public class RoadObstacle : MonoBehaviour
{
    [Header("Speed Penalty (Giảm tốc độ)")]
    // Lượng tốc độ bị giảm khi xe đâm phải vật cản (ví dụ: đang chạy 12 km/h -> giảm còn 8 km/h)
    [SerializeField] private float speedPenalty = 4f;

    // Tốc độ tối thiểu sau khi đâm (không để xe bị dừng hẳn)
    [SerializeField] private float minSpeedAfterHit = 3f;

    [Header("Despawn")]
    // Khoảng cách phía sau Player để tự hủy vật cản tránh tốn bộ nhớ
    [SerializeField] private float despawnDistanceBehind = 35f;

    private Transform _playerTransform;

    private void Start()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            _playerTransform = player.transform;
        }
    }

    private void Update()
    {
        // Tự hủy khi xe đã đi qua và bỏ xa phía sau
        if (_playerTransform != null)
        {
            if (_playerTransform.position.x - transform.position.x > despawnDistanceBehind)
            {
                Destroy(gameObject);
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Player3LaneMovement movement = other.GetComponent<Player3LaneMovement>();
        if (movement == null) movement = other.GetComponentInParent<Player3LaneMovement>();

        if (movement != null || other.CompareTag("Player"))
        {
            // 1. Nếu Player đang bấm SHIFT (Dash): Húc bay vật cản ngay lập tức, KHÔNG bị giảm tốc độ
            if (movement != null && movement.IsDashing())
            {
                Destroy(gameObject);
                return;
            }

            // 2. Nếu đâm bình thường: CHỈ GIẢM TỐC ĐỘ XE (TUYỆT ĐỐI KHÔNG TRỪ MÁU HP)
            if (movement != null)
            {
                float currentSpeed = movement.GetCurrentSpeed();
                movement.SetSpeed(Mathf.Max(minSpeedAfterHit, currentSpeed - speedPenalty));
            }

            // Phá hủy vật cản sau va chạm
            Destroy(gameObject);
        }
    }
}
