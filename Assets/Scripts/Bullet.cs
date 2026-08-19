using UnityEngine;

// Điều khiển viên đạn: bay theo hướng chỉ định, tự động xoay đầu đạn và gây sát thương cho Zombie
public class Bullet : MonoBehaviour
{
    [Header("Bullet Settings")]
    // Tốc độ bay của viên đạn (nên nhanh hơn tốc độ xe, ví dụ 20 - 25)
    [SerializeField] private float speed = 22f;

    // Thời gian tồn tại tối đa trước khi tự hủy
    [SerializeField] private float lifeTime = 3f;

    // Layer của chướng ngại vật để đạn va chạm và biến mất
    [SerializeField] private LayerMask obstacleLayer;

    private Vector2 _dir;
    private int _damage = 10;

    // Cài đặt hướng bay và xoay đầu đạn thẳng theo hướng đó
    public void SetDirection(Vector2 dir)
    {
        _dir = dir.normalized;

        // Xoay hình ảnh viên đạn theo đúng góc bay
        float angle = Mathf.Atan2(_dir.y, _dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    public void SetDamage(int dmg) => _damage = dmg;

    private void Start()
    {
        Destroy(gameObject, lifeTime);
    }

    private void Update()
    {
        transform.position += (Vector3)(_dir * speed * Time.deltaTime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Gây sát thương khi trúng Zombie
        if (other.CompareTag("Zombie"))
        {
            Zombie zombie = other.GetComponent<Zombie>();
            if (zombie != null)
            {
                zombie.TakeDamage(_damage);
            }

            Destroy(gameObject);
            return;
        }

        // Chạm vào chướng ngại vật/tường
        if (((1 << other.gameObject.layer) & obstacleLayer) != 0)
        {
            Destroy(gameObject);
        }
    }
}