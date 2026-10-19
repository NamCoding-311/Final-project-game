using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Quản lý Đấu trường Sinh Tồn (Brotato Mode):
// 1. Tự động dựng 4 bức tường đấu trường khép kín (ngăn người chơi & quái lọt ra ngoài).
// 2. Quản lý hệ thống Đợt Sóng (Waves): Đếm ngược thời gian, tăng dần độ khó.
// 3. Sinh quái từ 4 mép ngoài dồn vào đấu trường.
// 4. Hỗ trợ hiển thị UI bằng Canvas TextMeshPro tùy chỉnh hoặc fallback OnGUI.
public class ArenaManager : MonoBehaviour
{
    public static ArenaManager Instance { get; private set; }

    [Header("Custom UI Canvas (Tùy chọn - Kéo thả UI của bạn vào đây)")]
    [Tooltip("Text hiển thị Wave (ví dụ: TextMeshPro 'WAVE 1')")]
    [SerializeField] private TextMeshProUGUI customWaveText;

    [Tooltip("Text hiển thị đếm ngược thời gian (ví dụ: TextMeshPro '20s')")]
    [SerializeField] private TextMeshProUGUI customTimerText;

    [Tooltip("Banner hoặc Text thông báo hoàn thành Wave")]
    [SerializeField] private GameObject customWaveCompletedBanner;
    [SerializeField] private TextMeshProUGUI customWaveCompletedText;

    [Header("Arena Dimensions (Kích thước sàn đấu)")]
    [SerializeField] private float arenaWidth = 31f;
    [SerializeField] private float arenaHeight = 31f;
    [SerializeField] private bool showWallVisuals = false; // Mặc định tắt khối xanh che sàn, để lộ toàn bộ sàn Tilemap đá
    [SerializeField] private bool showArenaGizmos = true;

    [Header("Enemy Prefabs")]
    [SerializeField] private GameObject zombiePrefab;
    [SerializeField] private GameObject jumperPrefab;
    [SerializeField] private GameObject chargerPrefab;
    [SerializeField] private GameObject bossPrefab;

    [Header("Wave Progression & Difficulty Scaling")]
    [SerializeField] private int currentWave = 1;
    [SerializeField] private float waveDuration = 20f; // Thời gian mỗi đợt sóng (giây)
    [SerializeField] private float baseSpawnInterval = 1.0f; // Nhịp sinh quái
    [Tooltip("Tỉ lệ tăng máu quái mỗi Wave (0.35 = +35%/wave)")]
    [SerializeField] private float hpScalePerWave = 0.35f;
    [Tooltip("Tỉ lệ tăng sát thương quái mỗi Wave (0.12 = +12%/wave)")]
    [SerializeField] private float damageScalePerWave = 0.12f;
    [Tooltip("Tỉ lệ tăng tốc độ chạy mỗi Wave (0.035 = +3.5%/wave)")]
    [SerializeField] private float speedScalePerWave = 0.035f;
    [Tooltip("Tỉ lệ cơ bản xuất hiện Quái Tinh Anh (Elite)")]
    [SerializeField] private float baseEliteChance = 0.12f;

    [Header("End Game & Victory Settings")]
    [Tooltip("Số Wave cần hoàn thành để giành Chiến Thắng (mặc định 10)")]
    [SerializeField] private int maxWaves = 10;

    private int _totalKills = 0;
    private bool _isEndlessMode = false;

    public void AddKill() => _totalKills++;
    public int GetTotalKills() => _totalKills;
    public int GetCurrentWave() => currentWave;
    public int GetMaxWaves() => maxWaves;
    public bool IsEndlessMode() => _isEndlessMode;
    public void SetEndlessMode(bool enabled) => _isEndlessMode = enabled;

    private float _timeRemaining;
    private bool _isWaveActive = false;
    private bool _isWaveCompleted = false;
    private Transform _playerTransform;
    private PlayerHealth _playerHealth;
    private readonly List<Collider2D> _wallColliders = new List<Collider2D>();

    private void Awake()
    {
        Instance = this;

        // Mở rộng toàn bộ sàn đấu đá 31x31 bao phủ trọn vẹn 32x32 ô gạch Tilemap
        if (arenaHeight < 28f || arenaWidth < 28f)
        {
            arenaWidth = 31f;
            arenaHeight = 31f;
        }

        CreateArenaWalls();

        // Tự động đảm bảo hệ thống Level & Ngọc luôn có mặt
        if (GetComponent<BrotatoLevelSystem>() == null && FindAnyObjectByType<BrotatoLevelSystem>() == null)
        {
            gameObject.AddComponent<BrotatoLevelSystem>();
        }

        // Tự động đảm bảo hệ thống Cửa Hàng (Shop) luôn có mặt
        if (GetComponent<ArenaShopSystem>() == null && FindAnyObjectByType<ArenaShopSystem>() == null)
        {
            gameObject.AddComponent<ArenaShopSystem>();
        }

        // Tự động đảm bảo hệ thống Kết Thúc Game (GameOver / Victory) luôn có mặt
        if (GetComponent<ArenaGameOverSystem>() == null && FindAnyObjectByType<ArenaGameOverSystem>() == null)
        {
            gameObject.AddComponent<ArenaGameOverSystem>();
        }
    }

    private Sprite _pixelSprite;

    private Sprite GetOrCreatePixelSprite()
    {
        if (_pixelSprite == null)
        {
            Texture2D tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            tex.SetPixel(0, 0, Color.white);
            tex.Apply();
            _pixelSprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
        }
        return _pixelSprite;
    }

    private void Start()
    {
        Camera mainCam = Camera.main;
        if (mainCam != null && mainCam.orthographic)
        {
            mainCam.orthographicSize = 9.0f;
            mainCam.transform.position = new Vector3(0f, 0f, -10f);
        }

        FindPlayer();
        StartWave(currentWave);
    }

    private void Update()
    {
        if (!_isWaveActive) return;

        if (_playerTransform == null || !_playerTransform.gameObject.activeInHierarchy)
        {
            FindPlayer();
        }

        // Đếm ngược thời gian đợt sóng
        _timeRemaining -= Time.deltaTime;
        if (_timeRemaining <= 0f)
        {
            _timeRemaining = 0f;
            EndWave();
        }

        // Cập nhật Canvas UI tùy chỉnh nếu có
        if (customWaveText != null) customWaveText.text = $"WAVE {currentWave}";
        if (customTimerText != null) customTimerText.text = $"{Mathf.CeilToInt(_timeRemaining)}s";
    }

    private void LateUpdate()
    {
        UpdateCameraFollow();
    }

    // Camera 2D mượt mà bám theo Player và tự động kẹp trong ranh giới map 32x32
    private void UpdateCameraFollow()
    {
        if (_playerTransform == null) return;
        Camera cam = Camera.main;
        if (cam == null || !cam.orthographic) return;

        float halfH = cam.orthographicSize;
        float halfW = halfH * cam.aspect;

        float maxCamX = Mathf.Max(0f, (arenaWidth * 0.5f) - halfW);
        float maxCamY = Mathf.Max(0f, (arenaHeight * 0.5f) - halfH);

        float targetX = Mathf.Clamp(_playerTransform.position.x, -maxCamX, maxCamX);
        float targetY = Mathf.Clamp(_playerTransform.position.y, -maxCamY, maxCamY);

        Vector3 targetPos = new Vector3(targetX, targetY, -10f);
        cam.transform.position = Vector3.Lerp(cam.transform.position, targetPos, 10f * Time.deltaTime);
    }

    private void FindPlayer()
    {
        OnFootPlayerController player = FindAnyObjectByType<OnFootPlayerController>(FindObjectsInactive.Include);
        if (player != null)
        {
            if (!player.gameObject.activeSelf)
            {
                player.gameObject.SetActive(true);
            }
            _playerTransform = player.transform;
            _playerHealth = player.GetComponent<PlayerHealth>();

            // Mở khóa toàn bộ ranh giới di chuyển khắp sàn đấu đá 31x31
            float halfW = arenaWidth * 0.5f - 0.5f;
            float halfH = arenaHeight * 0.5f - 0.5f;
            player.SetArenaBounds(-halfW, halfW, -halfH, halfH);
        }
    }

    // 1. TỰ ĐỘNG TẠO SÀN VÀ 4 BỨC TƯỜNG ĐẤU TRƯỜNG KHÉP KÍN
    private void CreateArenaWalls()
    {
        // Sàn đấu (Floor) - chỉ tạo nếu chưa có Tilemap Grid hoặc sàn đấu trong Scene
        if (transform.Find("Arena_Floor") == null && FindAnyObjectByType<Grid>() == null)
        {
            GameObject floor = new GameObject("Arena_Floor");
            floor.transform.SetParent(transform);
            floor.transform.position = Vector3.zero;
            SpriteRenderer floorSr = floor.AddComponent<SpriteRenderer>();
            floorSr.sprite = GetOrCreatePixelSprite();
            floorSr.color = new Color(0.12f, 0.15f, 0.20f, 1f); // Nền xám xanh đậm hiện đại
            floorSr.sortingOrder = -20;
            floor.transform.localScale = new Vector3(arenaWidth, arenaHeight, 1f);
        }

        if (transform.Find("Arena_Walls") == null)
        {
            GameObject wallsParent = new GameObject("Arena_Walls");
            wallsParent.transform.SetParent(transform);

        float halfW = arenaWidth * 0.5f;
        float halfH = arenaHeight * 0.5f;
        float wallThickness = 1.0f;

        // Tường Trên (Top)
        CreateWall(wallsParent.transform, "Wall_Top", new Vector2(0f, halfH + wallThickness * 0.5f), new Vector2(arenaWidth + wallThickness * 2f, wallThickness));
        // Tường Dưới (Bottom)
        CreateWall(wallsParent.transform, "Wall_Bottom", new Vector2(0f, -halfH - wallThickness * 0.5f), new Vector2(arenaWidth + wallThickness * 2f, wallThickness));
        // Tường Trái (Left)
        CreateWall(wallsParent.transform, "Wall_Left", new Vector2(-halfW - wallThickness * 0.5f, 0f), new Vector2(wallThickness, arenaHeight));
        // Tường Phải (Right)
        CreateWall(wallsParent.transform, "Wall_Right", new Vector2(halfW + wallThickness * 0.5f, 0f), new Vector2(wallThickness, arenaHeight));
        }
    }

    private void CreateWall(Transform parent, string name, Vector2 pos, Vector2 size)
    {
        GameObject wall = new GameObject(name);
        wall.transform.SetParent(parent);
        wall.transform.position = pos;

        // Collider vật lý ngăn người chơi lọt ra ngoài màn hình
        BoxCollider2D box = wall.AddComponent<BoxCollider2D>();
        box.size = size;
        _wallColliders.Add(box);

        // Mặc định không vẽ Sprite khối màu xanh đè lên sàn đá nữa
        if (showWallVisuals)
        {
            SpriteRenderer wallSr = wall.AddComponent<SpriteRenderer>();
            wallSr.sprite = GetOrCreatePixelSprite();
            wallSr.color = new Color(0.28f, 0.36f, 0.48f, 0.35f); // Viền mờ tinh tế nếu bật
            wallSr.sortingOrder = -10;
            wall.transform.localScale = new Vector3(size.x, size.y, 1f);
        }
    }

    public float GetWaveHPMultiplier()
    {
        int w = Mathf.Max(1, currentWave);
        // Tăng theo đường cong chuẩn Brotato (tuyến tính + lũy thừa nhẹ)
        return 1f + (w - 1) * hpScalePerWave + Mathf.Pow(w - 1, 1.25f) * 0.05f;
    }

    public float GetWaveDamageMultiplier()
    {
        int w = Mathf.Max(1, currentWave);
        return 1f + (w - 1) * damageScalePerWave;
    }

    public float GetWaveSpeedMultiplier()
    {
        int w = Mathf.Max(1, currentWave);
        return Mathf.Clamp(1f + (w - 1) * speedScalePerWave, 1f, 1.45f);
    }

    private void SetupSpawnedEnemy(GameObject enemyObj)
    {
        if (enemyObj == null) return;

        float hpMult = GetWaveHPMultiplier();
        float dmgMult = GetWaveDamageMultiplier();
        float spdMult = GetWaveSpeedMultiplier();

        bool canBeElite = currentWave >= 2;
        float eliteChance = Mathf.Clamp(baseEliteChance + (currentWave - 2) * 0.03f, 0f, 0.35f);
        bool isElite = canBeElite && (Random.value < eliteChance);

        // 1. Basic Zombie
        Zombie z = enemyObj.GetComponent<Zombie>();
        if (z != null)
        {
            z.SetClampToRoad(false);
            z.ScaleStats(hpMult, dmgMult, spdMult);
            if (isElite) z.MakeElite(2.5f, 1.3f);
        }

        // 2. Zombie Charger
        ZombieCharger charger = enemyObj.GetComponent<ZombieCharger>();
        if (charger != null)
        {
            charger.ScaleStats(hpMult, dmgMult, spdMult);
            if (isElite) charger.MakeElite(2.2f, 1.25f);
        }

        // 3. Zombie Jumper
        ZombieJumper jumper = enemyObj.GetComponent<ZombieJumper>();
        if (jumper != null)
        {
            jumper.ScaleStats(hpMult, dmgMult, spdMult);
        }

        // 4. Zombie Boss
        ZombieBoss boss = enemyObj.GetComponent<ZombieBoss>();
        if (boss != null)
        {
            boss.SetClampToArena(false);
            // Boss tăng máu mạnh mẽ hơn theo wave (Wave 5: 1x, Wave 10: 2.2x, Wave 15: 3.5x)
            float bossWaveMult = 1f + Mathf.Max(0, currentWave - 5) * 0.35f;
            boss.ScaleStats(bossWaveMult, dmgMult, Mathf.Min(1.25f, spdMult));
        }

        // Bỏ qua va chạm giữa quái và tường biên giới trên TẤT CẢ Collider của quái
        Collider2D[] enemyCols = enemyObj.GetComponentsInChildren<Collider2D>();
        foreach (var enemyCol in enemyCols)
        {
            if (enemyCol != null)
            {
                foreach (var wallCol in _wallColliders)
                {
                    if (wallCol != null)
                    {
                        Physics2D.IgnoreCollision(enemyCol, wallCol, true);
                    }
                }
            }
        }
    }

    // 2. BẮT ĐẦU ĐỢT SÓNG
    public void StartWave(int waveNumber)
    {
        currentWave = waveNumber;
        _isWaveCompleted = false;

        // Ẩn banner hoàn thành wave nếu có
        if (customWaveCompletedBanner != null) customWaveCompletedBanner.SetActive(false);

        // Mỗi wave tăng thêm 4 giây sinh tồn
        waveDuration = 16f + (waveNumber * 4f);
        _timeRemaining = waveDuration;

        _isWaveActive = true;

        float hpMult = GetWaveHPMultiplier();
        float dmgMult = GetWaveDamageMultiplier();
        Debug.Log($"<color=#FF5555>[ArenaManager] Wave {currentWave} Started! Difficulty: Enemy HP x{hpMult:F2} | Damage x{dmgMult:F2} | Speed x{GetWaveSpeedMultiplier():F2}</color>");

        // Tính toán nhịp spawn (wave càng cao sinh quái càng dồn dập)
        float spawnRate = Mathf.Max(0.28f, baseSpawnInterval - (waveNumber * 0.08f));

        StopAllCoroutines();
        StartCoroutine(EnemySpawnRoutine(spawnRate));
    }

    // 3. SINH QUÁI TỪ 4 MÉP MÀN HÌNH CHẠY VÀO
    private IEnumerator EnemySpawnRoutine(float interval)
    {
        yield return new WaitForSeconds(0.8f);

        // Cứ mỗi 5 wave (Wave 5, Wave 10, Wave 15,...) thì thả Boss!
        if (currentWave % 5 == 0 && bossPrefab != null)
        {
            int bossCount = currentWave >= 10 ? 2 : 1; // Wave 10 thả hẳn 2 Boss cho kịch tính!
            for (int b = 0; b < bossCount; b++)
            {
                Vector3 bossPos = GetSpawnPositionAroundEdge();
                GameObject bossObj = Instantiate(bossPrefab, bossPos, Quaternion.identity);
                SetupSpawnedEnemy(bossObj);
            }
        }

        while (_isWaveActive)
        {
            // Từ Wave 4+: Có tỉ lệ sinh 2 quái cùng lúc để tạo cảm giác bầy đàn (swarm)
            int spawnBatch = (currentWave >= 4 && Random.value < 0.45f) ? 2 : 1;
            for (int s = 0; s < spawnBatch; s++)
            {
                SpawnEnemy();
            }

            yield return new WaitForSeconds(interval);
        }
    }

    private void SpawnEnemy()
    {
        Vector3 spawnPos = GetSpawnPositionAroundEdge();

        // Tỉ lệ xuất hiện Jumper và Charger tăng dần theo Wave
        int jumperChance = Mathf.Clamp((currentWave - 1) * 12, 0, 35);
        int chargerChance = (currentWave >= 2) ? Mathf.Clamp((currentWave - 1) * 10, 0, 30) : 0;

        int roll = Random.Range(0, 100);
        GameObject prefabToSpawn = zombiePrefab;

        if (chargerPrefab != null && roll < chargerChance)
        {
            prefabToSpawn = chargerPrefab;
        }
        else if (jumperPrefab != null && roll < (chargerChance + jumperChance))
        {
            prefabToSpawn = jumperPrefab;
        }

        if (prefabToSpawn != null)
        {
            GameObject spawnedObj = Instantiate(prefabToSpawn, spawnPos, Quaternion.identity);
            SetupSpawnedEnemy(spawnedObj);
        }
    }

    // Lấy ngẫu nhiên tọa độ nằm sát mép trong của 4 cạnh sàn đấu đá
    private Vector3 GetSpawnPositionAroundEdge()
    {
        // Sinh quái nằm trọn vẹn trên sàn đá cách mép tường 1.5m, không bao giờ bị rớt ra ngoài khoảng đen
        float spawnW = Mathf.Max(4f, (arenaWidth * 0.5f) - 1.5f);
        float spawnH = Mathf.Max(4f, (arenaHeight * 0.5f) - 1.5f);

        int side = Random.Range(0, 4); // 0: Top, 1: Bottom, 2: Left, 3: Right
        float x = 0f, y = 0f;

        switch (side)
        {
            case 0: // Cạnh Trên
                x = Random.Range(-spawnW, spawnW);
                y = spawnH;
                break;
            case 1: // Cạnh Dưới
                x = Random.Range(-spawnW, spawnW);
                y = -spawnH;
                break;
            case 2: // Cạnh Trái
                x = -spawnW;
                y = Random.Range(-spawnH, spawnH);
                break;
            case 3: // Cạnh Phải
                x = spawnW;
                y = Random.Range(-spawnH, spawnH);
                break;
        }

        return new Vector3(x, y, 0f);
    }

    // 4. KẾT THÚC ĐỢT SÓNG
    private void EndWave()
    {
        _isWaveActive = false;
        _isWaveCompleted = true;
        StopAllCoroutines();

        // Dọn sạch toàn bộ quái còn lại trên sàn đấu
        Zombie[] zombies = FindObjectsByType<Zombie>(FindObjectsSortMode.None);
        foreach (var z in zombies) if (z != null) Destroy(z.gameObject);

        ZombieJumper[] jumpers = FindObjectsByType<ZombieJumper>(FindObjectsSortMode.None);
        foreach (var j in jumpers) if (j != null) Destroy(j.gameObject);

        ZombieCharger[] chargers = FindObjectsByType<ZombieCharger>(FindObjectsSortMode.None);
        foreach (var c in chargers) if (c != null) Destroy(c.gameObject);

        ZombieBoss[] bosses = FindObjectsByType<ZombieBoss>(FindObjectsSortMode.None);
        foreach (var b in bosses) if (b != null) Destroy(b.gameObject);

        // Kiểm tra nếu đã sống sót qua toàn bộ đợt sóng (Mặc định 10 Waves) và không bật Vô Tận -> CHIẾN THẮNG!
        if (currentWave >= maxWaves && !_isEndlessMode)
        {
            ArenaGameOverSystem gameOver = ArenaGameOverSystem.Instance ?? FindAnyObjectByType<ArenaGameOverSystem>();
            if (gameOver != null)
            {
                gameOver.TriggerVictory();
                return;
            }
        }

        // Mở Cửa Hàng Giữa Các Hiệp (Inter-Wave Shop)
        ArenaShopSystem shop = ArenaShopSystem.Instance ?? FindAnyObjectByType<ArenaShopSystem>();
        if (shop != null)
        {
            shop.OpenShop(currentWave);
        }
        else
        {
            // Fallback nếu chưa có Shop thì hiển thị banner thông báo như cũ
            if (customWaveCompletedBanner != null) customWaveCompletedBanner.SetActive(true);
            if (customWaveCompletedText != null) customWaveCompletedText.text = $"WAVE {currentWave} COMPLETED!\nReady for next wave...";
            StartCoroutine(AutoNextWaveRoutine());
        }
    }

    private IEnumerator AutoNextWaveRoutine()
    {
        yield return new WaitForSeconds(2.0f);
        StartWave(currentWave + 1);
    }

    // 5. GIAO DIỆN HUD TỰ ĐỘNG (Trực quan, chuẩn phong cách Brotato)
    private void OnGUI()
    {
        // Tự động tắt OnGUI nếu người chơi đã tự kéo UI Canvas vào
        if (customWaveText != null || customTimerText != null) return;
        GUIStyle titleStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.UpperCenter,
            fontSize = 24,
            fontStyle = FontStyle.Bold
        };
        titleStyle.normal.textColor = Color.white;

        GUIStyle timerStyle = new GUIStyle(titleStyle)
        {
            fontSize = 32
        };
        timerStyle.normal.textColor = _timeRemaining <= 5f ? Color.red : Color.yellow;

        // Đồng hồ đếm ngược trên đỉnh màn hình
        if (_isWaveActive)
        {
            GUI.Label(new Rect(Screen.width * 0.5f - 150, 15, 300, 35), $"WAVE {currentWave}", titleStyle);
            GUI.Label(new Rect(Screen.width * 0.5f - 150, 50, 300, 45), $"{Mathf.CeilToInt(_timeRemaining)}s", timerStyle);
        }

        // Thông báo hoàn thành Wave (tự động chuyển sau 2 giây)
        if (_isWaveCompleted)
        {
            GUIStyle winStyle = new GUIStyle(titleStyle)
            {
                fontSize = 34
            };
            winStyle.normal.textColor = Color.green;

            GUI.Label(new Rect(Screen.width * 0.5f - 200, Screen.height * 0.35f, 400, 50), $"WAVE {currentWave} COMPLETED!", winStyle);

            GUIStyle nextStyle = new GUIStyle(titleStyle)
            {
                fontSize = 20
            };
            nextStyle.normal.textColor = Color.yellow;
            GUI.Label(new Rect(Screen.width * 0.5f - 200, Screen.height * 0.45f, 400, 40), $"Ready for next Wave {currentWave + 1}...", nextStyle);
        }
    }

    private void OnDrawGizmos()
    {
        if (!showArenaGizmos) return;

        // Vẽ khung viền đấu trường màu xanh neon trong Scene View
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(Vector3.zero, new Vector3(arenaWidth, arenaHeight, 0f));
    }
}

// Alias để tương thích ngược nếu component đã gán tên cũ
public class BrotatoArenaManager : ArenaManager { }

