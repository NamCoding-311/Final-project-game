using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Quản lý Đấu trường Sinh Tồn (Brotato Mode):
// 1. Tự động dựng 4 bức tường đấu trường khép kín (ngăn người chơi & quái lọt ra ngoài).
// 2. Quản lý hệ thống Đợt Sóng (Waves): Đếm ngược thời gian, tăng dần độ khó.
// 3. Sinh quái từ 4 mép ngoài dồn vào đấu trường.
// 4. Giao diện HUD tích hợp sẵn (Hiển thị Wave, Đồng hồ đếm ngược, Nút sang Wave tiếp theo).
public class ArenaManager : MonoBehaviour
{
    public static ArenaManager Instance { get; private set; }

    [Header("Arena Dimensions (Kích thước sàn đấu)")]
    [SerializeField] private float arenaWidth = 26f;
    [SerializeField] private float arenaHeight = 15f;
    [SerializeField] private bool showArenaGizmos = true;

    [Header("Enemy Prefabs")]
    [SerializeField] private GameObject zombiePrefab;
    [SerializeField] private GameObject jumperPrefab;
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
    }

    private void Start()
    {
        FindPlayer();
        StartWave(currentWave);
    }

    private void Update()
    {
        if (!_isWaveActive) return;

        if (_playerTransform == null)
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
    }

    private void FindPlayer()
    {
        OnFootPlayerController player = FindAnyObjectByType<OnFootPlayerController>();
        if (player != null)
        {
            _playerTransform = player.transform;
            _playerHealth = player.GetComponent<PlayerHealth>();
        }
    }

    // 1. TỰ ĐỘNG TẠO 4 BỨC TƯỜNG ĐẤU TRƯỜNG KHÉP KÍN
    private void CreateArenaWalls()
    {
        GameObject wallsParent = new GameObject("Arena_Walls");
        wallsParent.transform.SetParent(transform);

        float halfW = arenaWidth * 0.5f;
        float halfH = arenaHeight * 0.5f;
        float wallThickness = 2.0f;

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

        BoxCollider2D box = wall.AddComponent<BoxCollider2D>();
        box.size = size;
    }

    // 2. BẮT ĐẦU ĐỢT SÓNG
    public void StartWave(int waveNumber)
    {
        currentWave = waveNumber;
        _isWaveCompleted = false;

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

        // Tỉ lệ xuất hiện Jumper tăng theo Wave
        int jumperChance = Mathf.Clamp((currentWave - 1) * 15, 0, 45); // Wave 1: 0%, Wave 2: 15%, Wave 3: 30%, Wave 4: 45%

        GameObject prefabToSpawn = zombiePrefab;
        if (jumperPrefab != null && Random.Range(0, 100) < jumperChance)
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

