using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Quản lý Đấu trường Sinh Tồn (Brotato Mode):
// 1. Tự động dựng 4 bức tường đấu trường khép kín (ngăn người chơi & quái lọt ra ngoài).
// 2. Quản lý hệ thống Đợt Sóng (Waves): Đếm ngược thời gian, tăng dần độ khó.
// 3. Sinh cây xanh trên sàn mỗi Wave (chặt cây nhặt quả hồi máu & hòm tiếp tế).
// 4. Có dấu cảnh báo đỏ nguy hiểm trước khi quái và Boss xuất hiện.
// 5. Kết thúc Wave: Tự động gom/hút toàn bộ ngọc trên khắp sàn đấu về người chơi rồi mới mở Shop.
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
    [Tooltip("Độ thu phóng góc nhìn Camera (Số càng nhỏ càng lại gần nhân vật, khuyên dùng 5.0 - 6.0)")]
    [SerializeField] private float cameraZoom = 5.5f;
    [SerializeField] private bool showWallVisuals = false;
    [SerializeField] private bool showArenaGizmos = true;

    [Header("Enemy Prefabs")]
    [SerializeField] private GameObject zombiePrefab;
    [SerializeField] private GameObject jumperPrefab;
    [SerializeField] private GameObject chargerPrefab;
    [SerializeField] private GameObject bossPrefab;

    [Header("Wave Progression & Difficulty Scaling")]
    [SerializeField] private int currentWave = 1;
    [SerializeField] private float waveDuration = 20f;
    [SerializeField] private float baseSpawnInterval = 1.0f;
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

        if (arenaHeight < 28f || arenaWidth < 28f)
        {
            arenaWidth = 31f;
            arenaHeight = 31f;
        }

        CreateArenaWalls();

        if (GetComponent<BrotatoLevelSystem>() == null && FindAnyObjectByType<BrotatoLevelSystem>() == null)
        {
            gameObject.AddComponent<BrotatoLevelSystem>();
        }

        if (GetComponent<ArenaShopSystem>() == null && FindAnyObjectByType<ArenaShopSystem>() == null)
        {
            gameObject.AddComponent<ArenaShopSystem>();
        }

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
            mainCam.orthographicSize = cameraZoom;
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

        _timeRemaining -= Time.deltaTime;
        if (_timeRemaining <= 0f)
        {
            _timeRemaining = 0f;
            EndWave();
        }

        if (customWaveText != null) customWaveText.text = $"WAVE {currentWave}";
        if (customTimerText != null) customTimerText.text = $"{Mathf.CeilToInt(_timeRemaining)}s";
    }

    private void LateUpdate()
    {
        UpdateCameraFollow();
    }

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

            float halfW = arenaWidth * 0.5f - 0.5f;
            float halfH = arenaHeight * 0.5f - 0.5f;
            player.SetArenaBounds(-halfW, halfW, -halfH, halfH);
        }
    }

    private void CreateArenaWalls()
    {
        if (transform.Find("Arena_Floor") == null && FindAnyObjectByType<Grid>() == null)
        {
            GameObject floor = new GameObject("Arena_Floor");
            floor.transform.SetParent(transform);
            floor.transform.position = Vector3.zero;
            SpriteRenderer floorSr = floor.AddComponent<SpriteRenderer>();
            floorSr.sprite = GetOrCreatePixelSprite();
            floorSr.color = new Color(0.12f, 0.15f, 0.20f, 1f);
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

            CreateWall(wallsParent.transform, "Wall_Top", new Vector2(0f, halfH + wallThickness * 0.5f), new Vector2(arenaWidth + wallThickness * 2f, wallThickness));
            CreateWall(wallsParent.transform, "Wall_Bottom", new Vector2(0f, -halfH - wallThickness * 0.5f), new Vector2(arenaWidth + wallThickness * 2f, wallThickness));
            CreateWall(wallsParent.transform, "Wall_Left", new Vector2(-halfW - wallThickness * 0.5f, 0f), new Vector2(wallThickness, arenaHeight));
            CreateWall(wallsParent.transform, "Wall_Right", new Vector2(halfW + wallThickness * 0.5f, 0f), new Vector2(wallThickness, arenaHeight));
        }
    }

    private void CreateWall(Transform parent, string name, Vector2 pos, Vector2 size)
    {
        GameObject wall = new GameObject(name);
        wall.transform.SetParent(parent);
        wall.transform.position = pos;

        BoxCollider2D box = wall.AddComponent<BoxCollider2D>();
        box.size = size;
        _wallColliders.Add(box);

        if (showWallVisuals)
        {
            SpriteRenderer wallSr = wall.AddComponent<SpriteRenderer>();
            wallSr.sprite = GetOrCreatePixelSprite();
            wallSr.color = new Color(0.28f, 0.36f, 0.48f, 0.35f);
            wallSr.sortingOrder = -10;
            wall.transform.localScale = new Vector3(size.x, size.y, 1f);
        }
    }

    public float GetWaveHPMultiplier()
    {
        int w = Mathf.Max(1, currentWave);
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

        Zombie z = enemyObj.GetComponent<Zombie>();
        if (z != null)
        {
            z.SetClampToRoad(false);
            z.ScaleStats(hpMult, dmgMult, spdMult);
            if (isElite) z.MakeElite(2.5f, 1.3f);
        }

        ZombieCharger charger = enemyObj.GetComponent<ZombieCharger>();
        if (charger != null)
        {
            charger.SetClampToRoad(false);
            charger.ScaleStats(hpMult, dmgMult, spdMult);
            if (isElite) charger.MakeElite(2.2f, 1.25f);
        }

        ZombieJumper jumper = enemyObj.GetComponent<ZombieJumper>();
        if (jumper != null)
        {
            jumper.SetClampToRoad(false);
            jumper.ScaleStats(hpMult, dmgMult, spdMult);
        }

        ZombieBoss boss = enemyObj.GetComponent<ZombieBoss>();
        if (boss != null)
        {
            boss.SetClampToArena(false);
            float bossWaveMult = 1f + Mathf.Max(0, currentWave - 5) * 0.35f;
            boss.ScaleStats(bossWaveMult, dmgMult, Mathf.Min(1.25f, spdMult));
        }

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

        if (customWaveCompletedBanner != null) customWaveCompletedBanner.SetActive(false);

        waveDuration = 16f + (waveNumber * 4f);
        _timeRemaining = waveDuration;

        _isWaveActive = true;

        float hpMult = GetWaveHPMultiplier();
        float dmgMult = GetWaveDamageMultiplier();
        Debug.Log($"<color=#FF5555>[ArenaManager] Wave {currentWave} Started! Difficulty: Enemy HP x{hpMult:F2} | Damage x{dmgMult:F2} | Speed x{GetWaveSpeedMultiplier():F2}</color>");

        float spawnRate = Mathf.Max(0.28f, baseSpawnInterval - (waveNumber * 0.08f));

        StopAllCoroutines();
        StartCoroutine(EnemySpawnRoutine(spawnRate));
    }

    // 3. SINH QUÁI TỪ 4 MÉP MÀN HÌNH CHẠY VÀO (CÓ DẤU CẢNH BÁO ĐỎ TRƯỚC KHI SPAWN)
    private IEnumerator EnemySpawnRoutine(float interval)
    {
        yield return new WaitForSeconds(0.8f);

        if (currentWave % 5 == 0 && bossPrefab != null)
        {
            int bossCount = currentWave >= 10 ? 2 : 1;
            for (int b = 0; b < bossCount; b++)
            {
                Vector3 bossPos = GetSpawnPositionAroundEdge();
                SpawnWarningIndicator.ShowWarning(bossPos, 1.0f, () => {
                    if (!_isWaveActive) return;
                    GameObject bossObj = Instantiate(bossPrefab, bossPos, Quaternion.identity);
                    SetupSpawnedEnemy(bossObj);
                });
            }
        }

        while (_isWaveActive)
        {
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
            SpawnWarningIndicator.ShowWarning(spawnPos, 0.65f, () => {
                if (!_isWaveActive) return;
                GameObject spawnedObj = Instantiate(prefabToSpawn, spawnPos, Quaternion.identity);
                SetupSpawnedEnemy(spawnedObj);
            });
        }
    }

    private Vector3 GetSpawnPositionAroundEdge()
    {
        float spawnW = Mathf.Max(4f, (arenaWidth * 0.5f) - 1.5f);
        float spawnH = Mathf.Max(4f, (arenaHeight * 0.5f) - 1.5f);

        int side = Random.Range(0, 4);
        float x = 0f, y = 0f;

        switch (side)
        {
            case 0:
                x = Random.Range(-spawnW, spawnW);
                y = spawnH;
                break;
            case 1:
                x = Random.Range(-spawnW, spawnW);
                y = -spawnH;
                break;
            case 2:
                x = -spawnW;
                y = Random.Range(-spawnH, spawnH);
                break;
            case 3:
                x = spawnW;
                y = Random.Range(-spawnH, spawnH);
                break;
        }

        return new Vector3(x, y, 0f);
    }

    // 4. KẾT THÚC ĐỢT SÓNG (HÚT SẠCH NGỌC CUỐI WAVE CHUẨN BROTATO)
    private void EndWave()
    {
        _isWaveActive = false;
        _isWaveCompleted = true;
        StopAllCoroutines();

        Zombie[] zombies = FindObjectsByType<Zombie>(FindObjectsSortMode.None);
        foreach (var z in zombies) if (z != null) Destroy(z.gameObject);

        ZombieJumper[] jumpers = FindObjectsByType<ZombieJumper>(FindObjectsSortMode.None);
        foreach (var j in jumpers) if (j != null) Destroy(j.gameObject);

        ZombieCharger[] chargers = FindObjectsByType<ZombieCharger>(FindObjectsSortMode.None);
        foreach (var c in chargers) if (c != null) Destroy(c.gameObject);

        ZombieBoss[] bosses = FindObjectsByType<ZombieBoss>(FindObjectsSortMode.None);
        foreach (var b in bosses) if (b != null) Destroy(b.gameObject);

        // 2. Tự động gom toàn bộ ngọc (Materials/Gems) còn vương vãi trên sàn bay vút về người chơi
        MaterialGem.MagnetizeAllGems();

        // 3. Chờ 1.2s hút hết ngọc rồi mới mở Shop
        StartCoroutine(EndWaveSequenceRoutine());
    }

    private IEnumerator EndWaveSequenceRoutine()
    {
        if (customWaveCompletedBanner != null)
        {
            customWaveCompletedBanner.SetActive(true);
            if (customWaveCompletedText != null)
                customWaveCompletedText.text = $"WAVE {currentWave} COMPLETED!\nVacuuming materials...";
        }

        yield return new WaitForSeconds(1.2f);

        if (customWaveCompletedBanner != null) customWaveCompletedBanner.SetActive(false);

        if (currentWave >= maxWaves && !_isEndlessMode)
        {
            ArenaGameOverSystem gameOver = ArenaGameOverSystem.Instance ?? FindAnyObjectByType<ArenaGameOverSystem>();
            if (gameOver != null)
            {
                gameOver.TriggerVictory();
                yield break;
            }
        }

        ArenaShopSystem shop = ArenaShopSystem.Instance ?? FindAnyObjectByType<ArenaShopSystem>();
        if (shop != null)
        {
            shop.OpenShop(currentWave);
        }
        else
        {
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

        if (_isWaveActive)
        {
            GUI.Label(new Rect(Screen.width * 0.5f - 150, 15, 300, 35), $"WAVE {currentWave}", titleStyle);
            GUI.Label(new Rect(Screen.width * 0.5f - 150, 50, 300, 45), $"{Mathf.CeilToInt(_timeRemaining)}s", timerStyle);
        }

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
            GUI.Label(new Rect(Screen.width * 0.5f - 200, Screen.height * 0.45f, 400, 40), $"Vacuuming Gems for Shop...", nextStyle);
        }
    }

    private void OnDrawGizmos()
    {
        if (!showArenaGizmos) return;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(Vector3.zero, new Vector3(arenaWidth, arenaHeight, 0f));
    }
}

public class BrotatoArenaManager : ArenaManager { }
