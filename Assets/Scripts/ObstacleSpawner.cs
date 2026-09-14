using System.Collections.Generic;
using UnityEngine;

// Tự động sinh chướng ngại vật ngẫu nhiên trên các làn đường phía trước xe của Player
public class ObstacleSpawner : MonoBehaviour
{
    [Header("Player Target")]
    [SerializeField] private Transform playerTransform;

    [Header("Obstacle Prefabs")]
    // Danh sách các mẫu chướng ngại vật (Thùng dầu, Rào chắn bê tông, Rào nhựa, Xác xe cháy, Bao cát...)
    [SerializeField] private GameObject[] obstaclePrefabs;

    [Header("Spawn Interval Settings (Khoảng thời gian ngẫu nhiên)")]
    // Khoảng thời gian tối thiểu giữa 2 lần sinh vật cản (giây)
    [SerializeField] private float minSpawnInterval = 2.5f;

    // Khoảng thời gian tối đa giữa 2 lần sinh vật cản (giây)
    [SerializeField] private float maxSpawnInterval = 4.5f;

    [Header("Spawn Position Settings")]
    // Khoảng cách phía trước mũi xe để đặt vật cản (mét)
    [SerializeField] private float spawnDistanceAhead = 35f;

    [Header("Difficulty / Multi-Lane Obstacles")]
    // Tỉ lệ (0.0 đến 1.0) xuất hiện 2 vật cản cùng lúc ở 2 làn khác nhau để tăng độ khó (vẫn luôn chừa ít nhất 1-2 làn trống)
    [Range(0f, 1f)]
    [SerializeField] private float chanceOfDoubleObstacle = 0.35f;

    [Header("State")]
    [SerializeField] private bool isSpawningEnabled = true;

    private float _nextSpawnTime;
    private Player3LaneMovement _playerMovement;

    private void Start()
    {
        if (playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) playerTransform = player.transform;
        }

        if (playerTransform != null)
        {
            _playerMovement = playerTransform.GetComponent<Player3LaneMovement>();
        }

        ScheduleNextSpawn();
    }

    private void Update()
    {
        if (!isSpawningEnabled) return;
        if (obstaclePrefabs == null || obstaclePrefabs.Length == 0) return;

        // Nếu xe chưa được gán, thử tìm lại
        if (playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerTransform = player.transform;
                _playerMovement = playerTransform.GetComponent<Player3LaneMovement>();
            }
            return;
        }

        // Nếu xe đang dừng hẳn (ví dụ đang đỗ lề đường đánh Boss), không sinh thêm chướng ngại vật
        if (_playerMovement != null && _playerMovement.GetCurrentSpeed() <= 0.1f)
        {
            return;
        }

        if (Time.time >= _nextSpawnTime)
        {
            SpawnRandomObstacles();
            ScheduleNextSpawn();
        }
    }

    private void ScheduleNextSpawn()
    {
        float interval = Random.Range(minSpawnInterval, maxSpawnInterval);
        _nextSpawnTime = Time.time + interval;
    }

    // Sinh chướng ngại vật ngẫu nhiên trên các làn đường
    private void SpawnRandomObstacles()
    {
        if (obstaclePrefabs == null || obstaclePrefabs.Length == 0) return;

        // Lấy danh sách làn hiện có từ xe (thích ứng động theo cấu hình làn)
        float[] allLanes = null;
        if (_playerMovement != null)
        {
            allLanes = _playerMovement.GetAllLanePositions();
        }

        if (allLanes == null || allLanes.Length == 0)
        {
            // Dự phòng mặc định 3 làn
            allLanes = new float[] { 1.47f, 0.53f, -0.42f };
        }

        float spawnX = playerTransform.position.x + spawnDistanceAhead;

        // Trộn ngẫu nhiên danh sách các chỉ số làn (Fisher-Yates shuffle)
        List<int> laneIndices = new List<int>();
        for (int i = 0; i < allLanes.Length; i++) laneIndices.Add(i);
        for (int i = 0; i < laneIndices.Count; i++)
        {
            int temp = laneIndices[i];
            int randomIndex = Random.Range(i, laneIndices.Count);
            laneIndices[i] = laneIndices[randomIndex];
            laneIndices[randomIndex] = temp;
        }

        // Quyết định số lượng vật cản lần này (1 vật cản hoặc 2 vật cản)
        // Luôn đảm bảo số vật cản < tổng số làn để luôn có ít nhất 1-2 làn trống để né
        int countToSpawn = 1;
        if (allLanes.Length >= 3 && Random.value < chanceOfDoubleObstacle)
        {
            countToSpawn = Mathf.Min(2, allLanes.Length - 1);
        }

        for (int i = 0; i < countToSpawn; i++)
        {
            int chosenLaneIdx = laneIndices[i];
            float chosenY = allLanes[chosenLaneIdx];

            GameObject prefab = obstaclePrefabs[Random.Range(0, obstaclePrefabs.Length)];
            if (prefab != null)
            {
                // Thêm một chút lệch ngẫu nhiên nhẹ theo trục X để 2 vật cản không nằm thẳng tắp một hàng cứng nhắc
                float xOffset = (i > 0) ? Random.Range(2f, 5f) : 0f;
                Vector3 spawnPos = new Vector3(spawnX + xOffset, chosenY, 0f);
                Instantiate(prefab, spawnPos, Quaternion.identity);
            }
        }
    }

    // Bật / tắt cơ chế sinh vật cản từ bên ngoài (ví dụ StageLevelManager gọi khi vào Boss)
    public void SetSpawningActive(bool active)
    {
        isSpawningEnabled = active;
        if (active)
        {
            ScheduleNextSpawn();
        }
    }
}
