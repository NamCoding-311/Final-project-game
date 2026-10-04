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
    [SerializeField] private float arenaWidth = 26f;
    [SerializeField] private float arenaHeight = 15f;
    [SerializeField] private bool showArenaGizmos = true;

    [Header("Enemy Prefabs")]
    [SerializeField] private GameObject zombiePrefab;
    [SerializeField] private GameObject jumperPrefab;
    [SerializeField] private GameObject chargerPrefab;
    [SerializeField] private GameObject bossPrefab;

    [Header("Wave Progression")]
    [SerializeField] private int currentWave = 1;
    [SerializeField] private float waveDuration = 20f; // Thời gian mỗi đợt sóng (giây)
    [SerializeField] private float baseSpawnInterval = 1.0f; // Nhịp sinh quái

    private float _timeRemaining;
    private bool _isWaveActive = false;
    private bool _isWaveCompleted = false;
    private Transform _playerTransform;
    private PlayerHealth _playerHealth;

    private void Awake()
    {
        Instance = this;
        CreateArenaWalls();

        // Tự động đảm bảo hệ thống Level & Ngọc luôn có mặt
        if (GetComponent<BrotatoLevelSystem>() == null && FindAnyObjectByType<BrotatoLevelSystem>() == null)
        {
            gameObject.AddComponent<BrotatoLevelSystem>();
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
        // Điều chỉnh Camera bao trọn toàn bộ sàn đấu 26x15
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
        }
    }

    // 1. TỰ ĐỘNG TẠO SÀN VÀ 4 BỨC TƯỜNG ĐẤU TRƯỜNG KHÉP KÍN
    private void CreateArenaWalls()
    {
        // Sàn đấu (Floor)
        GameObject floor = new GameObject("Arena_Floor");
        floor.transform.SetParent(transform);
        floor.transform.position = Vector3.zero;
        SpriteRenderer floorSr = floor.AddComponent<SpriteRenderer>();
        floorSr.sprite = GetOrCreatePixelSprite();
        floorSr.color = new Color(0.12f, 0.15f, 0.20f, 1f); // Nền xám xanh đậm hiện đại
        floorSr.sortingOrder = -20;
        floor.transform.localScale = new Vector3(arenaWidth, arenaHeight, 1f);

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

    private void CreateWall(Transform parent, string name, Vector2 pos, Vector2 size)
    {
        GameObject wall = new GameObject(name);
        wall.transform.SetParent(parent);
        wall.transform.position = pos;

        // Collider vật lý ngăn người chơi & quái lọt ra ngoài
        BoxCollider2D box = wall.AddComponent<BoxCollider2D>();
        box.size = size;

        // Hình ảnh tường viền đấu trường
        SpriteRenderer wallSr = wall.AddComponent<SpriteRenderer>();
        wallSr.sprite = GetOrCreatePixelSprite();
        wallSr.color = new Color(0.28f, 0.36f, 0.48f, 1f); // Viền tường sáng màu
        wallSr.sortingOrder = -10;
        wall.transform.localScale = new Vector3(size.x, size.y, 1f);
    }

    // 2. BẮT ĐẦU ĐỢT SÓNG
    public void StartWave(int waveNumber)
    {
        currentWave = waveNumber;
        _isWaveCompleted = false;

        // Ẩn banner hoàn thành wave nếu có
        if (customWaveCompletedBanner != null) customWaveCompletedBanner.SetActive(false);

        // Mỗi wave tăng thêm 5 giây sinh tồn
        waveDuration = 15f + (waveNumber * 5f);
        _timeRemaining = waveDuration;

        _isWaveActive = true;

        // Tính toán nhịp spawn (wave càng cao sinh quái càng dồn dập)
        float spawnRate = Mathf.Max(0.35f, baseSpawnInterval - (waveNumber * 0.08f));

        StopAllCoroutines();
        StartCoroutine(EnemySpawnRoutine(spawnRate));
    }

    // 3. SINH QUÁI TỪ 4 MÉP MÀN HÌNH CHẠY VÀO
    private IEnumerator EnemySpawnRoutine(float interval)
    {
        yield return new WaitForSeconds(0.8f);

        // Nếu là Wave 5: Thả Boss ngay từ đầu sóng!
        if (currentWave >= 5 && bossPrefab != null)
        {
            Vector3 bossPos = GetSpawnPositionAroundEdge();
            Instantiate(bossPrefab, bossPos, Quaternion.identity);
        }

        while (_isWaveActive)
        {
            SpawnEnemy();
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
            Instantiate(prefabToSpawn, spawnPos, Quaternion.identity);
        }
    }

    // Lấy ngẫu nhiên tọa độ nằm sát mép ngoài của 4 cạnh đấu trường
    private Vector3 GetSpawnPositionAroundEdge()
    {
        float halfW = arenaWidth * 0.5f;
        float halfH = arenaHeight * 0.5f;
        float spawnOffset = 1.0f; // Cách mép tường 1m để chạy vào

        int side = Random.Range(0, 4); // 0: Top, 1: Bottom, 2: Left, 3: Right
        float x = 0f, y = 0f;

        switch (side)
        {
            case 0: // Cạnh Trên
                x = Random.Range(-halfW, halfW);
                y = halfH + spawnOffset;
                break;
            case 1: // Cạnh Dưới
                x = Random.Range(-halfW, halfW);
                y = -halfH - spawnOffset;
                break;
            case 2: // Cạnh Trái
                x = -halfW - spawnOffset;
                y = Random.Range(-halfH, halfH);
                break;
            case 3: // Cạnh Phải
                x = halfW + spawnOffset;
                y = Random.Range(-halfH, halfH);
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

        // Hiện banner hoàn thành wave nếu có
        if (customWaveCompletedBanner != null) customWaveCompletedBanner.SetActive(true);
        if (customWaveCompletedText != null) customWaveCompletedText.text = $"WAVE {currentWave} COMPLETED!\nReady for next wave...";

        // Tự động chuyển sang Wave tiếp theo sau 2 giây
        StartCoroutine(AutoNextWaveRoutine());
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

