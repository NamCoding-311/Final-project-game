using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

// Dữ liệu cấu hình cho 1 Chặng
[Serializable]
public class LevelStageInfo
{
    public string stageName = "Chặng 1: Sông Ban Ngày";
    public float bossSpawnDistance = 250f;      // Quãng đường xe chạy đến thì Boss xuất hiện
    public GameObject bossPrefab;               // Prefab của Boss chặng này
    public Color lightColor = Color.white;      // Màu ánh sáng Global Light 2D
    public float lightIntensity = 1f;           // Cường độ sáng
}

// Trạng thái của màn chơi
public enum LevelState
{
    NormalDriving,      // Đang chạy đường trường
    BossWarning,        // Còi báo động Boss sắp xuất hiện
    BossFight,          // Đang giao chiến với Boss
    TransitionCutscene, // Cutscene tăng tốc chuyển giao sang chặng tiếp theo
    LevelWon            // Chiến thắng hoàn thành cả 3 chặng
}

// Quản lý tiến trình 3 Chặng (Sáng -> Chiều -> Tối), đấu Trùm và Cutscene chuyển cảnh trên 1 Scene duy nhất
public class StageLevelManager : MonoBehaviour
{
    [Header("Player & Light References")]
    [SerializeField] private Transform playerTransform;
    [SerializeField] private Light2D globalLight2D;
    [SerializeField] private ZombieSpawner zombieSpawner;

    [Header("On-Foot Player & Arena Setup")]
    // Nhân vật người đi bộ khi xuống xe
    [SerializeField] private OnFootPlayerController onFootPlayer;

    // Tham chiếu Camera, Bản đồ và Spawner để khóa/mở khóa đấu trường
    [SerializeField] private CameraFollow cameraFollow;
    [SerializeField] private EndlessMapManager mapManager;
    [SerializeField] private ObstacleSpawner obstacleSpawner;
    [SerializeField] private InfiniteParallaxBackground parallaxBackground;

    [Header("Stages Setup (3 Chặng Sáng - Chiều - Tối)")]
    [SerializeField] private LevelStageInfo[] stages = new LevelStageInfo[]
    {
        new LevelStageInfo { stageName = "Chặng 1: Sông Ban Ngày", bossSpawnDistance = 250f, lightColor = Color.white, lightIntensity = 1f },
        new LevelStageInfo { stageName = "Chặng 2: Phố Hoàng Hôn", bossSpawnDistance = 500f, lightColor = new Color(1f, 0.6f, 0.3f), lightIntensity = 0.9f },
        new LevelStageInfo { stageName = "Chặng 3: Đêm Tàn Tích", bossSpawnDistance = 800f, lightColor = new Color(0.2f, 0.3f, 0.5f), lightIntensity = 0.7f }
    };

    [Header("Player Health Bars (Thanh máu Xe & Người)")]
    // Thanh máu của Xe (sẽ ẩn khi đánh Boss)
    [SerializeField] private GameObject carHealthBar;

    // Thanh máu của Người đi bộ (Trashcan - chỉ hiện khi xuống xe đánh Boss)
    [SerializeField] private GameObject onFootHealthBar;

    [Header("UI Announcements")]
    // Text thông báo tên chặng
    [SerializeField] private TextMeshProUGUI stageAnnouncementText;

    // Banner cảnh báo Boss xuất hiện
    [SerializeField] private GameObject bossWarningBanner;

    // Thanh máu của Boss trên đỉnh màn hình
    [SerializeField] private Slider bossHealthBar;
    [SerializeField] private TextMeshProUGUI bossNameText;

    // Màn hình Chiến Thắng
    [SerializeField] private GameObject victoryPanel;

    private int _currentStageIndex = 0;
    private LevelState _currentState = LevelState.NormalDriving;
    private float _startPlayerX;
    private ZombieBoss _activeBoss;
    private Player3LaneMovement _playerMovement;

    private void OnEnable()
    {
        ZombieBoss.OnBossHealthChanged += UpdateBossHealthUI;
        ZombieBoss.OnBossDied += HandleBossDefeated;
    }

    private void OnDisable()
    {
        ZombieBoss.OnBossHealthChanged -= UpdateBossHealthUI;
        ZombieBoss.OnBossDied -= HandleBossDefeated;
    }

    private void Awake()
    {
        // Tự động tắt hoàn toàn nếu đang ở trong chế độ Đấu trường Sinh Tồn (Brotato)
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "ArenaSurvival" || FindAnyObjectByType<ArenaManager>() != null)
        {
            enabled = false;
            return;
        }
    }

    private void Start()
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

        if (globalLight2D == null)
        {
            globalLight2D = FindAnyObjectByType<Light2D>();
        }

        if (zombieSpawner == null)
        {
            zombieSpawner = FindAnyObjectByType<ZombieSpawner>();
        }

        if (onFootPlayer == null)
        {
            onFootPlayer = FindAnyObjectByType<OnFootPlayerController>(FindObjectsInactive.Include);
        }
        if (onFootPlayer != null)
        {
            onFootPlayer.gameObject.SetActive(false); // Ẩn lúc đang lái xe
        }

        if (cameraFollow == null)
        {
            cameraFollow = FindAnyObjectByType<CameraFollow>();
        }

        if (mapManager == null)
        {
            mapManager = FindAnyObjectByType<EndlessMapManager>();
        }

        if (obstacleSpawner == null)
        {
            obstacleSpawner = FindAnyObjectByType<ObstacleSpawner>();
        }

        if (parallaxBackground == null)
        {
            parallaxBackground = FindAnyObjectByType<InfiniteParallaxBackground>();
        }

        // Cài đặt trạng thái hiển thị thanh máu ban đầu (Hiện máu xe, ẩn máu người đi bộ)
        if (carHealthBar == null)
        {
            GameObject hpGo = GameObject.Find("HPBar");
            if (hpGo != null) carHealthBar = hpGo;
        }
        if (onFootHealthBar == null)
        {
            GameObject onFootHpGo = GameObject.Find("OnFootHPBar");
            if (onFootHpGo != null) onFootHealthBar = onFootHpGo;
        }

        if (carHealthBar != null) carHealthBar.SetActive(true);
        if (onFootHealthBar != null) onFootHealthBar.SetActive(false);

        // Tắt các UI không cần thiết lúc bắt đầu
        if (bossWarningBanner != null) bossWarningBanner.SetActive(false);
        if (bossHealthBar != null) bossHealthBar.gameObject.SetActive(false);
        if (victoryPanel != null) victoryPanel.SetActive(false);

        // Khởi tạo ánh sáng và background chặng 1
        ApplyInitialStage();
    }

    private void Update()
    {
        if (playerTransform == null || _currentState != LevelState.NormalDriving) return;

        float currentDistance = playerTransform.position.x - _startPlayerX;

        // Kiểm tra xem đã đến mốc đấu Boss của chặng hiện tại chưa
        if (_currentStageIndex < stages.Length)
        {
            if (currentDistance >= stages[_currentStageIndex].bossSpawnDistance)
            {
                StartCoroutine(TriggerBossEncounterSequence());
            }
        }
    }

    // Thiết lập ánh sáng, background và thông báo chặng 1
    private void ApplyInitialStage()
    {
        if (stages.Length > 0)
        {
            if (globalLight2D != null)
            {
                globalLight2D.color = stages[0].lightColor;
                globalLight2D.intensity = stages[0].lightIntensity;
            }

            if (parallaxBackground != null)
            {
                parallaxBackground.ApplyStageSprites(0);
            }

            ShowStageAnnouncement(stages[0].stageName);
        }
    }

    // Chuỗi dừng xe, tấp lề góc trên bên trái, người chơi xuống xe và khóa đấu trường Boss
    private IEnumerator TriggerBossEncounterSequence()
    {
        _currentState = LevelState.BossWarning;

        // Tắt điều khiển người chơi trên xe
        if (_playerMovement != null)
        {
            _playerMovement.enabled = false;
        }

        // Tắt súng trên xe để không bắn tự động khi người chơi đã xuống xe
        PlayerShooting carShooting = playerTransform != null ? playerTransform.GetComponent<PlayerShooting>() : null;
        if (carShooting != null) carShooting.enabled = false;

        // 1. Tự động lái xe tấp vào lề đường ở góc trên bên trái (Top-Left Parking)
        float topParkY = 2.4f; // Làn trên cùng sát lề đường
        if (_playerMovement != null)
        {
            topParkY = _playerMovement.GetLaneY(_playerMovement.GetTotalLanes() - 1);
        }

        Vector3 startCarPos = playerTransform != null ? playerTransform.position : Vector3.zero;
        float targetParkX = startCarPos.x + 8f; // Xe giảm tốc trôi nhẹ 8m về phía trước rồi đỗ hẳn
        Vector3 targetParkPos = new Vector3(targetParkX, topParkY, startCarPos.z);

        // Hiệu ứng xe giảm tốc và tấp dần lên lề trên bên trái trong 0.8 giây
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
        if (playerTransform != null)
        {
            playerTransform.position = targetParkPos;
        }

        // 2. Tính toán phạm vi đấu trường (Arena) quanh vị trí đỗ xe ở góc trên bên trái
        float arenaLeft = targetParkPos.x - 4f;
        float arenaRight = targetParkPos.x + 22f;
        float arenaBottom = -1.2f;
        float arenaTop = 3.2f;

        // Tạm ngưng bản đồ và quái thường
        if (mapManager != null) mapManager.SetSpawningPaused(true);
        if (zombieSpawner != null) zombieSpawner.SetSpawningActive(false);
        if (obstacleSpawner != null) obstacleSpawner.enabled = false;

        // Tiêu diệt toàn bộ Zombie con tàn dư trên màn hình để đấu trường sạch sẽ cho trận đánh Boss
        Zombie[] leftoverZombies = FindObjectsByType<Zombie>();
        foreach (var z in leftoverZombies)
        {
            if (z != null)
            {
                Destroy(z.gameObject);
            }
        }

        // 3. Cho người chơi bước xuống xe (Kích hoạt OnFootPlayer ngay dưới mép xe đỗ)
        if (onFootPlayer != null)
        {
            Vector3 spawnPos = new Vector3(targetParkPos.x + 1.2f, targetParkPos.y - 1.2f, 0f);
            onFootPlayer.transform.position = spawnPos;
            onFootPlayer.SetArenaBounds(arenaLeft, arenaRight, arenaBottom, arenaTop);
            onFootPlayer.gameObject.SetActive(true);

            PlayerShooting footShooting = onFootPlayer.GetComponent<PlayerShooting>();
            if (footShooting != null) footShooting.enabled = true;

            // Chuyển Camera sang bám theo người đi bộ và khóa khung nhìn trong đấu trường
            if (cameraFollow != null)
            {
                cameraFollow.SetTarget(onFootPlayer.transform);
                cameraFollow.SetArenaLock(true, targetParkPos.x + 4f, targetParkPos.x + 18f, 0f);
            }

            // Ẩn thanh máu của xe, hiện thanh máu của nhân vật đi bộ (Trashcan)
            if (carHealthBar != null) carHealthBar.SetActive(false);
            if (onFootHealthBar != null) onFootHealthBar.SetActive(true);

            // Chuyển toàn bộ Zombie thường và Boss sang tập trung rượt đuổi người chơi đi bộ thay vì cái xe
            RedirectAllZombiesTo(onFootPlayer.transform);
        }

        // 4. Nhấp nháy cảnh báo Boss
        if (bossWarningBanner != null) bossWarningBanner.SetActive(true);

        yield return new WaitForSeconds(2.0f);

        if (bossWarningBanner != null) bossWarningBanner.SetActive(false);

        // 5. Sinh Boss ra phía đối diện trong sàn đấu
        SpawnBossForCurrentStage(arenaLeft, arenaRight, arenaBottom, arenaTop);

        _currentState = LevelState.BossFight;
    }

    // Sinh Boss và gán mục tiêu tấn công là nhân vật đi bộ
    private void SpawnBossForCurrentStage(float leftX, float rightX, float bottomY, float topY)
    {
        LevelStageInfo currentStage = stages[_currentStageIndex];

        Transform targetTransform = onFootPlayer != null && onFootPlayer.gameObject.activeInHierarchy
            ? onFootPlayer.transform
            : playerTransform;

        // Vị trí sinh Boss:  xuất hiện ngay mép phải màn hình
        Vector3 spawnPos = new Vector3(targetTransform.position.x + 14f, targetTransform.position.y, 0f);

        GameObject bossObj = null;
        if (currentStage.bossPrefab != null)
        {
            bossObj = Instantiate(currentStage.bossPrefab, spawnPos, Quaternion.identity);
        }
        else
        {
            // Dự phòng: Nếu chưa tạo riêng Prefab Boss, tự động lấy Zombie thường phóng to 1.8x và tăng máu
            GameObject zombieObj = GameObject.FindGameObjectWithTag("Zombie");
            if (zombieObj != null)
            {
                bossObj = Instantiate(zombieObj, spawnPos, Quaternion.identity);
                bossObj.transform.localScale *= 1.8f;
                ZombieBoss zb = bossObj.AddComponent<ZombieBoss>();
                zb.TakeDamage(-1); // Kích hoạt sự kiện
            }
        }

        if (bossObj != null)
        {
            _activeBoss = bossObj.GetComponent<ZombieBoss>();
            if (_activeBoss != null)
            {
                _activeBoss.SetTarget(targetTransform);
                _activeBoss.SetArenaBounds(leftX, rightX, bottomY, topY);
            }

            if (bossHealthBar != null)
            {
                bossHealthBar.gameObject.SetActive(true);
                if (_activeBoss != null && bossNameText != null)
                {
                    bossNameText.text = _activeBoss.GetBossName();
                }
            }
        }
    }

    // Cập nhật thanh máu Boss
    private void UpdateBossHealthUI(ZombieBoss boss, int currentHp, int maxHp)
    {
        if (bossHealthBar != null)
        {
            bossHealthBar.maxValue = maxHp;
            bossHealthBar.value = currentHp;
        }
    }

    // Xử lý khi Boss bị tiêu diệt
    private void HandleBossDefeated(ZombieBoss boss)
    {
        if (bossHealthBar != null) bossHealthBar.gameObject.SetActive(false);

        // Kích hoạt chuỗi bước lên xe và tiếp tục hành trình
        StartCoroutine(RemountVehicleAndProceed());
    }

    // Người chơi bước lên lại xe, mở khóa đấu trường và chuyển sang chặng tiếp theo
    private IEnumerator RemountVehicleAndProceed()
    {
        ShowStageAnnouncement("⚔️ HẠ GỤC TRÙM!\nBƯỚC LÊN XE TIẾP TỤC HÀNH TRÌNH");

        yield return new WaitForSeconds(1.2f);

        // 1. Ẩn nhân vật người đi bộ
        if (onFootPlayer != null)
        {
            onFootPlayer.gameObject.SetActive(false);
        }

        // Ẩn thanh máu của người đi bộ, hiện lại thanh máu của xe
        if (onFootHealthBar != null) onFootHealthBar.SetActive(false);
        if (carHealthBar != null) carHealthBar.SetActive(true);

        // Chuyển toàn bộ Zombie nhắm lại vào xe
        RedirectAllZombiesTo(playerTransform);

        // 2. Bật lại điều khiển xe và súng trên xe
        if (_playerMovement != null)
        {
            _playerMovement.enabled = true;
        }

        PlayerShooting carShooting = playerTransform != null ? playerTransform.GetComponent<PlayerShooting>() : null;
        if (carShooting != null) carShooting.enabled = true;

        // 3. Trả Camera về bám theo xe và gỡ bỏ khóa đấu trường
        if (cameraFollow != null)
        {
            cameraFollow.SetTarget(playerTransform);
            cameraFollow.SetArenaLock(false);
        }

        // 4. Mở khóa bản đồ và chướng ngại vật
        if (mapManager != null) mapManager.SetSpawningPaused(false);
        if (obstacleSpawner != null) obstacleSpawner.enabled = true;

        // 5. Nếu là Boss cuối cùng (Chặng 3) -> CHIẾN THẮNG
        if (_currentStageIndex >= stages.Length - 1)
        {
            StartCoroutine(TriggerVictorySequence());
        }
        else
        {
            // Nếu là Chặng 1 hoặc 2 -> Cutscene tăng tốc chuyển sang chặng tiếp theo
            StartCoroutine(TriggerStageTransitionCutscene());
        }
    }

    // Chuyển hướng toàn bộ Zombie thường đang có trong Scene sang nhắm vào mục tiêu mới
    public void RedirectAllZombiesTo(Transform newTarget)
    {
        if (newTarget == null) return;
        Zombie[] allZombies = FindObjectsByType<Zombie>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (var z in allZombies)
        {
            if (z != null) z.SetTarget(newTarget);
        }
    }

    // Cutscene chuyển chặng: Xe tăng tốc Nitro, ánh sáng đổi màu, thông báo chặng mới
    private IEnumerator TriggerStageTransitionCutscene()
    {
        _currentState = LevelState.TransitionCutscene;

        int nextStageIndex = _currentStageIndex + 1;
        LevelStageInfo nextStage = stages[nextStageIndex];

        ShowStageAnnouncement($"HOÀN THÀNH CHẶNG {_currentStageIndex + 1}!\nTIẾN VÀO {nextStage.stageName.ToUpper()}");

        // 1. Kích hoạt xe tăng tốc phóng nhanh lướt qua xác Boss
        if (_playerMovement != null)
        {
            _playerMovement.SetSpeed(24f);
        }

        // 2. Chuyển đổi Background Parallax sang chặng mới (Crossfade mượt mà 2.0s)
        if (parallaxBackground != null)
        {
            parallaxBackground.SwitchToStage(nextStageIndex, fadeDuration: 2.0f);
        }

        // 3. Chuyển đổi màu ánh sáng Global Light 2D mượt mà (Fade sang màu của chặng mới)
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

        // 3. Chuyển sang chặng mới
        _currentStageIndex = nextStageIndex;

        // Bật lại sinh zombie thường
        if (zombieSpawner != null) zombieSpawner.SetSpawningActive(true);

        _currentState = LevelState.NormalDriving;
    }

    // Chuỗi sự kiện Chiến Thắng Màn Chơi (Victory)
    private IEnumerator TriggerVictorySequence()
    {
        _currentState = LevelState.LevelWon;

        ShowStageAnnouncement("🎉 CHIẾN THẮNG! BẠN ĐÃ SỐNG SÓT QUA ĐÊM!");

        if (zombieSpawner != null) zombieSpawner.SetSpawningActive(false);

        yield return new WaitForSeconds(2.0f);

        if (victoryPanel != null)
        {
            victoryPanel.SetActive(true);
        }
    }

    // Hiển thị dòng chữ thông báo trên màn hình
    private void ShowStageAnnouncement(string message)
    {
        if (stageAnnouncementText != null)
        {
            stageAnnouncementText.text = message;
            stageAnnouncementText.gameObject.SetActive(true);
            CancelInvoke(nameof(HideStageAnnouncement));
            Invoke(nameof(HideStageAnnouncement), 3f);
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
        if (parallaxBackground != null) parallaxBackground.SwitchToStage(0, 2f);
        if (globalLight2D != null && stages.Length > 0)
        {
            globalLight2D.color = stages[0].lightColor;
            globalLight2D.intensity = stages[0].lightIntensity;
        }
        _currentStageIndex = 0;
        ShowStageAnnouncement("TEST: TIẾN VÀO " + stages[0].stageName.ToUpper());
    }

    [ContextMenu("TEST: Chuyển Sang Chặng 2 (Hoàng Hôn)")]
    public void TestSwitchToStage2()
    {
        if (parallaxBackground != null) parallaxBackground.SwitchToStage(1, 2f);
        if (globalLight2D != null && stages.Length > 1)
        {
            globalLight2D.color = stages[1].lightColor;
            globalLight2D.intensity = stages[1].lightIntensity;
        }
        _currentStageIndex = 1;
        ShowStageAnnouncement("TEST: TIẾN VÀO " + stages[1].stageName.ToUpper());
    }

    [ContextMenu("TEST: Chuyển Sang Chặng 3 (Đêm Tàn Tích)")]
    public void TestSwitchToStage3()
    {
        if (parallaxBackground != null) parallaxBackground.SwitchToStage(2, 2f);
        if (globalLight2D != null && stages.Length > 2)
        {
            globalLight2D.color = stages[2].lightColor;
            globalLight2D.intensity = stages[2].lightIntensity;
        }
        _currentStageIndex = 2;
        ShowStageAnnouncement("TEST: TIẾN VÀO " + stages[2].stageName.ToUpper());
    }
}
