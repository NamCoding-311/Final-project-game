using UnityEngine;

// Điều khiển viên đạn: Tự động cộng hưởng vận tốc xe (Inherited Velocity) để đạn luôn xé gió bay vọt ra phía trước
public class Bullet : MonoBehaviour
{
    [Header("Bullet Settings")]
    // Tốc độ bay cơ bản của đạn
    [SerializeField] private float speed = 25f;

    // Thời gian tồn tại tối đa trước khi tự hủy
    [SerializeField] private float lifeTime = 2.5f;

    // Layer của chướng ngại vật để đạn va chạm và biến mất
    [SerializeField] private LayerMask obstacleLayer;

    private Vector2 _dir;
    private int _damage = 10;
    private float _inheritedForwardSpeed = 0f;

    // Cài đặt hướng bay và xoay đầu đạn thẳng theo hướng đó
    public void SetDirection(Vector2 dir)
    {
        _dir = dir.normalized;

        float angle = Mathf.Atan2(_dir.y, _dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    public void SetDamage(int dmg) => _damage = dmg;

    // Kế thừa vận tốc của chiếc xe: Đạn bay bằng tốc độ đạn + tốc độ xe
    public void SetInheritedSpeed(float carSpeed)
    {
        _inheritedForwardSpeed = carSpeed;
    }

    private void Start()
    {
        Destroy(gameObject, lifeTime);
    }

    private void Update()
    {
        // Vận tốc thực = Hướng bắn * Tốc độ đạn + Đà tiến của xe theo trục X
        Vector3 velocity = (Vector3)(_dir * speed);
        velocity.x += _inheritedForwardSpeed;

        transform.position += velocity * Time.deltaTime;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Gây sát thương khi trúng Zombie hoặc ZombieBoss
        if (other.CompareTag("Zombie") || other.GetComponent<ZombieBoss>() != null)
        {
            Zombie zombie = other.GetComponent<Zombie>();
            if (zombie != null)
            {
                zombie.TakeDamage(_damage);
            }

            ZombieBoss boss = other.GetComponent<ZombieBoss>();
            if (boss != null)
            {
                boss.TakeDamage(_damage);
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