using UnityEngine;

// Viên ngọc xanh (Material Gem / XP) rơi từ quái khi chết trong chế độ Brotato:
// - Phát sáng và bập bùng nhẹ trên sàn.
// - Có cơ chế Nam Châm (Magnet): Tự động tăng tốc bay vút về phía người chơi khi đến gần.
// - Cộng Tiền & XP khi nhặt được.
public class MaterialGem : MonoBehaviour
{
    [Header("Values")]
    [SerializeField] private int xpValue = 1;
    [SerializeField] private float baseMagnetSpeed = 7f;
    [SerializeField] private float collectDistance = 0.5f;

    private Transform _playerTransform;
    private bool _isBeingMagnetized = false;
    private float _currentSpeed;
    private Vector3 _startPosition;
    private float _floatOffset;

    private void Awake()
    {
        // Tự tạo hình viên ngọc xanh phát sáng nếu Prefab chưa gắn Sprite
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr == null)
        {
            sr = gameObject.AddComponent<SpriteRenderer>();
        }

        if (sr.sprite == null)
        {
            sr.sprite = CreateGemSprite();
        }

        sr.color = new Color(0.1f, 1f, 0.35f, 1f); // Xanh lục ngọc bảo phát sáng
        sr.sortingOrder = 15; // Đảm bảo luôn nổi lên trên mặt đất và sàn đấu
        transform.localScale = new Vector3(0.75f, 0.75f, 1f);

        // Tự động thêm CircleCollider2D dạng Trigger nếu chưa có
        CircleCollider2D col = GetComponent<CircleCollider2D>();
        if (col == null)
        {
            col = gameObject.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.4f;
        }

        _floatOffset = Random.Range(0f, 10f);
        _startPosition = transform.position;
        _currentSpeed = baseMagnetSpeed;
    }

    private void Start()
    {
        FindPlayer();
    }

    private void Update()
    {
        if (_playerTransform == null)
        {
            FindPlayer();
            if (_playerTransform == null) return;
        }

        float distance = Vector2.Distance(transform.position, _playerTransform.position);

        // Lấy bán kính hút từ hệ thống Level hoặc mặc định 3.5m
        float magnetRange = BrotatoLevelSystem.Instance != null 
            ? BrotatoLevelSystem.Instance.GetMagnetRadius() 
            : 3.5f;

        // Nếu người chơi ở trong bán kính nam châm
        if (distance <= magnetRange)
        {
            _isBeingMagnetized = true;
        }

        // Hiệu ứng bay hút vào người chơi
        if (_isBeingMagnetized)
        {
            // Tăng tốc dần khi bay càng gần
            _currentSpeed += 15f * Time.deltaTime;
            transform.position = Vector3.MoveTowards(transform.position, _playerTransform.position, _currentSpeed * Time.deltaTime);

            if (distance <= collectDistance)
            {
                Collect();
            }
        }
        else
        {
            // Hiệu ứng bập bùng nhẹ trên mặt sàn
            float bobbing = Mathf.Sin((Time.time + _floatOffset) * 6f) * 0.08f;
            transform.position = new Vector3(_startPosition.x, _startPosition.y + bobbing, _startPosition.z);
        }
    }

    private void FindPlayer()
    {
        OnFootPlayerController player = FindAnyObjectByType<OnFootPlayerController>();
        if (player != null && player.gameObject.activeInHierarchy)
        {
            _playerTransform = player.transform;
            return;
        }

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            _playerTransform = playerObj.transform;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") || other.GetComponent<OnFootPlayerController>() != null)
        {
            Collect();
        }
    }

    private void Collect()
    {
        if (BrotatoLevelSystem.Instance != null)
        {
            BrotatoLevelSystem.Instance.AddXP(xpValue);
        }

        Destroy(gameObject);
    }

    // Vẽ hình viên ngọc lục giác / kim cương bằng code (16x16)
    private Sprite CreateGemSprite()
    {
        int size = 16;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point;

        Color transparent = new Color(0, 0, 0, 0);
        Color gemColor = Color.white;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                int dx = Mathf.Abs(x - size / 2);
                int dy = Mathf.Abs(y - size / 2);
                if (dx + dy <= size / 2)
                {
                    texture.SetPixel(x, y, gemColor);
                }
                else
                {
                    texture.SetPixel(x, y, transparent);
                }
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 16f);
    }
}
