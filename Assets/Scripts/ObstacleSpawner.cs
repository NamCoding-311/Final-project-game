using System.Collections.Generic;
using UnityEngine;

// Tự động sinh chướng ngại vật ngẫu nhiên trên 3 làn đường phía trước xe của Player
public class ObstacleSpawner : MonoBehaviour
{
    [Header("Player Target")]
    [SerializeField] private Transform playerTransform;

    [Header("Obstacle Prefabs")]
    // Danh sách các mẫu chướng ngại vật (Thùng phuy, Rào chắn, Xe hỏng, Cọc tiêu)
    [SerializeField] private GameObject[] obstaclePrefabs;

    [Header("Spawn Settings")]
    // Khoảng thời gian giữa 2 lần sinh chướng ngại vật (giây)
    [SerializeField] private float spawnInterval = 3.5f;

    // Khoảng cách phía trước mũi xe để đặt vật cản (mét)
    [SerializeField] private float spawnDistanceAhead = 30f;

    [Header("Lane Heights (Tọa độ Y của 3 làn đường)")]
    // Độ cao Y tương ứng với 3 làn xe chạy (Top, Middle, Bottom)
    [SerializeField] private float laneTopY = 1.1f;
    [SerializeField] private float laneCenterY = 0.3f;
    [SerializeField] private float laneBottomY = -0.5f;

    private float _nextSpawnTime;

    private void Start()
    {
        if (playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) playerTransform = player.transform;
        }

        _nextSpawnTime = Time.time + spawnInterval;
    }

    private void Update()
    {
        if (playerTransform == null || obstaclePrefabs == null || obstaclePrefabs.Length == 0) return;

        if (Time.time >= _nextSpawnTime)
        {
            SpawnRandomObstacle();
            _nextSpawnTime = Time.time + spawnInterval;
        }
    }

    private void SpawnRandomObstacle()
    {
        // 1. Chọn ngẫu nhiên 1 Prefab chướng ngại vật
        GameObject selectedPrefab = obstaclePrefabs[Random.Range(0, obstaclePrefabs.Length)];
        if (selectedPrefab == null) return;

        // 2. Lấy danh sách làn từ xe của Player (tự động thích ứng dù là 3 làn hay 4 làn)
        float chosenY;
        if (playerTransform != null)
        {
            Player3LaneMovement movement = playerTransform.GetComponent<Player3LaneMovement>();
            if (movement != null)
            {
                float[] allLanes = movement.GetAllLanePositions();
                if (allLanes != null && allLanes.Length > 0)
                {
                    chosenY = allLanes[Random.Range(0, allLanes.Length)];
                    Vector3 dynamicSpawnPos = new Vector3(playerTransform.position.x + spawnDistanceAhead, chosenY, 0f);
                    Instantiate(selectedPrefab, dynamicSpawnPos, Quaternion.identity);
                    return;
                }
            }
        }

        // Dự phòng nếu không tìm thấy Player
        float[] lanes = new float[] { laneTopY, laneCenterY, laneBottomY };
        chosenY = lanes[Random.Range(0, lanes.Length)];

        // 3. Tọa độ sinh phía trước Player
        Vector3 spawnPos = new Vector3(playerTransform != null ? playerTransform.position.x + spawnDistanceAhead : spawnDistanceAhead, chosenY, 0f);

        // 4. Sinh vật thể
        Instantiate(selectedPrefab, spawnPos, Quaternion.identity);
    }
}
