using System.Collections;
using UnityEngine;

// Hệ thống Vật Phẩm Hồi Máu Rơi Ngẫu Nhiên (Health Pack / Medkit Drop):
// - Rơi từ quái khi bị tiêu diệt theo tỉ lệ phần trăm.
// - Tự động tạo hình Sprite Túi Cứu Thương Pixel Art (hộp trắng chữ thập đỏ phát sáng).
// - Khi người chơi lại gần hoặc chạm vào, hồi phục lượng HP khẩn cấp.
// - Dễ dàng tùy chỉnh thông số (lượng máu hồi, tỉ lệ rơi, thời gian tồn tại, ảnh custom) trong Inspector.
public class ArenaPowerUp : MonoBehaviour
{
    [Header("Medkit Settings (Tùy chỉnh trong Inspector)")]
    [Tooltip("Lượng máu hồi phục khi nhặt túi cứu thương")]
    [SerializeField] private int healAmount = 35;

    [Tooltip("Thời gian tồn tại tối đa trước khi tự hủy (giây)")]
    [SerializeField] private float lifetime = 25f;

    [Tooltip("Bán kính nhặt vật phẩm xung quanh người chơi")]
    [SerializeField] private float collectRadius = 0.8f;

    [Header("Visuals (Tùy chọn)")]
    [Tooltip("Ảnh hiển thị tùy biến. Nếu để trống, game sẽ tự tạo hình Túi Cứu Thương Pixel Art")]
    [SerializeField] private Sprite customSprite;

    private Transform _playerTransform;
    private PlayerHealth _playerHealth;
    private SpriteRenderer _sr;
    private float _spawnTime;
    private bool _isCollected = false;

    private void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();
        if (_sr == null) _sr = gameObject.AddComponent<SpriteRenderer>();

        CircleCollider2D col = GetComponent<CircleCollider2D>();
        if (col == null) col = gameObject.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = collectRadius;

        _spawnTime = Time.time;
    }

    private void Start()
    {
        ApplyVisuals();
        FindPlayer();
    }

    private void Update()
    {
        if (_isCollected) return;

        if (_playerTransform == null)
        {
            FindPlayer();
            if (_playerTransform == null) return;
        }

        // Hiệu ứng lơ lửng và nhấp nháy phát sáng nhẹ nhàng trên mặt đất
        float floatY = Mathf.Sin((Time.time - _spawnTime) * 4f) * 0.08f;
        transform.position += new Vector3(0f, floatY * Time.deltaTime, 0f);

        float pulse = 1f + Mathf.PingPong((Time.time - _spawnTime) * 1.5f, 0.15f);
        transform.localScale = new Vector3(pulse, pulse, 1f);

        // Nhấp nháy nhanh khi sắp hết thời gian tồn tại (5 giây cuối)
        float timeRemaining = lifetime - (Time.time - _spawnTime);
        if (timeRemaining <= 5f)
        {
            if (_sr != null)
            {
                float blinkAlpha = Mathf.PingPong(Time.time * 8f, 1f);
                Color c = _sr.color;
                c.a = Mathf.Clamp(blinkAlpha, 0.2f, 1f);
                _sr.color = c;
            }

            if (timeRemaining <= 0f)
            {
                Destroy(gameObject);
                return;
            }
        }

        // Kiểm tra khoảng cách nhặt
        float dist = Vector2.Distance(transform.position, _playerTransform.position);
        if (dist <= collectRadius)
        {
            Collect();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_isCollected) return;
        if (other.CompareTag("Player") || other.GetComponent<OnFootPlayerController>() != null)
        {
            Collect();
        }
    }

    private void FindPlayer()
    {
        OnFootPlayerController player = FindAnyObjectByType<OnFootPlayerController>();
        if (player != null && player.gameObject.activeInHierarchy)
        {
            _playerTransform = player.transform;
            _playerHealth = player.GetComponent<PlayerHealth>();
            return;
        }

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            _playerTransform = playerObj.transform;
            _playerHealth = playerObj.GetComponent<PlayerHealth>();
        }
    }

    private void Collect()
    {
        if (_isCollected) return;
        _isCollected = true;

        if (_playerHealth != null)
        {
            _playerHealth.Heal(healAmount);
            Debug.Log($"<color=#55FF55>[PowerUp] Picked up Medkit! Restored {healAmount} HP.</color>");
        }

        Destroy(gameObject);
    }

    // =========================================================================
    // TẠO HÌNH ẢNH SPRITE TÚI CỨU THƯƠNG PIXEL PROCEDURAL
    // =========================================================================
    private void ApplyVisuals()
    {
        if (_sr == null) return;

        _sr.sortingOrder = 20; // Nổi lên trên mặt đất và ngọc

        if (customSprite != null)
        {
            _sr.sprite = customSprite;
            return;
        }

        _sr.sprite = CreateMedkitSprite();
        _sr.color = Color.white;
    }

    // Vẽ Túi cứu thương Pixel (Hộp trắng viền đỏ dấu thập đỏ)
    private Sprite CreateMedkitSprite()
    {
        int size = 20;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;

        Color red = new Color(0.9f, 0.15f, 0.2f, 1f);
        Color white = Color.white;
        Color clear = new Color(0, 0, 0, 0);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                // Thân hộp (hình vuông bo nhẹ góc)
                if (x >= 2 && x <= 17 && y >= 3 && y <= 16)
                {
                    // Dấu chữ thập y tế ở giữa
                    bool isCrossH = (y >= 8 && y <= 11 && x >= 5 && x <= 14);
                    bool isCrossV = (x >= 8 && x <= 11 && y >= 5 && y <= 14);

                    if (isCrossH || isCrossV)
                    {
                        tex.SetPixel(x, y, red);
                    }
                    else
                    {
                        tex.SetPixel(x, y, white);
                    }
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

    // =========================================================================
    // HÀM STATIC HỖ TRỢ SPAWN TIỆN LỢI TỪ CODE
    // =========================================================================
    public static ArenaPowerUp Spawn(Vector3 position)
    {
        GameObject go = new GameObject("Medkit_Drop");
        go.transform.position = position;
        return go.AddComponent<ArenaPowerUp>();
    }

    public static bool TryDropRandom(Vector3 position, float dropChancePercent = 3.5f)
    {
        if (Random.Range(0f, 100f) > dropChancePercent) return false;

        Spawn(position);
        return true;
    }
}
