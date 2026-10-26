using UnityEngine;

// Điều khiển tảng đá do Boss ném: Bay vòng cung hình Parabol và gây sát thương khi va chạm
public class BossRock : MonoBehaviour
{
    [Header("Damage & Visuals")]
    [SerializeField] private int damage = 25;
    [SerializeField] private float rotateSpeed = 360f; // Tốc độ tự xoay tròn khi bay

    private Vector2 _startPos;
    private Vector2 _targetPos;
    private float _arcHeight = 2.5f;
    private float _duration = 1.0f;
    private float _elapsed = 0f;
    private bool _isLaunched = false;

    // Kích hoạt bắn viên đá bay vòng cung đến tọa độ mục tiêu
    public void Launch(Vector2 targetPos, float arcHeight = 2.5f, float speed = 12f)
    {
        _startPos = transform.position;
        _targetPos = targetPos;
        _arcHeight = arcHeight;

        float distance = Vector2.Distance(_startPos, _targetPos);
        _duration = Mathf.Max(0.5f, distance / speed);
        _elapsed = 0f;
        _isLaunched = true;
    }

    private void Update()
    {
        if (!_isLaunched) return;

        _elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(_elapsed / _duration);

        // Chuyển động Parabol: Lerp toạ độ X và Y, sau đó cộng thêm độ võng cong Parabol
        Vector2 currentPos = Vector2.Lerp(_startPos, _targetPos, t);
        currentPos.y += 4f * _arcHeight * t * (1f - t); // Đường cong Parabol đạt đỉnh ở giữa

        transform.position = currentPos;

        // Xoay viên đá liên tục tạo cảm giác bay mạnh mẽ
        transform.Rotate(0, 0, rotateSpeed * Time.deltaTime);

        // Khi đá tiếp đất/bay đến đích mà không trúng ai -> Tự vỡ/hủy
        if (t >= 1f)
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Bỏ qua va chạm với chính Boss hoặc Zombie khác
        if (other.CompareTag("Zombie") || other.GetComponent<ZombieBoss>() != null)
        {
            return;
        }

        // 1. Trúng nhân vật người chơi đi bộ (Trashcan)
        OnFootPlayerController onFoot = other.GetComponent<OnFootPlayerController>();
        if (onFoot != null)
        {
            onFoot.TakeDamage(damage);
            Destroy(gameObject);
            return;
        }

        // 2. Trúng xe của người chơi
        if (other.CompareTag("Player"))
        {
            PlayerHealth health = other.GetComponent<PlayerHealth>();
            if (health != null)
            {
                health.TakeDamage(damage);
            }
            Destroy(gameObject);
            return;
        }
    }
}
