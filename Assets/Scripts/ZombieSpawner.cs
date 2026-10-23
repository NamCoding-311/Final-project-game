using UnityEngine;

// Tự động sinh Zombie hợp bối cảnh đường phố kết hợp cơ chế Bầy quái chốt chặn (Roadblock Hordes):
// 1. Sinh quái lẻ tẻ rải rác đón đầu trên làn đường và vỉa hè.
// 2. Định kỳ kích hoạt Bầy quái (Zombie Horde) dàn hàng ngang chắn các làn đường phía trước từ xa.
// 3. Giúp người chơi có đủ thời gian (4-5 giây) để quan sát, lia chuột xả súng dọn đường trước khi xe chạy tới.
// 4. TUYỆT ĐỐI KHÔNG sinh ở Phía Trên (Lan can bờ sông / bầu trời).
public class ZombieSpawner : MonoBehaviour
{
    [Header("Prefabs & Target")]
    // Prefab Zombie thường cần sinh
    [SerializeField] private GameObject zombiePrefab;

    // Prefab Zombie Jumper (biết phóng vồ vào xe)
    [SerializeField] private GameObject jumperPrefab;

    // Tỉ lệ % xuất hiện Zombie Jumper (còn lại là zombie thường)
    [Range(0, 100)]
    [SerializeField] private int jumperSpawnChance = 25;

    // Transform của Player/Xe
    [SerializeField] private Transform playerTransform;

    [Header("Spawn Timing (Sinh quái lẻ thông thường)")]
    // Thời gian cách nhau giữa mỗi lần sinh Zombie lẻ (giây)
    [SerializeField] private float spawnInterval = 1.8f;

    [Header("Spawn Direction Weights (Tỉ lệ xuất hiện 3 hướng: Trước, Dưới, Sau)")]
    [Range(0, 100)]
    [SerializeField] private int frontSpawnWeight = 65;  // 65% đón đầu phía trước (Bên Phải)
    [Range(0, 100)]
    [SerializeField] private int bottomSpawnWeight = 25; // 25% trồi từ vỉa hè dưới lên (Phía Dưới)
    [Range(0, 100)]
    [SerializeField] private int backSpawnWeight = 10;   // 10% rượt đuổi từ phía sau (Bên Trái)

    [Header("Spawn Distances (Khoảng cách trước và sau xe)")]
    [SerializeField] private float minDistanceAhead = 18f;
    [SerializeField] private float maxDistanceAhead = 26f;
    [SerializeField] private float minDistanceBehind = 8f;
    [SerializeField] private float maxDistanceBehind = 15f;

    [Header("Road Boundaries (Khóa cứng mặt đường - TUYỆT ĐỐI KHÔNG SPAWN LÊN BỜ SÔNG)")]
    // Mép lan can trên cùng (giáp bờ sông) - CHẶN CỨNG không bao giờ sinh cao hơn mốc này
    [SerializeField] private float roadMaxY = 3.0f;

    // Mép làn đường dưới cùng
    [SerializeField] private float roadMinY = -0.8f;

    // Điểm xuất phát ở mép dưới vỉa hè khi trồi lên
    [SerializeField] private float bottomSpawnY = -1.4f;

    [Header("Difficulty / Multi-Spawn")]
    // Số lượng Zombie tối đa sinh ra trong 1 đợt lẻ
    [SerializeField] private int maxZombiesPerWave = 2;

    [Header("Zombie Horde Settings (Cơ chế Bầy quái chốt chặn dàn hàng ngang)")]
    // Bật / tắt cơ chế sinh bầy quái chốt chặn
    [SerializeField] private bool enableHordeWaves = true;

    // Thời gian giãn cách giữa các đợt bầy quái xuất hiện (giây)
    [SerializeField] private float hordeInterval = 8.0f;

    // Số lượng quái tối thiểu và tối đa trong một đợt bầy quái
    [SerializeField] private int minHordeZombies = 4;
    [SerializeField] private int maxHordeZombies = 7;

    // Khoảng cách đón đầu phía trước mặt xe khi bầy quái xuất hiện
    [SerializeField] private float hordeSpawnDistance = 24f;

    // Trạng thái cho phép sinh Zombie (tạm dừng khi đánh Boss hoặc chuyển cảnh)
    private bool _isSpawningActive = true;
    private Player3LaneMovement _laneMovement;
    private float _nextHordeTime;

    private void Start()
    {
        FindPlayer();

        // Đặt mốc thời gian xuất hiện đợt bầy quái đầu tiên
        _nextHordeTime = Time.time + hordeInterval;

        // Bắt đầu chu kỳ sinh Zombie lẻ đều đặn
        InvokeRepeating(nameof(SpawnZombiesAroundPlayer), 1.5f, spawnInterval);
    }

    private void Update()
    {
        // Tự động tìm lại Player nếu bị đổi (ví dụ khi bước xuống xe)
        if (playerTransform == null || !playerTransform.gameObject.activeInHierarchy)
        {
            FindPlayer();
        }

        // Kiểm tra thời điểm kích hoạt Bầy quái chốt chặn (Roadblock Horde)
        if (_isSpawningActive && enableHordeWaves && Time.time >= _nextHordeTime)
        {
            _nextHordeTime = Time.time + hordeInterval;
            SpawnZombieHorde();
        }
    }

    private void FindPlayer()
    {
        OnFootPlayerController onFoot = FindAnyObjectByType<OnFootPlayerController>();
        if (onFoot != null && onFoot.gameObject.activeInHierarchy)
        {
            playerTransform = onFoot.transform;
            _laneMovement = null;
            return;
        }

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
            _laneMovement = player.GetComponent<Player3LaneMovement>();
        }
    }

    // Bật / tắt sinh Zombie (dùng khi vào trận đấu Boss)
    public void SetSpawningActive(bool active)
    {
        _isSpawningActive = active;
    }

    // Sinh Zombie lẻ ngẫu nhiên theo 3 hướng hợp lệ (Phía Trước, Phía Dưới, Phía Sau)
    private void SpawnZombiesAroundPlayer()
    {
        if (!_isSpawningActive) return;
        if (playerTransform == null) return;
        if (zombiePrefab == null && jumperPrefab == null) return;

        int spawnCount = Random.Range(1, maxZombiesPerWave + 1);

        for (int i = 0; i < spawnCount; i++)
        {
            Vector3 spawnPosition = CalculateSpawnPosition();

            // Xác định loại Zombie sẽ sinh (Jumper hay Thường)
            GameObject prefabToSpawn = GetRandomZombiePrefab();

            if (prefabToSpawn != null)
            {
                Instantiate(prefabToSpawn, spawnPosition, Quaternion.identity);
            }
        }
    }

    // Sinh một Bầy Quái (Zombie Horde) dàn hàng ngang chắn các làn đường phía trước từ xa
    private void SpawnZombieHorde()
    {
        if (playerTransform == null) return;

        float baseSpawnX = playerTransform.position.x + hordeSpawnDistance;
        int hordeCount = Random.Range(minHordeZombies, maxHordeZombies + 1);

        // Lấy danh sách tọa độ các làn đường
        float[] lanes = _laneMovement != null ? _laneMovement.GetAllLanePositions() : null;

        for (int i = 0; i < hordeCount; i++)
        {
            // Trải đều quái trên các làn đường để tạo thành bức tường chốt chặn
            float spawnY;
            if (lanes != null && lanes.Length > 0)
            {
                spawnY = lanes[i % lanes.Length];
            }
            else
            {
                spawnY = Mathf.Lerp(roadMinY, roadMaxY, (float)i / Mathf.Max(1, hordeCount - 1));
            }

            // Dãn nhẹ tọa độ X để đàn quái không bị chồng lên nhau
            float offsetX = Random.Range(-1.5f, 2.5f);
            Vector3 spawnPos = new Vector3(baseSpawnX + offsetX, spawnY, 0f);

            GameObject prefabToSpawn = GetRandomZombiePrefab();
            if (prefabToSpawn != null)
            {
                Instantiate(prefabToSpawn, spawnPos, Quaternion.identity);
            }
        }
    }

    // Lựa chọn ngẫu nhiên loại Zombie dựa theo tỉ lệ Jumper
    private GameObject GetRandomZombiePrefab()
    {
        if (jumperPrefab != null && Random.Range(0, 100) < jumperSpawnChance)
        {
            return jumperPrefab;
        }
        return zombiePrefab != null ? zombiePrefab : jumperPrefab;
    }

    // Tính toán tọa độ sinh quái lẻ: CHỈ sinh ở Bên Phải, Bên Trái hoặc Phía Dưới
    private Vector3 CalculateSpawnPosition()
    {
        Vector3 playerPos = playerTransform.position;
        int totalWeight = frontSpawnWeight + bottomSpawnWeight + backSpawnWeight;
        if (totalWeight <= 0) totalWeight = 100;

        int roll = Random.Range(0, totalWeight);

        float spawnX;
        float spawnY;

        if (roll < frontSpawnWeight)
        {
            // 1. Phía trước mặt xe (Bên Phải): Xuất hiện trên các làn đường đón đầu
            spawnX = playerPos.x + Random.Range(minDistanceAhead, maxDistanceAhead);
            spawnY = GetRandomLaneY();
        }
        else if (roll < frontSpawnWeight + bottomSpawnWeight)
        {
            // 2. Phía dưới (Lề đường / Vỉa hè): Trồi từ dưới vỉa hè lên mặt đường
            spawnX = playerPos.x + Random.Range(-4f, 16f);
            spawnY = bottomSpawnY;
        }
        else
        {
            // 3. Phía sau xe (Bên Trái): Xuất hiện trên mặt đường rượt theo
            spawnX = playerPos.x - Random.Range(minDistanceBehind, maxDistanceBehind);
            spawnY = GetRandomLaneY();
        }

        // Khóa chặn tuyệt đối không bao giờ vượt qua lan can bờ sông ở phía trên
        spawnY = Mathf.Min(spawnY, roadMaxY);

        return new Vector3(spawnX, spawnY, 0f);
    }

    // Lấy ngẫu nhiên độ cao một làn đường để Zombie đứng đúng làn
    private float GetRandomLaneY()
    {
        if (_laneMovement != null)
        {
            float[] lanes = _laneMovement.GetAllLanePositions();
            if (lanes != null && lanes.Length > 0)
            {
                return lanes[Random.Range(0, lanes.Length)];
            }
        }
        return Random.Range(roadMinY, roadMaxY);
    }
}
