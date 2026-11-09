using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Quản lý Bàn Tác Chiến Bunker (Tactical War Room / Radio Dispatch)
// Cho phép chọn các chặng chiến dịch (Sector 1 -> Sector 2 -> Sector 3),
// xem tình báo quái vật, nghe điện đàm chỉ huy và xuất kích chiến đấu.
public class TacticalWarRoomManager : MonoBehaviour
{
    [System.Serializable]
    public class MissionSectorInfo
    {
        public int sectorIndex; // 1, 2, 3
        public string sectorCode = "SECTOR 01";
        public string sectorName = "HIGHWAY OUTSKIRTS";
        public string sceneToLoad = "map1";
        public int threatLevel = 2; // 1 -> 5
        public float targetDistance = 160f;
        public string enemyIntel = "Zombies, Jumpers, Chargers";
        public string bossIntel = "Outskirts Horde (No Boss)";
        [TextArea(3, 5)]
        public string radioDispatch = "HQ to Survivor: Outskirts highway is swarming with runners. Punch through their blockade, dismount and secure Checkpoint Alpha!";
        public string[] objectives = new string[]
        {
            "Drive through highway barricades (160m)",
            "Clear 10 Outskirts Zombies on foot",
            "Unlock route to Sector 02"
        };
        public string rewardText = "+1,500 Cash  |  Unlock Military Truck";
        public int cashReward = 1500;
        public Sprite sectorPreviewSprite;
    }

    [Header("Mission Data")]
    [SerializeField] private MissionSectorInfo[] sectors;

    [Header("UI Panels")]
    [SerializeField] private GameObject warRoomPanel;
    [SerializeField] private GameObject lobbyDefaultHUD;

    [Header("Dossier UI Elements (Hồ Sơ Nhiệm Vụ)")]
    [SerializeField] private TextMeshProUGUI sectorTitleText;
    [SerializeField] private TextMeshProUGUI threatLevelText;
    [SerializeField] private TextMeshProUGUI distanceText;
    [SerializeField] private TextMeshProUGUI enemyIntelText;
    [SerializeField] private TextMeshProUGUI bossIntelText;
    [SerializeField] private TextMeshProUGUI radioTranscriptText;
    [SerializeField] private TextMeshProUGUI objectivesText;
    [SerializeField] private TextMeshProUGUI rewardsText;
    [SerializeField] private Image sectorPreviewImage;

    [Header("Deploy & Action Buttons")]
    [SerializeField] private Button deployButton;
    [SerializeField] private TextMeshProUGUI deployButtonText;
    [SerializeField] private Button closeButton;
    [SerializeField] private Button openWarRoomButton;
    [SerializeField] private Button returnToMenuButton;

    [Header("Map Pins (Điểm Ghim Bản Đồ)")]
    [SerializeField] private Button[] mapPinButtons;
    [SerializeField] private GameObject[] pinLockedIcons;
    [SerializeField] private GameObject[] pinClearedIcons;
    [SerializeField] private Image[] pinSelectionGlows;

    [Header("Audio")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioClip lobbyMusicClip;

    [Header("Player Economy")]
    [SerializeField] private TextMeshProUGUI cashDisplayTopText;

    private int _selectedSectorIndex = 0;
    private int _maxUnlockedSector = 1;
    private Coroutine _typewriterRoutine;

    private const string PREF_UNLOCKED_SECTOR = "MaxUnlockedSector";
    private const string PREF_PLAYER_CASH = "PlayerCash";

    public static TacticalWarRoomManager Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
        InitializeDefaultSectors();
        LoadProgression();
    }

    private void OnEnable()
    {
        GameRewardManager.OnCashChanged += HandleCashUpdated;
    }

    private void OnDisable()
    {
        GameRewardManager.OnCashChanged -= HandleCashUpdated;
    }

    private void HandleCashUpdated(int newCash)
    {
        UpdateCashDisplay();
    }

    private void Start()
    {
        SetupAudio();
        SetupButtons();

        // Mặc định chọn Sector cao nhất vừa mở khóa
        SelectSector(Mathf.Clamp(_maxUnlockedSector - 1, 0, sectors.Length - 1));

        UpdateCashDisplay();

        // Mở sẵn War Room Panel nếu người chơi vào scene này để chọn nhiệm vụ
        if (warRoomPanel != null)
        {
            warRoomPanel.SetActive(true);
        }
    }

    private void Update()
    {
        // Phím tắt tiện lợi cho nhà phát triển test mở khóa: bấm U để mở full màn
        if (Input.GetKeyDown(KeyCode.U))
        {
            _maxUnlockedSector = sectors.Length;
            PlayerPrefs.SetInt(PREF_UNLOCKED_SECTOR, _maxUnlockedSector);
            PlayerPrefs.Save();
            RefreshPinsState();
            SelectSector(_selectedSectorIndex);
            Debug.Log("[TacticalWarRoom] Dev cheat: All sectors unlocked!");
        }
    }

    // Khởi tạo thông số chuẩn của 3 Sector nếu chưa có trong Inspector
    private void InitializeDefaultSectors()
    {
        if (sectors != null && sectors.Length == 3) return;

        sectors = new MissionSectorInfo[3];

        // Sector 1: Cao tốc ngoại ô
        sectors[0] = new MissionSectorInfo
        {
            sectorIndex = 1,
            sectorCode = "SECTOR 01",
            sectorName = "HIGHWAY OUTSKIRTS",
            sceneToLoad = "map1",
            threatLevel = 2,
            targetDistance = 160f,
            enemyIntel = "Basic Walkers, Rapid Jumpers, Heavy Chargers",
            bossIntel = "Outskirts Horde (10 Mutants)",
            radioDispatch = "HQ to Survivor: Outskirts highway is blocked with swarms of runners and jumpers. Punch through their blockade, dismount and secure Checkpoint Alpha!",
            objectives = new string[]
            {
                "1. Ram through barricades & reach 160m",
                "2. Dismount vehicle and eliminate 10 Zombies",
                "3. Secure forward outpost & unlock Sector 02"
            },
            rewardText = "+1,500 Cash  |  Armored Truck Access",
            cashReward = 1500
        };

        // Sector 2: Khu ô nhiễm hóa chất
        sectors[1] = new MissionSectorInfo
        {
            sectorIndex = 2,
            sectorCode = "SECTOR 02",
            sectorName = "CONTAMINATED ZONE",
            sceneToLoad = "map2",
            threatLevel = 4,
            targetDistance = 350f,
            enemyIntel = "Acid-Infused Walkers, Aggressive Chargers",
            bossIntel = "MUTATED TANK ZOMBIE (Boulder Thrower)",
            radioDispatch = "Hazard Alert! Toxic chemical spills detected in Sector 2. A mutated Tank Zombie has been sighted hurling concrete boulders. Exercise extreme caution!",
            objectives = new string[]
            {
                "1. Traverse acid highway lanes (350m)",
                "2. Dodge flying concrete boulder strikes",
                "3. Defeat Mutated Tank Zombie on foot"
            },
            rewardText = "+3,500 Cash  |  Heavy Armor Upgrade",
            cashReward = 3500
        };

        // Sector 3: Tâm chấn thảm họa - Đấu Boss Apex Tank
        sectors[2] = new MissionSectorInfo
        {
            sectorIndex = 3,
            sectorCode = "SECTOR 03",
            sectorName = "APEX GROUND ZERO",
            sceneToLoad = "map3",
            threatLevel = 5,
            targetDistance = 550f,
            enemyIntel = "Colossal Outbreak Horde (High Density)",
            bossIntel = "APEX TANK (Ground Slam Shockwave & Turbo Rush)",
            radioDispatch = "Emergency Broadcast! Sector 3 is completely lost to the epidemic. The Apex Tank has overrun downtown. This is our last stand — destroy the monster and save the city!",
            objectives = new string[]
            {
                "1. Breach Ground Zero epicenter (550m)",
                "2. Survive Boss Ground Slam shockwave",
                "3. Terminate APEX TANK and win the game!"
            },
            rewardText = "+8,000 Cash  |  Ultimate Savior Trophy",
            cashReward = 8000
        };
    }

    private void SetupAudio()
    {
        if (musicSource == null)
        {
            musicSource = gameObject.AddComponent<AudioSource>();
        }

        musicSource.loop = true;
        musicSource.playOnAwake = false;
        musicSource.volume = 0.55f;

#if UNITY_EDITOR
        if (lobbyMusicClip == null)
        {
            lobbyMusicClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/sound eff/lobby music.mp3");
        }
#endif

        if (lobbyMusicClip != null && !musicSource.isPlaying)
        {
            musicSource.clip = lobbyMusicClip;
            musicSource.Play();
        }

        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;
        }
    }

    private void SetupButtons()
    {
        if (deployButton != null)
        {
            deployButton.onClick.RemoveAllListeners();
            deployButton.onClick.AddListener(OnDeployClicked);
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(CloseWarRoom);
        }

        if (openWarRoomButton != null)
        {
            openWarRoomButton.onClick.RemoveAllListeners();
            openWarRoomButton.onClick.AddListener(OpenWarRoom);
        }

        if (returnToMenuButton != null)
        {
            returnToMenuButton.onClick.RemoveAllListeners();
            returnToMenuButton.onClick.AddListener(ReturnToMainMenu);
        }

        // Gắn listener cho từng ghim bản đồ
        if (mapPinButtons != null)
        {
            for (int i = 0; i < mapPinButtons.Length; i++)
            {
                int idx = i;
                if (mapPinButtons[i] != null)
                {
                    mapPinButtons[i].onClick.RemoveAllListeners();
                    mapPinButtons[i].onClick.AddListener(() => SelectSector(idx));
                }
            }
        }
    }

    private void LoadProgression()
    {
        _maxUnlockedSector = PlayerPrefs.GetInt(PREF_UNLOCKED_SECTOR, 1);
        _maxUnlockedSector = Mathf.Clamp(_maxUnlockedSector, 1, 3);
    }

    // Đổi hiển thị ghim khi chọn 1 Sector trên bản đồ
    public void SelectSector(int index)
    {
        if (index < 0 || index >= sectors.Length) return;

        _selectedSectorIndex = index;
        MissionSectorInfo sector = sectors[_selectedSectorIndex];

        // 1. Cập nhật Tiêu đề & Thông tin cơ bản (Định dạng chữ chuẩn, êm dịu dễ đọc)
        if (sectorTitleText != null)
        {
            sectorTitleText.text = $"{sector.sectorCode}: {sector.sectorName}";
        }

        if (sectorPreviewImage != null && sector.sectorPreviewSprite != null)
        {
            sectorPreviewImage.sprite = sector.sectorPreviewSprite;
        }

        if (distanceText != null)
        {
            distanceText.text = $"Distance: {sector.targetDistance:0} Meters";
        }

        if (enemyIntelText != null)
        {
            enemyIntelText.text = $"Mutants: {sector.enemyIntel}";
        }

        if (bossIntelText != null)
        {
            bossIntelText.text = $"Boss: {sector.bossIntel}";
        }

        // 2. Mức độ nguy hiểm (Threat Level)
        if (threatLevelText != null)
        {
            string threatRating = sector.threatLevel switch
            {
                1 => "Low (Tier 1)",
                2 => "Moderate (Tier 2)",
                3 => "High (Tier 3)",
                4 => "Dangerous (Tier 4)",
                _ => "Extreme (Tier 5)"
            };
            threatLevelText.text = $"Threat: {threatRating}";
        }

        // 3. Hiệu ứng gõ điện đàm radio (Typewriter Radio Dispatch)
        if (radioTranscriptText != null)
        {
            if (_typewriterRoutine != null) StopCoroutine(_typewriterRoutine);
            _typewriterRoutine = StartCoroutine(TypewriterRoutine(sector.radioDispatch));
        }

        // 4. Danh sách mục tiêu chiến thuật (Objectives)
        if (objectivesText != null)
        {
            string objStr = "Tactical Objectives:\n";
            for (int i = 0; i < sector.objectives.Length; i++)
            {
                objStr += $"- {sector.objectives[i]}\n";
            }
            objectivesText.text = objStr.TrimEnd();
        }

        // 5. Phần thưởng (Rewards)
        if (rewardsText != null)
        {
            rewardsText.text = $"Rewards: {sector.rewardText}";
        }

        // 6. Trạng thái Mở khóa & Nút Xuất kích
        bool isUnlocked = (sector.sectorIndex <= _maxUnlockedSector);
        if (deployButton != null)
        {
            deployButton.interactable = isUnlocked;
        }

        if (deployButtonText != null)
        {
            deployButtonText.text = isUnlocked ? "DEPLOY VEHICLE" : "[ LOCKED ]";
        }

        RefreshPinsState();
    }

    private void RefreshPinsState()
    {
        if (mapPinButtons == null) return;

        for (int i = 0; i < mapPinButtons.Length; i++)
        {
            bool isUnlocked = (i + 1 <= _maxUnlockedSector);
            bool isCleared = (i + 1 < _maxUnlockedSector);
            bool isSelected = (i == _selectedSectorIndex);

            if (pinLockedIcons != null && i < pinLockedIcons.Length && pinLockedIcons[i] != null)
            {
                pinLockedIcons[i].SetActive(!isUnlocked);
            }

            if (pinClearedIcons != null && i < pinClearedIcons.Length && pinClearedIcons[i] != null)
            {
                pinClearedIcons[i].SetActive(isCleared);
            }

            if (pinSelectionGlows != null && i < pinSelectionGlows.Length && pinSelectionGlows[i] != null)
            {
                pinSelectionGlows[i].gameObject.SetActive(false);
            }

            // Đổi màu thanh nhấn AccentBar bên trái thẻ khi chọn
            Transform cardTr = mapPinButtons[i].transform;
            Transform accentTr = cardTr.Find("AccentBar");
            if (accentTr != null)
            {
                Image imgAcc = accentTr.GetComponent<Image>();
                if (imgAcc != null)
                {
                    imgAcc.color = isSelected ? new Color(1f, 0.65f, 0.1f) : new Color(0.3f, 0.35f, 0.42f);
                }
            }

            Transform numTr = cardTr.Find("Number");
            if (numTr != null)
            {
                TextMeshProUGUI numTmp = numTr.GetComponent<TextMeshProUGUI>();
                if (numTmp != null)
                {
                    numTmp.color = isSelected ? new Color(1f, 0.75f, 0.2f) : new Color(0.35f, 0.85f, 1f);
                }
            }
        }
    }

    // Hiệu ứng chữ chạy radio điện đàm
    private IEnumerator TypewriterRoutine(string fullText)
    {
        radioTranscriptText.text = "";
        for (int i = 0; i <= fullText.Length; i++)
        {
            radioTranscriptText.text = fullText.Substring(0, i);
            yield return new WaitForSeconds(0.012f);
        }
        _typewriterRoutine = null;
    }

    // Nút Xuất kích chiến đấu
    public void OnDeployClicked()
    {
        MissionSectorInfo sector = sectors[_selectedSectorIndex];
        if (sector.sectorIndex > _maxUnlockedSector) return;

        StartCoroutine(DeploySequence(sector.sceneToLoad));
    }

    private IEnumerator DeploySequence(string sceneName)
    {
        if (deployButton != null) deployButton.interactable = false;
        if (deployButtonText != null) deployButtonText.text = "⚡ DEPLOYING...";

        // Nhấp nháy xác nhận lệnh xuất kích
        yield return new WaitForSeconds(0.4f);

        if (Application.CanStreamedLevelBeLoaded(sceneName))
        {
            PlayerPrefs.SetString("LastLevel", sceneName);
            PlayerPrefs.Save();
            SceneManager.LoadScene(sceneName);
        }
        else
        {
            Debug.LogError($"[TacticalWarRoom] Scene '{sceneName}' not found in Build Settings!");
            if (deployButton != null) deployButton.interactable = true;
        }
    }

    public void OpenWarRoom()
    {
        if (warRoomPanel != null) warRoomPanel.SetActive(true);
        if (lobbyDefaultHUD != null) lobbyDefaultHUD.SetActive(false);
        SelectSector(_selectedSectorIndex);
    }

    public void CloseWarRoom()
    {
        if (warRoomPanel != null) warRoomPanel.SetActive(false);
        if (lobbyDefaultHUD != null) lobbyDefaultHUD.SetActive(true);
    }

    public void ReturnToMainMenu()
    {
        if (Application.CanStreamedLevelBeLoaded("Main menu"))
        {
            SceneManager.LoadScene("Main menu");
        }
        else
        {
            SceneManager.LoadScene(0);
        }
    }

    private void UpdateCashDisplay()
    {
        int cash = GameRewardManager.Instance != null 
            ? GameRewardManager.Instance.GetCurrentCash() 
            : PlayerPrefs.GetInt(PREF_PLAYER_CASH, 2500);

        if (cashDisplayTopText != null)
        {
            cashDisplayTopText.text = $"FUNDS: ${cash:N0}";
        }
    }

    // Gán ảnh Preview cho 3 Sector từ Editor Builder
    public void SetSectorPreviewSprites(Sprite s1, Sprite s2, Sprite s3)
    {
        InitializeDefaultSectors();
        if (sectors != null && sectors.Length >= 3)
        {
            sectors[0].sectorPreviewSprite = s1;
            sectors[1].sectorPreviewSprite = s2;
            sectors[2].sectorPreviewSprite = s3;
            if (sectorPreviewImage != null && _selectedSectorIndex >= 0 && _selectedSectorIndex < sectors.Length)
            {
                sectorPreviewImage.sprite = sectors[_selectedSectorIndex].sectorPreviewSprite;
            }
        }
    }

    // Hàm gọi khi hoàn thành chặng (gọi từ StageLevelManager hoặc UI Win)
    public static void UnlockNextSector(int completedSectorIndex)
    {
        int currentMax = PlayerPrefs.GetInt(PREF_UNLOCKED_SECTOR, 1);
        if (completedSectorIndex >= currentMax && completedSectorIndex < 3)
        {
            PlayerPrefs.SetInt(PREF_UNLOCKED_SECTOR, completedSectorIndex + 1);
            PlayerPrefs.Save();
            Debug.Log($"[TacticalWarRoom] Sector {completedSectorIndex + 1} unlocked!");
        }
    }
}
