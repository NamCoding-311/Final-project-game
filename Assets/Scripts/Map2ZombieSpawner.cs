
using System.Collections.Generic;
using UnityEngine;

public class Map2ZombieSpawner : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Map2StageManager stageManager;
    [SerializeField] private Transform playerVehicle;

    [Header("Zombie Prefabs")]
    [SerializeField] private GameObject normalZombiePrefab;
    [SerializeField] private GameObject crawlerPrefab; // ZombieJumper
    [SerializeField] private GameObject chargerPrefab; // ZombieCharger

    [Header("Spawn Position")]
    [SerializeField] private float minDistanceAhead = 16f;
    [SerializeField] private float maxDistanceAhead = 24f;
    [SerializeField] private float roadMinY = -0.8f;
    [SerializeField] private float roadMaxY = 3f;

    [Header("Performance")]
    [SerializeField, Min(1)] private int maxActiveZombies = 18;

    [Header("Stage 1 - Warm Up")]
    [SerializeField] private float stage1SpawnInterval = 2f;
    [SerializeField, Min(0)] private int stage1NormalWeight = 75;
    [SerializeField, Min(0)] private int stage1CrawlerWeight = 20;
    [SerializeField, Min(0)] private int stage1ChargerWeight = 5;

    [Header("Stage 2 - High Pressure")]
    [SerializeField] private float stage2SpawnInterval = 1.1f;
    [SerializeField, Min(0)] private int stage2NormalWeight = 20;
    [SerializeField, Min(0)] private int stage2CrawlerWeight = 45;
    [SerializeField, Min(0)] private int stage2ChargerWeight = 35;

    [Header("Stage 3 - Final Assault")]
    [SerializeField] private float stage3SpawnInterval = 0.7f;
    [SerializeField, Min(0)] private int stage3NormalWeight = 10;
    [SerializeField, Min(0)] private int stage3CrawlerWeight = 45;
    [SerializeField, Min(0)] private int stage3ChargerWeight = 45;

    private readonly List<GameObject> activeZombies =
        new List<GameObject>();

    private Player3LaneMovement laneMovement;
    private float nextSpawnTime;
    private bool spawningActive = true;

    private void Start()
    {
        if (stageManager == null)
            stageManager = FindAnyObjectByType<Map2StageManager>();

        FindPlayer();

        if (stageManager == null)
        {
            Debug.LogError(
                "Map2ZombieSpawner: Chua gan Map2StageManager!",
                this
            );
            enabled = false;
            return;
        }

        if (playerVehicle == null)
        {
            Debug.LogError(
                "Map2ZombieSpawner: Khong tim thay xe Player!",
                this
            );
            enabled = false;
            return;
        }

        nextSpawnTime = Time.time + 1.5f;
    }

    private void Update()
    {
        if (playerVehicle == null ||
            !playerVehicle.gameObject.activeInHierarchy)
        {
            FindPlayer();
        }

        if (!spawningActive || playerVehicle == null ||
            stageManager == null)
            return;

        if (Time.time >= nextSpawnTime)
        {
            SpawnOneZombie();
            nextSpawnTime = Time.time + GetSpawnInterval();
        }
    }

    private void FindPlayer()
    {
        if (playerVehicle == null)
        {
            GameObject player =
                GameObject.FindGameObjectWithTag("Player");

            if (player != null)
                playerVehicle = player.transform;
        }

        if (playerVehicle != null)
            laneMovement =
                playerVehicle.GetComponent<Player3LaneMovement>();
    }

    private float GetSpawnInterval()
    {
        switch (stageManager.CurrentStage)
        {
            case Map2StageManager.Stage.HighPressure:
                return stage2SpawnInterval;

            case Map2StageManager.Stage.FinalAssault:
                return stage3SpawnInterval;

            default:
                return stage1SpawnInterval;
        }
    }

    private void SpawnOneZombie()
    {
        CleanupZombieList();

        if (activeZombies.Count >= maxActiveZombies)
            return;

        GameObject prefab = ChoosePrefab();

        if (prefab == null)
        {
            Debug.LogWarning(
                "Map2ZombieSpawner: Chua gan prefab zombie!",
                this
            );
            return;
        }

        Vector3 position = CalculateSpawnPosition();
        GameObject zombie = Instantiate(
            prefab,
            position,
            Quaternion.identity
        );

        activeZombies.Add(zombie);
    }

    private GameObject ChoosePrefab()
    {
        int normalWeight;
        int crawlerWeight;
        int chargerWeight;

        switch (stageManager.CurrentStage)
        {
            case Map2StageManager.Stage.HighPressure:
                normalWeight = stage2NormalWeight;
                crawlerWeight = stage2CrawlerWeight;
                chargerWeight = stage2ChargerWeight;
                break;

            case Map2StageManager.Stage.FinalAssault:
                normalWeight = stage3NormalWeight;
                crawlerWeight = stage3CrawlerWeight;
                chargerWeight = stage3ChargerWeight;
                break;

            default:
                normalWeight = stage1NormalWeight;
                crawlerWeight = stage1CrawlerWeight;
                chargerWeight = stage1ChargerWeight;
                break;
        }

        int total = normalWeight + crawlerWeight + chargerWeight;

        if (total <= 0)
            return normalZombiePrefab != null
                ? normalZombiePrefab
                : crawlerPrefab != null
                    ? crawlerPrefab
                    : chargerPrefab;

        int roll = Random.Range(0, total);

        if (roll < normalWeight)
            return normalZombiePrefab;

        roll -= normalWeight;

        if (roll < crawlerWeight)
            return crawlerPrefab;

        return chargerPrefab;
    }

    private Vector3 CalculateSpawnPosition()
    {
        float x = playerVehicle.position.x +
                  Random.Range(minDistanceAhead, maxDistanceAhead);

        float y;

        if (laneMovement != null)
        {
            float[] lanes = laneMovement.GetAllLanePositions();

            if (lanes != null && lanes.Length > 0)
                y = lanes[Random.Range(0, lanes.Length)];
            else
                y = Random.Range(roadMinY, roadMaxY);
        }
        else
        {
            y = Random.Range(roadMinY, roadMaxY);
        }

        y = Mathf.Clamp(y, roadMinY, roadMaxY);

        return new Vector3(x, y, 0f);
    }

    private void CleanupZombieList()
    {
        for (int i = activeZombies.Count - 1; i >= 0; i--)
        {
            if (activeZombies[i] == null)
                activeZombies.RemoveAt(i);
        }
    }

    public void SetSpawningActive(bool active)
    {
        spawningActive = active;
    }
}
