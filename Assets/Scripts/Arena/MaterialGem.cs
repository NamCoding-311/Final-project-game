using System.Collections.Generic;
using UnityEngine;

// Hệ thống Đồng Xu 3 Bậc (Tiered Coin / Material Gem) trong chế độ Brotato:
// 1. Xu Bạc (Silver Coin - MonedaP): Tiền cơ bản nhất (1 Xu) - Drop từ Zombie thường.
// 2. Xu Vàng (Gold Coin - MonedaD): Tiền cao cấp (5 Xu) - Drop từ Zombie Jumper, Charger, Elite.
// 3. Xu Đỏ (Red Coin - MonedaR): Tiền quý hiếm (20 Xu) - Drop từ Zombie Boss, Elite Charger.
// - Hoạt ảnh xoay đồng xu lấp lánh trên sàn.
// - Nam châm hút về người chơi và tự động gom sạch cuối mỗi Wave.
public enum CoinTier
{
    Silver, // 1 Xu (Cơ bản)
    Gold,   // 5 Xu (Quái trâu)
    Red     // 20 Xu (Boss & Quái cực trâu)
}

public class MaterialGem : MonoBehaviour
{
    [Header("Coin / Gem Config")]
    [SerializeField] private CoinTier coinTier = CoinTier.Silver;
    [SerializeField] private int xpValue = 1;
    [SerializeField] private float baseMagnetSpeed = 7f;
    [SerializeField] private float collectDistance = 0.5f;

    [Header("Sprite Animation")]
    [SerializeField] private Sprite[] animationFrames;
    [SerializeField] private float frameRate = 0.09f;

    private Transform _playerTransform;
    private bool _isBeingMagnetized = false;
    private float _currentSpeed;
    private Vector3 _startPosition;
    private float _floatOffset;
    private SpriteRenderer _sr;

    private int _currentFrame = 0;
    private float _frameTimer = 0f;

    private void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();
        if (_sr == null) _sr = gameObject.AddComponent<SpriteRenderer>();

        CircleCollider2D col = GetComponent<CircleCollider2D>();
        if (col == null)
        {
            col = gameObject.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.35f;
        }

        _sr.sortingOrder = 15;
        _floatOffset = Random.Range(0f, 10f);
        _startPosition = transform.position;
        _currentSpeed = baseMagnetSpeed;

        // Nếu chưa cấu hình frame thì tự động tải theo tier
        if (animationFrames == null || animationFrames.Length == 0)
        {
            ApplyCoinTier(coinTier);
        }
    }

    private void Start()
    {
        FindPlayer();
    }

    public void ApplyCoinTier(CoinTier tier)
    {
        coinTier = tier;
        switch (tier)
        {
            case CoinTier.Silver:
                xpValue = 1;
                LoadCoinFrames("Assets/UI/Coin_Gems/MonedaP.png", "MonedaP");
                if (_sr != null) _sr.color = Color.white;
                transform.localScale = Vector3.one * 2.2f;
                break;

            case CoinTier.Gold:
                xpValue = 5;
                LoadCoinFrames("Assets/UI/Coin_Gems/MonedaD.png", "MonedaD");
                if (_sr != null) _sr.color = Color.white;
                transform.localScale = Vector3.one * 2.5f;
                break;

            case CoinTier.Red:
                xpValue = 20;
                LoadCoinFrames("Assets/UI/Coin_Gems/MonedaR.png", "MonedaR");
                if (_sr != null) _sr.color = Color.white;
                transform.localScale = Vector3.one * 2.8f;
                break;
        }
    }

    private void LoadCoinFrames(string path, string prefix)
    {
#if UNITY_EDITOR
        Object[] subAssets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(path);
        List<Sprite> list = new List<Sprite>();
        foreach (var a in subAssets)
        {
            if (a is Sprite s) list.Add(s);
        }
        if (list.Count > 0)
        {
            animationFrames = list.ToArray();
            if (_sr != null) _sr.sprite = animationFrames[0];
            return;
        }
#endif
        // Tải từ Resources nếu có
        Sprite[] resSprites = Resources.LoadAll<Sprite>("Coins/" + prefix);
        if (resSprites != null && resSprites.Length > 0)
        {
            animationFrames = resSprites;
            if (_sr != null) _sr.sprite = animationFrames[0];
            return;
        }

        // Fallback procedural sprite nếu chưa nạp được
        if (_sr != null && _sr.sprite == null)
        {
            _sr.sprite = CreateGemSprite();
        }
    }

    // Tương thích ngược với code cũ
    public void SetGemData(int value, Color color, float scale = 0.75f)
    {
        xpValue = value;
        transform.localScale = new Vector3(scale, scale, 1f);
        if (_sr != null) _sr.color = color;
    }

    public void ForceMagnetize()
    {
        _isBeingMagnetized = true;
        _currentSpeed = Mathf.Max(_currentSpeed, baseMagnetSpeed * 1.8f);
    }

    public static void MagnetizeAllGems()
    {
        MaterialGem[] allGems = FindObjectsByType<MaterialGem>(FindObjectsSortMode.None);
        foreach (var gem in allGems)
        {
            if (gem != null) gem.ForceMagnetize();
        }
    }

    private void Update()
    {
        // 1. Hoạt ảnh xoay đồng xu
        if (animationFrames != null && animationFrames.Length > 1)
        {
            _frameTimer += Time.deltaTime;
            if (_frameTimer >= frameRate)
            {
                _frameTimer = 0f;
                _currentFrame = (_currentFrame + 1) % animationFrames.Length;
                if (_sr != null) _sr.sprite = animationFrames[_currentFrame];
            }
        }

        // 2. Nam châm hút về phía người chơi
        if (_playerTransform == null)
        {
            FindPlayer();
            if (_playerTransform == null) return;
        }

        float distance = Vector2.Distance(transform.position, _playerTransform.position);
        float magnetRange = BrotatoLevelSystem.Instance != null 
            ? BrotatoLevelSystem.Instance.GetMagnetRadius() 
            : 3.5f;

        if (distance <= magnetRange)
        {
            _isBeingMagnetized = true;
        }

        if (_isBeingMagnetized)
        {
            _currentSpeed += 16f * Time.deltaTime;
            transform.position = Vector3.MoveTowards(transform.position, _playerTransform.position, _currentSpeed * Time.deltaTime);

            if (distance <= collectDistance)
            {
                Collect();
            }
        }
        else
        {
            // Bập bùng nhẹ trên mặt sàn
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

    // =========================================================================
    // HÀM TẠO ĐỒNG XU TIỆN DỤNG TỪ CODE
    // =========================================================================
    public static MaterialGem SpawnCoin(Vector3 position, CoinTier tier)
    {
        GameObject coinObj = new GameObject($"Coin_{tier}");
        coinObj.transform.position = position;

        MaterialGem mg = coinObj.AddComponent<MaterialGem>();
        mg.ApplyCoinTier(tier);
        return mg;
    }

    private Sprite CreateGemSprite()
    {
        int size = 16;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point;
        Color col = (coinTier == CoinTier.Gold) ? new Color(1f, 0.85f, 0.1f) : ((coinTier == CoinTier.Red) ? new Color(0.95f, 0.15f, 0.2f) : new Color(0.85f, 0.85f, 0.9f));

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                int dx = Mathf.Abs(x - size / 2);
                int dy = Mathf.Abs(y - size / 2);
                texture.SetPixel(x, y, (dx + dy <= size / 2) ? col : Color.clear);
            }
        }
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 16f);
    }
}
