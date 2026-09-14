using UnityEngine;

// Tự động sinh Zombie HỢP LOGIC BỐI CẢNH:
// - CHỈ sinh ở 3 hướng: Phía Trước (Phải), Phía Sau (Trái), và Vỉa Hè (Dưới).
// - TUYỆT ĐỐI KHÔNG sinh ở Phía Trên (Lan can bờ sông / bầu trời).
// - Luôn khóa chặt vị trí nằm trọn trên mặt đường và vỉa hè, không bao giờ bị off-road.
public class ZombieSpawner : MonoBehaviour
{
    [Header("Prefabs & Target")]
    // Prefab Zombie cần sinh
    [SerializeField] private GameObject zombiePrefab;

    // Transform của Player/Xe
    [SerializeField] private Transform playerTransform;

    [Header("Spawn Timing")]
    // Thời gian cách nhau giữa mỗi lần sinh Zombie (giây)
    [SerializeField] private float spawnInterval = 1.2f;

    [Header("Spawn Direction Weights (Tỉ lệ xuất hiện 3 hướng: Trước, Dưới, Sau)")]
    [Range(0, 100)]
    [SerializeField] private int frontSpawnWeight = 60;  // 60% đón đầu phía trước (Bên Phải)
    [Range(0, 100)]
    [SerializeField] private int bottomSpawnWeight = 25; // 25% trồi từ vỉa hè dưới lên (Phía Dưới)
    [Range(0, 100)]
    [SerializeField] private int backSpawnWeight = 15;   // 15% rượt đuổi từ phía sau (Bên Trái)

    [Header("Spawn Distances (Khoảng cách trước và sau xe)")]
    [SerializeField] private float minDistanceAhead = 15f;
    [SerializeField] private float maxDistanceAhead = 25f;
    [SerializeField] private float minDistanceBehind = 8f;
    [SerializeField] private float maxDistanceBehind = 16f;

    [Header("Road Boundaries (Khóa cứng mặt đường - TUYỆT ĐỐI KHÔNG SPAWN LÊN BỜ SÔNG)")]
    // Mép lan can trên cùng (giáp bờ sông) - CHẶN CỨNG không bao giờ sinh cao hơn mốc này
    [SerializeField] private float roadMaxY = 3.0f;

    // Mép làn đường dưới cùng
    [SerializeField] private float roadMinY = -0.8f;

    // Điểm xuất phát ở mép dưới vỉa hè khi trồi lên
    [SerializeField] private float bottomSpawnY = -1.4f;

    [Header("Difficulty / Multi-Spawn")]
    // Số lượng Zombie tối đa sinh ra trong 1 đợt
    [SerializeField] private int maxZombiesPerWave = 2;

    // Trạng thái cho phép sinh Zombie (tạm dừng khi đánh Boss hoặc chuyển cảnh)
    private bool _isSpawningActive = true;
    private Player3LaneMovement _laneMovement;

    private void Start()
    {
        FindPlayer();

        // Bắt đầu chu kỳ sinh Zombie liên tục
        InvokeRepeating(nameof(SpawnZombiesAroundPlayer), 1f, spawnInterval);
    }

    private void Update()
    {
        // Tự động tìm lại Player nếu bị đổi (ví dụ khi bước xuống xe)
        if (playerTransform == null || !playerTransform.gameObject.activeInHierarchy)
        {
            FindPlayer();
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

    // Sinh Zombie ngẫu nhiên theo 3 hướng hợp lệ (Phía Trước, Phía Dưới, Phía Sau)
    private void SpawnZombiesAroundPlayer()
    {
        if (!_isSpawningActive) return;
        if (playerTransform == null || zombiePrefab == null) return;

        int spawnCount = Random.Range(1, maxZombiesPerWave + 1);

        for (int i = 0; i < spawnCount; i++)
        {
            Vector3 spawnPosition = CalculateSpawnPosition();
            Instantiate(zombiePrefab, spawnPosition, Quaternion.identity);
        }
    }

    // Tính toán tọa độ sinh: CHỈ sinh ở Bên Phải, Bên Trái hoặc Phía Dưới; TUYỆT ĐỐI KHÔNG SINH Ở TRÊN BỜ SÔNG
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

    // Vẽ vùng sinh Zombie trong Scene view để bạn dễ quan sát
    private void OnDrawGizmosSelected()
    {
        Vector3 center = playerTransform != null ? playerTransform.position : transform.position;

        // Vùng Phía Trước (Xanh lá cây)
        Gizmos.color = Color.green;
        Vector3 frontCenter = new Vector3(center.x + (minDistanceAhead + maxDistanceAhead) * 0.5f, (roadMinY + roadMaxY) * 0.5f, 0f);
        Vector3 frontSize = new Vector3(maxDistanceAhead - minDistanceAhead, roadMaxY - roadMinY, 0f);
        Gizmos.DrawWireCube(frontCenter, frontSize);

        // Vùng Phía Dưới (Vàng)
        Gizmos.color = Color.yellow;
        Vector3 bottomCenter = new Vector3(center.x + 6f, bottomSpawnY, 0f);
        Vector3 bottomSize = new Vector3(20f, 0.4f, 0f);
        Gizmos.DrawWireCube(bottomCenter, bottomSize);

        // Vùng Phía Sau (Đỏ cam)
        Gizmos.color = new Color(1f, 0.5f, 0f);
        Vector3 backCenter = new Vector3(center.x - (minDistanceBehind + maxDistanceBehind) * 0.5f, (roadMinY + roadMaxY) * 0.5f, 0f);
        Vector3 backSize = new Vector3(maxDistanceBehind - minDistanceBehind, roadMaxY - roadMinY, 0f);
        Gizmos.DrawWireCube(backCenter, backSize);

        // Đường ranh giới lan can bờ sông (Đỏ cảnh báo)
        Gizmos.color = Color.red;
        Gizmos.DrawLine(new Vector3(center.x - 30f, roadMaxY, 0f), new Vector3(center.x + 35f, roadMaxY, 0f));
    }
}