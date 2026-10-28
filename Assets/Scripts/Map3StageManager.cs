using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

// Quản lý 3 Chặng cho Map 3 (Chiến trường quyết đấu Boss):
// - Cấu trúc kế thừa trọn vẹn từ StageLevelManager.
// - Chặng 1: 0m - 160m (Ngoại ô khu công nghiệp) - Lái xe né quái, cuối chặng xuống xe bắn hạ 12 Zombie.
// - Chặng 2: 160m - 350m (Khu ô nhiễm) - Quái xuất hiện dày đặc, cuối chặng đối đầu Trùm TANK ZOMBIE (CHỈ DÙNG CHIÊU NÉM ĐÁ).
// - Chặng 3: 350m - 550m (Tâm chấn tận thế) - Đợt quái khốc liệt nhất, cuối chặng đối đầu TRÙM CUỐI APEX TANK (FULL SKILLS: Ném đá + Húc tốc, x2.2 HP).
// - Hạ gục Apex Tank để phá đảo màn chơi và giành chiến thắng trò chơi!
public class Map3StageManager : StageLevelManager
{
    protected override void Awake()
    {
        base.Awake();

        // Thông điệp chiến thắng chung cuộc của Map 3
        mapVictoryText = "VICTORY!\nYOU DEFEATED THE APEX TANK AND SAVED THE CITY!";

        // Khởi tạo thông số 3 chặng của Map 3 nếu Inspector chưa có dữ liệu
        if (stages == null || stages.Length == 0)
        {
            stages = new LevelStageInfo[]
            {
                new LevelStageInfo
                {
                    stageName = "STAGE 1 : INDUSTRIAL OUTSKIRTS",
                    stageDistance = 160f,
                    onFootZombieCount = 12,
                    zombieHpMultiplier = 1.0f,
                    spawnInterval = 1.6f,
                    minHorde = 4,
                    maxHorde = 7,
                    lightColor = new Color(0.9f, 0.95f, 1f),
                    lightIntensity = 1.0f,
                    jumperSpawnChance = 15,
                    chargerSpawnChance = 15,
                    onFootSpecialChance = 25,
                    onFootPromptText = "STAGE 1 CHECKPOINT!\nCLEAR 12 ZOMBIES TO BREACH THE GATES!",
                    isBossStage = false
                },
                new LevelStageInfo
                {
                    stageName = "STAGE 2 : CONTAMINATED ZONE",
                    stageDistance = 350f,
                    onFootZombieCount = 1,
                    zombieHpMultiplier = 1.6f,
                    spawnInterval = 1.1f,
                    minHorde = 6,
                    maxHorde = 10,
                    lightColor = new Color(0.95f, 0.7f, 0.45f),
                    lightIntensity = 0.85f,
                    jumperSpawnChance = 25,
                    chargerSpawnChance = 25,
                    onFootSpecialChance = 40,
                    isBossStage = true,
                    bossName = "TANK ZOMBIE (STAGE 2 BOSS)",
                    bossAllowRockThrow = true,
                    bossAllowCharge = false, // Chỉ ném đá, không húc
                    bossHpMultiplier = 1.0f
                },
                new LevelStageInfo
                {
                    stageName = "STAGE 3 : APEX GROUND ZERO",
                    stageDistance = 550f,
                    onFootZombieCount = 1,
                    zombieHpMultiplier = 2.4f,
                    spawnInterval = 0.8f,
                    minHorde = 8,
                    maxHorde = 12,
                    lightColor = new Color(0.85f, 0.3f, 0.3f),
                    lightIntensity = 0.75f,
                    jumperSpawnChance = 35,
                    chargerSpawnChance = 35,
                    onFootSpecialChance = 60,
                    isBossStage = true,
                    bossName = "APEX TANK (FINAL BOSS)",
                    bossAllowRockThrow = true,
                    bossAllowCharge = true,  // Full kỹ năng: Ném đá và Húc tốc
                    bossHpMultiplier = 2.2f  // Máu cực trâu (gấp 2.2 lần)
                }
            };
        }
    }

    protected override void Start()
    {
        base.Start();

        // Tự động tìm kiếm các tham chiếu UI của Map 3 nếu Inspector chưa gắn
        if (stageAnnouncementText == null)
        {
            GameObject txtObj = GameObject.Find("StageAnnouncementText");
            if (txtObj != null) stageAnnouncementText = txtObj.GetComponent<TextMeshProUGUI>();
        }

        if (carHealthBar == null) carHealthBar = GameObject.Find("HPBar");
        if (onFootHealthBar == null)
        {
            onFootHealthBar = GameObject.Find("OnFootBar");
            if (onFootHealthBar == null) onFootHealthBar = GameObject.Find("OnFootHPBar");
        }

        if (bossHealthBar == null)
        {
            GameObject bBar = GameObject.Find("BossHealthBar");
            if (bBar != null) bossHealthBar = bBar.GetComponent<Slider>();
        }

        if (bossWarningText == null)
        {
            GameObject bWarn = GameObject.Find("BossWarningBanner");
            if (bWarn != null) bossWarningText = bWarn.GetComponent<TextMeshProUGUI>();
        }

#if UNITY_EDITOR
        if (bossPrefab == null)
        {
            bossPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/PreFab/Zombie Boss.prefab");
        }
#endif
    }
}
