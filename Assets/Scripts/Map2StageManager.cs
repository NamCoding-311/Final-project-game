using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using TMPro;

// Quản lý 3 Chặng cho Map 2 (Bến Tre - Đường Đất Hoàng Hôn):
// - Cấu trúc kế thừa trọn vẹn từ StageLevelManager (chạy y như Map 1).
// - Tập trung tần suất xuất hiện cao của quái dị thường: Hunter (ZombieJumper) và Charger.
// - Chặng 1: Hoàng hôn ấm áp (150m) - Bầy Hunter nhảy bổ từ các hướng vào xe.
// - Chặng 2: Chiều tà đổ bóng (320m) - Xuất hiện Zombie Charger húc cực mạnh trên các làn đường đất.
// - Chặng 3: Đêm tàn tàn tích (500m) - Bầy quái hỗn hợp Charger, Hunter và Zombie chạy trâu máu nhất.
// - Cuối mỗi chặng: Xe tự động tấp lề, OnFootPlayer bước xuống chiến đấu, camera khóa đấu trường,
//   hoàn thành đợt quái thì xe phóng Nitro sang chặng mới.
// - Hoàn thành chặng 3: Hiện UI chiến thắng chuyển sang Map 3.
public class Map2StageManager : StageLevelManager
{
    protected override void Awake()
    {
        base.Awake();

        // Thông điệp chiến thắng riêng của Map 2
        mapVictoryText = "VICTORY!\nYOU SURVIVED MAP 2: SUNSET WASTELAND!";

        // Nếu inspector chưa cấu hình chặng thì khởi tạo cấu hình mặc định cho Map 2
        if (stages == null || stages.Length == 0)
        {
            stages = new LevelStageInfo[]
            {
                new LevelStageInfo
                {
                    stageName = "STAGE 1 : SUNSET DIRT ROAD",
                    stageDistance = 150f,
                    onFootZombieCount = 12,
                    zombieHpMultiplier = 1.0f,
                    spawnInterval = 1.5f,
                    minHorde = 4,
                    maxHorde = 7,
                    lightColor = new Color(1f, 0.75f, 0.5f),
                    lightIntensity = 1.0f,
                    jumperSpawnChance = 0,      // Chặng 1 lái xe: 100% quái cơ bản, chưa có dị thường
                    chargerSpawnChance = 0,     // Chặng 1 lái xe: 100% quái cơ bản, chưa có dị thường
                    onFootSpecialChance = 35,   // Cuối chặng 1 xuống xe: Xuất hiện quái dị thường lần đầu (~3-4 con Hunter / Charger)
                    onFootPromptText = "STAGE 1 CHECKPOINT!\nDEFEND AGAINST FIRST MUTATED STRAINS: 12 INFECTED!"
                },
                new LevelStageInfo
                {
                    stageName = "STAGE 2 : DUSK WASTELAND",
                    stageDistance = 320f,
                    onFootZombieCount = 18,
                    zombieHpMultiplier = 1.6f,
                    spawnInterval = 1.0f,
                    minHorde = 6,
                    maxHorde = 10,
                    lightColor = new Color(0.92f, 0.48f, 0.28f),
                    lightIntensity = 0.85f,
                    jumperSpawnChance = 25,     // Chặng 2 lái xe: Hunter nhảy bổ và Charger húc trên đường
                    chargerSpawnChance = 25,    // Chặng 2 lái xe: Hunter nhảy bổ và Charger húc trên đường
                    onFootSpecialChance = 55,   // Cuối chặng 2 xuống xe: 55% quái dị thường
                    onFootPromptText = "STAGE 2 CHECKPOINT!\nCLEAR 18 MUTATED HUNTERS & CHARGERS!"
                },
                new LevelStageInfo
                {
                    stageName = "STAGE 3 : CRIMSON NIGHTFALL",
                    stageDistance = 500f,
                    onFootZombieCount = 24,
                    zombieHpMultiplier = 2.4f,
                    spawnInterval = 0.8f,
                    minHorde = 8,
                    maxHorde = 12,
                    lightColor = new Color(0.42f, 0.22f, 0.32f),
                    lightIntensity = 0.65f,
                    jumperSpawnChance = 35,     // Chặng 3 lái xe: Tần suất quái dị thường cao nhất
                    chargerSpawnChance = 35,    // Chặng 3 lái xe: Tần suất quái dị thường cao nhất
                    onFootSpecialChance = 75,   // Cuối chặng 3 xuống xe: 75% quái dị thường cấp cao
                    onFootPromptText = "FINAL STAND!\nELIMINATE 24 APEX INFECTED TO SURVIVE MAP 2!"
                }
            };
        }
    }

    protected override void Start()
    {
        base.Start();

        // Tự động tìm kiếm các thành phần của Map 2 nếu inspector chưa gán
        if (stageAnnouncementText == null)
        {
            GameObject txtObj = GameObject.Find("StageAnnouncementText");
            if (txtObj != null)
            {
                stageAnnouncementText = txtObj.GetComponent<TextMeshProUGUI>();
            }
        }

        if (carHealthBar == null)
        {
            carHealthBar = GameObject.Find("HPBar");
        }

        if (onFootHealthBar == null)
        {
            onFootHealthBar = GameObject.Find("OnFootBar");
        }
    }
}
