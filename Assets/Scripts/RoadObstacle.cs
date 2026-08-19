using UnityEngine;

// Quản lý các chướng ngại vật tĩnh trên làn đường (Rào chắn, Thùng gỗ, Xe hỏng, Đá cản)
public class RoadObstacle : MonoBehaviour
{
    [Header("Obstacle Settings")]
    // Sát thương gây cho Player nếu đâm phải
    [SerializeField] private int damageToPlayer = 25;

    // Giảm tốc độ xe khi đâm phải vật cản
    [SerializeField] private float speedPenalty = 3f;

    // Khoảng cách phía sau Player để tự hủy chướng ngại vật tránh rác bộ nhớ
    [SerializeField] private float despawnDistanceBehind = 30f;

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
        if (other.CompareTag("Player"))
        {
            // Trừ máu người chơi
            PlayerHealth health = other.GetComponent<PlayerHealth>();
            if (health != null)
            {
                health.TakeDamage(damageToPlayer);
            }

            // Làm xe giảm tốc độ
            Player3LaneMovement movement = other.GetComponent<Player3LaneMovement>();
            if (movement != null)
            {
                float currentSpeed = movement.GetCurrentSpeed();
                movement.SetSpeed(Mathf.Max(2f, currentSpeed - speedPenalty));
            }

            // Phá hủy chướng ngại vật sau va chạm (hoặc chạy hiệu ứng vỡ nát)
            Destroy(gameObject);
        }
    }
}
