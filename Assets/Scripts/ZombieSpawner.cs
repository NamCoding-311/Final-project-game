using UnityEngine;

// Tự động sinh Zombie trong bán kính xung quanh Player, LOẠI TRỪ vùng phía sau xe
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

    [Header("Spawn Area (Arc Around Player)")]
    // Khoảng cách tối thiểu để sinh Zombie (tránh sinh ngay sát người chơi)
    [SerializeField] private float minSpawnRadius = 12f;

    // Khoảng cách tối đa để sinh Zombie
    [SerializeField] private float maxSpawnRadius = 24f;

    // Góc mở rộng phía trước mặt xe tính theo độ (-90 đến +90 là nửa bán cầu phía trước)
    // Ví dụ 100 độ sẽ bao quát cả phía trước, chéo trên, chéo dưới, trên và dưới
    [Range(30f, 135f)]
    [SerializeField] private float forwardAngleArc = 105f;

    [Header("Difficulty / Multi-Spawn")]
    // Số lượng Zombie tối đa sinh ra trong 1 đợt
    [SerializeField] private int maxZombiesPerWave = 2;

    // Giới hạn trục Y để Zombie không bị sinh ra quá xa ngoài màn hình
    [SerializeField] private float maxVerticalY = 6f;

    private void Start()
    {
        // Tự động tìm Player nếu chưa được gán
        if (playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) playerTransform = player.transform;
        }

        // Bắt đầu chu kỳ sinh Zombie liên tục
        InvokeRepeating(nameof(SpawnZombiesAroundPlayer), 1f, spawnInterval);
    }

    // Sinh Zombie ngẫu nhiên trong vùng bán nguyệt phía trước/xung quanh xe
    private void SpawnZombiesAroundPlayer()
    {
        if (playerTransform == null || zombiePrefab == null) return;

        int spawnCount = Random.Range(1, maxZombiesPerWave + 1);

        for (int i = 0; i < spawnCount; i++)
        {
            Vector3 spawnPosition = CalculateSpawnPosition();
            Instantiate(zombiePrefab, spawnPosition, Quaternion.identity);
        }
    }

    // Tính toán tọa độ sinh ngẫu nhiên quanh Player (trừ vùng sau xe)
    private Vector3 CalculateSpawnPosition()
    {
        // Chọn một góc ngẫu nhiên từ -forwardAngleArc đến +forwardAngleArc (0 độ là hướng thẳng sang phải)
        float randomAngleDeg = Random.Range(-forwardAngleArc, forwardAngleArc);
        float angleRad = randomAngleDeg * Mathf.Deg2Rad;

        // Chọn bán kính ngẫu nhiên
        float randomDistance = Random.Range(minSpawnRadius, maxSpawnRadius);

        // Tính tọa độ offset từ người chơi
        float offsetX = Mathf.Cos(angleRad) * randomDistance;
        float offsetY = Mathf.Sin(angleRad) * randomDistance;

        Vector3 spawnPos = playerTransform.position + new Vector3(offsetX, offsetY, 0f);

        // Giới hạn trục Y nằm trong phạm vi hiển thị hợp lý của map
        spawnPos.y = Mathf.Clamp(spawnPos.y, -maxVerticalY, maxVerticalY);
        spawnPos.z = 0f;

        return spawnPos;
    }

    // Vẽ vùng sinh Zombie trong Scene view để dễ quan sát và căn chỉnh
    private void OnDrawGizmosSelected()
    {
        if (playerTransform == null) return;

        Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
        Vector3 playerPos = playerTransform.position;

        // Vẽ các tia biên giới hạn vùng sinh
        Vector3 topLimit = playerPos + Quaternion.Euler(0, 0, forwardAngleArc) * Vector3.right * maxSpawnRadius;
        Vector3 bottomLimit = playerPos + Quaternion.Euler(0, 0, -forwardAngleArc) * Vector3.right * maxSpawnRadius;

        Gizmos.DrawLine(playerPos, topLimit);
        Gizmos.DrawLine(playerPos, bottomLimit);
    }
}