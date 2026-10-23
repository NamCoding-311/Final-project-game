using UnityEngine;

// Điều khiển viên đạn: Tự động cộng hưởng vận tốc xe (Inherited Velocity) để đạn luôn xé gió bay vọt ra phía trước
public class Bullet : MonoBehaviour
{
    [Header("Bullet Settings")]
    // Tốc độ bay cơ bản của đạn
    [SerializeField] protected float speed = 25f;

    // Thời gian tồn tại tối đa trước khi tự hủy
    [SerializeField] protected float lifeTime = 2.5f;

    // Layer của chướng ngại vật để đạn va chạm và biến mất
    [SerializeField] protected LayerMask obstacleLayer;

    protected Vector2 _dir;
    protected int _damage = 10;
    protected float _inheritedForwardSpeed = 0f;

    // Cài đặt hướng bay và xoay đầu đạn thẳng theo hướng đó
    public virtual void SetDirection(Vector2 dir)
    {
        _dir = dir.normalized;

        float angle = Mathf.Atan2(_dir.y, _dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    public virtual void SetDamage(int dmg) => _damage = dmg;

    // Kế thừa vận tốc của chiếc xe: Đạn bay bằng tốc độ đạn + tốc độ xe
    public void SetInheritedSpeed(float carSpeed)
    {
        _inheritedForwardSpeed = carSpeed;
    }

    protected virtual void Start()
    {
        Destroy(gameObject, lifeTime);
    }

    protected virtual void Update()
    {
        // Vận tốc thực = Hướng bắn * Tốc độ đạn + Đà tiến của xe theo trục X
        Vector3 velocity = (Vector3)(_dir * speed);
        velocity.x += _inheritedForwardSpeed;

        transform.position += velocity * Time.deltaTime;
    }

    protected virtual void OnTriggerEnter2D(Collider2D other)
    {
        // Gây sát thương khi trúng Zombie hoặc ZombieBoss
        if (other.CompareTag("Zombie") || other.GetComponent<ZombieBoss>() != null)
        {
            ApplyDamageToEnemy(other);
            OnBulletHit(other);
            Destroy(gameObject);
            return;
        }

        // Chạm vào chướng ngại vật/tường
        if (((1 << other.gameObject.layer) & obstacleLayer) != 0)
        {
            OnBulletHit(other);
            Destroy(gameObject);
        }
    }

    protected void ApplyDamageToEnemy(Collider2D other)
    {
        Zombie zombie = other.GetComponent<Zombie>();
        if (zombie != null) zombie.TakeDamage(_damage);

        ZombieJumper jumper = other.GetComponent<ZombieJumper>();
        if (jumper != null) jumper.TakeDamage(_damage);

        ZombieCharger charger = other.GetComponent<ZombieCharger>();
        if (charger != null) charger.TakeDamage(_damage);

        ZombieBoss boss = other.GetComponent<ZombieBoss>();
        if (boss != null) boss.TakeDamage(_damage);
    }

    // Điểm móc (hook) cho các loại đạn đặc biệt (Nổ, Bom xăng, Đạn độc) override
    protected virtual void OnBulletHit(Collider2D other) { }
}
