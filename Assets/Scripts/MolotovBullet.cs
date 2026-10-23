using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Đạn Bom Lửa / Molotov (Vũ Khí Mới):
// - Khi bay có màu cam rực lửa và xoay tròn nhẹ.
// - Khi chạm quái hoặc va chạm sàn, tạo ra Vùng Lửa (FireZone) tồn tại 3.5 giây.
// - Quái bước qua vùng lửa sẽ bị thiêu đốt liên tục (Damage Over Time).
public class MolotovBullet : Bullet
{
    [Header("Molotov Fire Settings")]
    [SerializeField] private float fireZoneDuration = 3.5f;
    [SerializeField] private float fireRadius = 1.6f;
    [SerializeField] private int burnDamagePerTick = 8;
    [SerializeField] private float tickInterval = 0.35f;

    private bool _hasExploded = false;

    protected override void Start()
    {
        base.Start();

        // Biến màu đạn sang màu cam rực lửa
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.color = new Color(1f, 0.45f, 0.1f, 1f);
        }
    }

    protected override void Update()
    {
        base.Update();
        // Xoay nhẹ viên đạn tạo cảm giác ném chai xăng
        transform.Rotate(0f, 0f, -480f * Time.deltaTime);
    }

    protected override void OnBulletHit(Collider2D other)
    {
        SpawnFireZone();
    }

    private void OnDestroy()
    {
        // Nếu đạn hết thời gian bay mà chưa nổ thì cũng bung lửa tại điểm rơi
        if (!_hasExploded && gameObject.scene.isLoaded)
        {
            SpawnFireZone();
        }
    }

    private void SpawnFireZone()
    {
        if (_hasExploded) return;
        _hasExploded = true;

        GameObject fireObj = new GameObject("FireZone_Molotov");
        fireObj.transform.position = transform.position;

        FireZone fireZone = fireObj.AddComponent<FireZone>();
        fireZone.Setup(fireZoneDuration, fireRadius, burnDamagePerTick, tickInterval);
    }
}

// Component Vùng Lửa Thiêu Đốt (FireZone)
public class FireZone : MonoBehaviour
{
    private float _duration;
    private float _radius;
    private int _tickDamage;
    private float _tickInterval;

    private SpriteRenderer _sr;
    private float _spawnTime;
    private float _nextTickTime;

    public void Setup(float duration, float radius, int tickDamage, float tickInterval)
    {
        _duration = duration;
        _radius = radius;
        _tickDamage = tickDamage;
        _tickInterval = tickInterval;
    }

    private void Awake()
    {
        _spawnTime = Time.time;
        _nextTickTime = Time.time + 0.1f;

        _sr = gameObject.AddComponent<SpriteRenderer>();
        _sr.sprite = CreateFireCircleSprite();
        _sr.sortingOrder = 5; // Dưới quái nhưng trên mặt đất
        _sr.color = new Color(1f, 0.4f, 0.05f, 0.7f); // Cam lửa bùng cháy
    }

    private void Update()
    {
        float elapsed = Time.time - _spawnTime;

        // Bập bùng ngọn lửa bằng scale và alpha
        float flickerScale = _radius * (1f + Mathf.Sin(Time.time * 16f) * 0.08f);
        transform.localScale = new Vector3(flickerScale, flickerScale, 1f);

        if (_sr != null)
        {
            float alpha = Mathf.Clamp01(1f - (elapsed / _duration));
            _sr.color = new Color(1f, 0.35f + Mathf.PingPong(Time.time * 6f, 0.25f), 0.05f, alpha * 0.75f);
        }

        // Thiêu đốt tất cả quái vật nằm trong vùng lửa theo nhịp tick
        if (Time.time >= _nextTickTime)
        {
            _nextTickTime = Time.time + _tickInterval;
            DealBurnDamage();
        }

        if (elapsed >= _duration)
        {
            Destroy(gameObject);
        }
    }

    private void DealBurnDamage()
    {
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, _radius * 0.5f);
        foreach (var col in colliders)
        {
            if (col == null || !col.gameObject.activeInHierarchy) continue;

            Zombie z = col.GetComponent<Zombie>();
            if (z != null) z.TakeDamage(_tickDamage);

            ZombieJumper j = col.GetComponent<ZombieJumper>();
            if (j != null) j.TakeDamage(_tickDamage);

            ZombieCharger c = col.GetComponent<ZombieCharger>();
            if (c != null) c.TakeDamage(_tickDamage);

            ZombieBoss b = col.GetComponent<ZombieBoss>();
            if (b != null) b.TakeDamage(_tickDamage);
        }
    }

    // Vẽ vùng tròn lửa procedural (hình vòng tròn ngọn lửa rực sáng)
    private Sprite CreateFireCircleSprite()
    {
        int size = 32;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;

        Color centerColor = new Color(1f, 0.85f, 0.3f, 1f); // Vàng sáng tâm lửa
        Color rimColor = new Color(1f, 0.25f, 0.05f, 0.9f);    // Đỏ cam rìa lửa
        Color clear = new Color(0, 0, 0, 0);

        float center = size * 0.5f;
        float maxR = size * 0.48f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                if (dist <= maxR)
                {
                    float t = dist / maxR;
                    Color c = Color.Lerp(centerColor, rimColor, t);
                    c.a = Mathf.SmoothStep(1f, 0f, t);
                    tex.SetPixel(x, y, c);
                }
                else
                {
                    tex.SetPixel(x, y, clear);
                }
            }
        }

        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 16f);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, _radius * 0.5f);
    }
}
