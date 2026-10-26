using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

// Dữ liệu cấu hình cho 1 Chặng trong Map 1
[Serializable]
public class LevelStageInfo
{
    [Tooltip("Tên định danh của chặng")]
    public string stageName = "Stage 1 : Daytime Riverbank";

    [Tooltip("Quãng đường xe chạy đến thì kết thúc chặng (xe đỗ lại để xuống xe)")]
    public float stageDistance = 180f;

    [Tooltip("Số lượng Zombie cần tiêu diệt khi xuống xe ở cuối chặng")]
    public int onFootZombieCount = 10;

    [Tooltip("Hệ số máu Zombie ở chặng này (Chặng sau trâu hơn chặng trước)")]
    public float zombieHpMultiplier = 1.0f;

    [Tooltip("Nhịp độ sinh Zombie lẻ lúc lái xe (giây/con, càng thấp càng đông)")]
    public float spawnInterval = 1.8f;

    [Tooltip("Số lượng Zombie tối thiểu và tối đa trong 1 đợt bầy quái chốt chặn")]
    public int minHorde = 4;
    public int maxHorde = 6;

    [Tooltip("Màu sắc và cường độ ánh sáng môi trường của chặng")]
    public Color lightColor = Color.white;
    public float lightIntensity = 1f;

    [Tooltip("Tỉ lệ xuất hiện Zombie Jumper khi lái xe (0-100%)")]
    [Range(0, 100)] public int jumperSpawnChance = 0;

    [Tooltip("Tỉ lệ xuất hiện Zombie Charger khi lái xe (0-100%)")]
    [Range(0, 100)] public int chargerSpawnChance = 0;

    [Tooltip("Tỉ lệ quái dị thường trong đợt sóng On-Foot cuối chặng (0-100%)")]
    [Range(0, 100)] public int onFootSpecialChance = 0;

    [Tooltip("Tùy chọn thông báo nhiệm vụ khi xuống xe ở cuối chặng")]
    public string onFootPromptText = "";
}

// Trạng thái của màn chơi
public enum LevelState
{
    NormalDriving,      // Đang lái xe đường trường
    ParkingVehicle,     // Xe đang giảm tốc tấp vào lề
    OnFootDefense,      // Người chơi xuống xe chiến đấu với bầy Zombie
    TransitionCutscene, // Cutscene tăng tốc chuyển giao sang chặng tiếp theo
    LevelWon            // Hoàn thành xuất sắc cả 3 chặng của Map 1
}

// Quản lý tiến trình 3 Chặng (Sáng -> Chiều -> Tối) của Map 1:
// - Lái xe đường dài, càng về cuối chặng Zombie càng đông.
// - Đến cuối chặng: Xe dừng lại tấp lề, người chơi xuống xe quét sạch bầy Zombie.
// - Qua mỗi chặng: Zombie được tăng chỉ số HP (trâu hơn).
// - Hoàn thành chặng 3: Hiện UI chiến thắng chuyển sang Map 2.
public class StageLevelManager : MonoBehaviour
{
    [Header("Player & Light References")]
    [SerializeField] protected Transform playerTransform;
    [SerializeField] protected Light2D globalLight2D;
    [SerializeField] protected ZombieSpawner zombieSpawner;

    [Header("On-Foot Player & Arena Setup")]
    // Nhân vật người đi bộ khi xuống xe
    [SerializeField] protected OnFootPlayerController onFootPlayer;

    [SerializeField] protected CameraFollow cameraFollow;
    [SerializeField] protected EndlessMapManager mapManager;
    [SerializeField] protected ObstacleSpawner obstacleSpawner;
    [SerializeField] protected InfiniteParallaxBackground parallaxBackground;

    [Header("Stages Setup (3 Chặng Sáng - Chiều - Tối)")]
    [SerializeField] protected LevelStageInfo[] stages = new LevelStageInfo[]
    {
        new LevelStageInfo
        {
            stageName = "Stage 1 : Morning",
            stageDistance = 180f,
            onFootZombieCount = 10,
            zombieHpMultiplier = 1.0f, // Máu cơ bản 50 HP
            spawnInterval = 1.8f,
            minHorde = 4,
            maxHorde = 6,
            lightColor = Color.white,
            lightIntensity = 1f
        },
        new LevelStageInfo
        {
            stageName = "Stage 2 : Dawn",
            stageDistance = 420f,
            onFootZombieCount = 16,
            zombieHpMultiplier = 1.6f, // Máu trâu hơn: ~80 HP
            spawnInterval = 1.2f,     // Spawn dày đặc hơn
            minHorde = 6,
            maxHorde = 9,
            lightColor = new Color(1f, 0.65f, 0.35f),
            lightIntensity = 0.9f
        },
        new LevelStageInfo
        {
            stageName = "Stage 3 : Night",
            stageDistance = 700f,
            onFootZombieCount = 22,
            zombieHpMultiplier = 2.4f, // Máu cực trâu: ~120 HP
            spawnInterval = 0.9f,     // Spawn dày đặc nhất
            minHorde = 7,
            maxHorde = 11,
            lightColor = new Color(0.25f, 0.35f, 0.55f),
            lightIntensity = 0.7f
        }
    };

    [Header("Player Health Bars (Thanh máu Xe & Người)")]
    [SerializeField] protected GameObject carHealthBar;
    [SerializeField] protected GameObject onFootHealthBar;

    [Header("UI Announcements")]
    [SerializeField] protected TextMeshProUGUI stageAnnouncementText;
    [SerializeField] protected LevelCompleteUI levelCompleteUI;
    [SerializeField] protected string mapVictoryText = "VICTORY!\nYOU SURVIVED MAP 1: RIVER CITY!";

    protected int _currentStageIndex = 0;
    protected LevelState _currentState = LevelState.NormalDriving;
    protected float _startPlayerX;
    protected Player3LaneMovement _playerMovement;

    // Quản lý đợt Zombie khi xuống xe
    protected int _remainingWaveZombies = 0;
    protected int _totalWaveZombies = 0;
    protected bool _isClimaxSpawnActive = false;

    protected virtual void OnEnable()
    {
        Zombie.OnZombieDied += HandleWaveZombieDied;
    }

    protected virtual void OnDisable()
    {
        Zombie.OnZombieDied -= HandleWaveZombieDied;
    }

    protected virtual void Awake()
    {
        // Tự động tắt nếu đang ở chế độ Đấu trường Sinh Tồn (Brotato)
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "ArenaSurvival" || FindAnyObjectByType<ArenaManager>() != null)
        {
            enabled = false;
            return;
        }
    }

    protected virtual void Start()
    {
        if (!enabled) return;

        if (playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) playerTransform = player.transform;
        }

        if (playerTransform != null)
        {
            _startPlayerX = playerTransform.position.x;
            _playerMovement = playerTransform.GetComponent<Player3LaneMovement>();
        }

        if (globalLight2D == null) globalLight2D = FindAnyObjectByType<Light2D>();
        if (zombieSpawner == null) zombieSpawner = FindAnyObjectByType<ZombieSpawner>();

        if (onFootPlayer == null)
        {
            onFootPlayer = FindAnyObjectByType<OnFootPlayerController>(FindObjectsInactive.Include);
        }
        if (onFootPlayer != null)
        {
            onFootPlayer.gameObject.SetActive(false); // Luôn ẩn lúc đang lái xe
        }

        if (cameraFollow == null) cameraFollow = FindAnyObjectByType<CameraFollow>();
        if (mapManager == null) mapManager = FindAnyObjectByType<EndlessMapManager>();
        if (obstacleSpawner == null) obstacleSpawner = FindAnyObjectByType<ObstacleSpawner>();
        if (parallaxBackground == null) parallaxBackground = FindAnyObjectByType<InfiniteParallaxBackground>();
        if (levelCompleteUI == null) levelCompleteUI = FindAnyObjectByType<LevelCompleteUI>();

        // Thiết lập hiển thị thanh máu
        if (carHealthBar == null) carHealthBar = GameObject.Find("HPBar");
        if (onFootHealthBar == null) onFootHealthBar = GameObject.Find("OnFootHPBar");

        if (carHealthBar != null) carHealthBar.SetActive(true);
        if (onFootHealthBar != null) onFootHealthBar.SetActive(false);

        // Áp dụng cấu hình chặng 1
        ApplyStageSettings(_currentStageIndex, isInstant: true);
    }

    private void Update()
    {
        if (playerTransform == null || _currentState != LevelState.NormalDriving) return;

        float currentDistance = playerTransform.position.x - _startPlayerX;

        if (_currentStageIndex < stages.Length)
        {
            LevelStageInfo stage = stages[_currentStageIndex];

            // Khi còn cách cuối chặng 45m: Kích hoạt đợt Zombie tràn ra ồ ạt đón đầu (Cao trào cuối chặng)
            if (!_isClimaxSpawnActive && currentDistance >= stage.stageDistance - 45f)
            {
                _isClimaxSpawnActive = true;
                if (zombieSpawner != null)
                {
                    zombieSpawner.SetClimaxSpawnRate(0.55f); // Tăng tốc độ sinh quái gấp đôi
                }
            }

            // Khi xe chạm mốc kết thúc chặng: Tấp xe vào lề và cho người chơi xuống xe
            if (currentDistance >= stage.stageDistance)
            {
                StartCoroutine(TriggerOnFootEncounterSequence());
            }
        }
    }

    // Áp dụng thông số (Ánh sáng, background, độ khó zombie) của chặng
    private void ApplyStageSettings(int stageIndex, bool isInstant)
    {
        if (stageIndex < 0 || stageIndex >= stages.Length) return;
        LevelStageInfo stage = stages[stageIndex];

        // 1. Cập nhật độ khó Zombie Spawner
        if (zombieSpawner != null)
        {
            zombieSpawner.SetStageDifficulty(
                stageIndex,
                stage.zombieHpMultiplier,
                stage.spawnInterval,
                stage.minHorde,
                stage.maxHorde
            );
            zombieSpawner.SetSpecialSpawnChances(stage.jumperSpawnChance, stage.chargerSpawnChance);
        }

        // 2. Cập nhật ánh sáng
        if (globalLight2D != null && isInstant)
        {
            globalLight2D.color = stage.lightColor;
            globalLight2D.intensity = stage.lightIntensity;
        }

        // 3. Cập nhật Background
        if (parallaxBackground != null && isInstant)
        {
            parallaxBackground.ApplyStageSprites(stageIndex);
        }

        ShowStageAnnouncement(stage.stageName);
        _isClimaxSpawnActive = false;
    }

    // Chuỗi dừng xe, tấp lề góc trên bên trái, người chơi xuống xe bắn Zombie
    private IEnumerator TriggerOnFootEncounterSequence()
    {
        _currentState = LevelState.ParkingVehicle;

        // Tắt điều khiển xe và súng xe
        if (_playerMovement != null) _playerMovement.enabled = false;
        PlayerShooting carShooting = playerTransform != null ? playerTransform.GetComponent<PlayerShooting>() : null;
        if (carShooting != null) carShooting.enabled = false;

        // 1. Tự động tấp xe vào lề trên bên trái (Làn trên cùng)
        float topParkY = 2.4f;
        if (_playerMovement != null)
        {
            topParkY = _playerMovement.GetLaneY(_playerMovement.GetTotalLanes() - 1);
        }

        Vector3 startCarPos = playerTransform != null ? playerTransform.position : Vector3.zero;
        float targetParkX = startCarPos.x + 8f; // Trôi nhẹ 8m rồi dừng hẳn
        Vector3 targetParkPos = new Vector3(targetParkX, topParkY, startCarPos.z);

        float parkElapsed = 0f;
        float parkDuration = 0.8f;
        while (parkElapsed < parkDuration)
        {
            parkElapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, parkElapsed / parkDuration);
            if (playerTransform != null)
            {
                playerTransform.position = Vector3.Lerp(startCarPos, targetParkPos, t);
            }
            yield return null;
        }
        if (playerTransform != null) playerTransform.position = targetParkPos;

        // 2. Thiết lập phạm vi đấu trường (tự động tương thích với mép đường của từng Map)
        float arenaLeft = targetParkPos.x - 4f;
        float arenaRight = targetParkPos.x + 22f;
        float arenaBottom = _playerMovement != null ? _playerMovement.GetMinYLimit() : (zombieSpawner != null ? zombieSpawner.GetRoadMinY() : -1.2f);
        float arenaTop = _playerMovement != null ? _playerMovement.GetMaxYLimit() : (zombieSpawner != null ? zombieSpawner.GetRoadMaxY() : 3.2f);
        float centerArenaY = (arenaBottom + arenaTop) * 0.5f;

        // Tạm dừng sinh quái thường và vật cản
        if (mapManager != null) mapManager.SetSpawningPaused(true);
        if (zombieSpawner != null) zombieSpawner.SetSpawningActive(false);
        if (obstacleSpawner != null) obstacleSpawner.enabled = false;

        // Dọn dẹp quái lẻ cũ trên đường
        Zombie[] leftoverZombies = FindObjectsByType<Zombie>(FindObjectsSortMode.None);
        foreach (var z in leftoverZombies)
        {
            if (z != null) Destroy(z.gameObject);
        }

        // 3. Cho người chơi bước xuống xe
        if (onFootPlayer != null)
        {
            float playerSpawnY = Mathf.Clamp(targetParkPos.y - 0.8f, arenaBottom + 0.3f, arenaTop - 0.3f);
            Vector3 spawnPos = new Vector3(targetParkPos.x + 1.2f, playerSpawnY, 0f);
            onFootPlayer.transform.position = spawnPos;
            onFootPlayer.SetArenaBounds(arenaLeft, arenaRight, arenaBottom, arenaTop);
            onFootPlayer.gameObject.SetActive(true);

            PlayerShooting footShooting = onFootPlayer.GetComponent<PlayerShooting>();
            if (footShooting != null) footShooting.enabled = true;

            // Khóa Camera bám theo người đi bộ ở trung tâm làn đường
            if (cameraFollow != null)
            {
                cameraFollow.SetTarget(onFootPlayer.transform);
                cameraFollow.SetArenaLock(true, targetParkPos.x + 4f, targetParkPos.x + 18f, centerArenaY);
            }

            // Đổi thanh máu
            if (carHealthBar != null) carHealthBar.SetActive(false);
            if (onFootHealthBar != null) onFootHealthBar.SetActive(true);
        }

        _currentState = LevelState.OnFootDefense;

        LevelStageInfo currentStage = stages[_currentStageIndex];
        _totalWaveZombies = currentStage.onFootZombieCount;
        _remainingWaveZombies = _totalWaveZombies;

        // Thông báo nhiệm vụ bằng tiếng Anh
        string waveNotice = !string.IsNullOrEmpty(currentStage.onFootPromptText)
            ? currentStage.onFootPromptText
            : _currentStageIndex switch
            {
                0 => $"STAGE 1 CHECKPOINT!\nDISMOUNT AND CLEAR {_totalWaveZombies} ZOMBIES TO PROCEED!",
                1 => $"STAGE 2 CHECKPOINT!\nCLEAR {_totalWaveZombies} MUTATED ZOMBIES!",
                _ => $"FINAL OUTBREAK!\nELIMINATE {_totalWaveZombies} ZOMBIES TO ESCAPE THE CITY!"
            };
        ShowStageAnnouncement(waveNotice, 4f);

        yield return new WaitForSeconds(1.5f);

        // Nếu Zombie Spawner bị tắt thì tự động bỏ qua đợt quái
        if (zombieSpawner == null || !zombieSpawner.enabled || !zombieSpawner.gameObject.activeInHierarchy || !zombieSpawner.IsSpawningEnabled())
        {
            yield return new WaitForSeconds(1.0f);
            StartCoroutine(RemountVehicleAndProceed());
            yield break;
        }

        // 4. Sinh đợt Zombie bao vây với chỉ số máu và tỉ lệ dị thường của chặng này
        Transform target = onFootPlayer != null ? onFootPlayer.transform : playerTransform;
        if (zombieSpawner != null)
        {
            zombieSpawner.SpawnOnFootWave(
                target,
                _totalWaveZombies,
                arenaLeft,
                arenaRight,
                arenaBottom,
                arenaTop,
                currentStage.zombieHpMultiplier,
                currentStage.onFootSpecialChance
            );
        }
    }

    // Xử lý khi 1 Zombie trong đợt sóng bị hạ gục
    private void HandleWaveZombieDied(Zombie zombie)
    {
        if (_currentState != LevelState.OnFootDefense) return;

        _remainingWaveZombies = Mathf.Max(0, _remainingWaveZombies - 1);
        UpdateWaveCountAnnouncement();

        // Khi toàn bộ Zombie trong đợt bị tiêu diệt
        if (_remainingWaveZombies <= 0)
        {
            StartCoroutine(RemountVehicleAndProceed());
        }
    }

    private void UpdateWaveCountAnnouncement()
    {
        if (_remainingWaveZombies > 0)
        {
            ShowStageAnnouncement($"ZOMBIES: {_remainingWaveZombies} / {_totalWaveZombies}", 1.5f);
        }
    }

    // Người chơi bước lên lại xe, hoàn thành chặng và chuyển sang chặng tiếp theo
    private IEnumerator RemountVehicleAndProceed()
    {
        _currentState = LevelState.ParkingVehicle;

        ShowStageAnnouncement("HORDE CLEARED!\nRETURN TO VEHICLE AND PROCEED!", 2.5f);
        yield return new WaitForSeconds(1.2f);

        // 1. Ẩn người đi bộ, hiện lại thanh máu xe
        if (onFootPlayer != null) onFootPlayer.gameObject.SetActive(false);
        if (onFootHealthBar != null) onFootHealthBar.SetActive(false);
        if (carHealthBar != null) carHealthBar.SetActive(true);

        // 2. Bật lại điều khiển xe và súng trên xe
        if (_playerMovement != null) _playerMovement.enabled = true;
        PlayerShooting carShooting = playerTransform != null ? playerTransform.GetComponent<PlayerShooting>() : null;
        if (carShooting != null) carShooting.enabled = true;

        // 3. Trả Camera về bám theo xe và gỡ bỏ khóa đấu trường
        if (cameraFollow != null)
        {
            cameraFollow.SetTarget(playerTransform);
            cameraFollow.SetArenaLock(false);
        }

        // 4. Mở khóa bản đồ và vật cản
        if (mapManager != null) mapManager.SetSpawningPaused(false);
        if (obstacleSpawner != null) obstacleSpawner.enabled = true;

        // 5. Kiểm tra xem đã hoàn thành chặng cuối (Chặng 3) chưa
        if (_currentStageIndex >= stages.Length - 1)
        {
            StartCoroutine(TriggerVictorySequence());
        }
        else
        {
            // Nếu là Chặng 1 hoặc 2 -> Cutscene chuyển sang chặng tiếp theo
            StartCoroutine(TriggerStageTransitionCutscene());
        }
    }

    // Cutscene chuyển chặng: Xe phóng nhanh Nitro, ánh sáng & background fade mượt mà
    private IEnumerator TriggerStageTransitionCutscene()
    {
        _currentState = LevelState.TransitionCutscene;

        int nextStageIndex = _currentStageIndex + 1;
        LevelStageInfo nextStage = stages[nextStageIndex];

        ShowStageAnnouncement($"STAGE {_currentStageIndex + 1} COMPLETE!\nENTERING {nextStage.stageName.ToUpper()}", 3.5f);

        // 1. Xe tăng tốc Nitro phóng vút qua
        if (_playerMovement != null)
        {
            _playerMovement.SetSpeed(22f);
        }

        // 2. Đổi Background Parallax (Fade 2.0s)
        if (parallaxBackground != null)
        {
            parallaxBackground.SwitchToStage(nextStageIndex, fadeDuration: 2.0f);
        }

        // 3. Fade màu ánh sáng Global Light 2D sang chặng mới
        if (globalLight2D != null)
        {
            Color startColor = globalLight2D.color;
            float startIntensity = globalLight2D.intensity;
            float elapsed = 0f;
            float duration = 2.5f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                globalLight2D.color = Color.Lerp(startColor, nextStage.lightColor, t);
                globalLight2D.intensity = Mathf.Lerp(startIntensity, nextStage.lightIntensity, t);
                yield return null;
            }

            globalLight2D.color = nextStage.lightColor;
            globalLight2D.intensity = nextStage.lightIntensity;
        }
        else
        {
            yield return new WaitForSeconds(2.5f);
        }

        // 4. Áp dụng độ khó và máu mới cho Zombie ở chặng mới
        _currentStageIndex = nextStageIndex;
        ApplyStageSettings(_currentStageIndex, isInstant: false);

        if (zombieSpawner != null)
        {
            zombieSpawner.SetSpawningActive(true);
        }

        _currentState = LevelState.NormalDriving;
    }

    // Chuỗi sự kiện Chiến Thắng Map 1 (Hoàn thành cả 3 chặng)
    private IEnumerator TriggerVictorySequence()
    {
        _currentState = LevelState.LevelWon;

        ShowStageAnnouncement(mapVictoryText, 4f);

        if (zombieSpawner != null) zombieSpawner.SetSpawningActive(false);

        yield return new WaitForSeconds(2.0f);

        // Hiện bảng Level Complete để bấm sang Map 2
        if (levelCompleteUI != null)
        {
            levelCompleteUI.ShowCompletePanel();
        }
        else
        {
            GameObject lcGo = GameObject.Find("LevelCompletePanel");
            if (lcGo != null) lcGo.SetActive(true);
        }
    }

    private void ShowStageAnnouncement(string message, float duration = 3f)
    {
        if (stageAnnouncementText != null)
        {
            stageAnnouncementText.text = message;
            stageAnnouncementText.gameObject.SetActive(true);
            CancelInvoke(nameof(HideStageAnnouncement));
            Invoke(nameof(HideStageAnnouncement), duration);
        }
    }

    private void HideStageAnnouncement()
    {
        if (stageAnnouncementText != null)
        {
            stageAnnouncementText.gameObject.SetActive(false);
        }
    }

    public int GetCurrentStageIndex() => _currentStageIndex;
    public LevelState GetCurrentState() => _currentState;

    [ContextMenu("TEST: Chuyển Sang Chặng 1 (Sáng)")]
    public void TestSwitchToStage1()
    {
        _currentStageIndex = 0;
        ApplyStageSettings(0, isInstant: true);
    }

    [ContextMenu("TEST: Chuyển Sang Chặng 2 (Hoàng Hôn)")]
    public void TestSwitchToStage2()
    {
        _currentStageIndex = 1;
        ApplyStageSettings(1, isInstant: true);
    }

    [ContextMenu("TEST: Chuyển Sang Chặng 3 (Đêm Tàn Tích)")]
    public void TestSwitchToStage3()
    {
        _currentStageIndex = 2;
        ApplyStageSettings(2, isInstant: true);
    }
}
