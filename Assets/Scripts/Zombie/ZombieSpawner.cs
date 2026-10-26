using System.Collections.Generic;
using UnityEngine;

// Tự động sinh Zombie hợp bối cảnh đường phố kết hợp cơ chế Bầy quái chốt chặn (Roadblock Hordes):
// 1. Sinh quái lẻ tẻ rải rác đón đầu trên làn đường và vỉa hè.
// 2. Định kỳ kích hoạt Bầy quái (Zombie Horde) dàn hàng ngang chắn các làn đường phía trước từ xa.
// 3. Tự động điều chỉnh độ khó, số lượng quái và chỉ số máu theo từng Chặng (Stage 1 -> 2 -> 3).
// 4. Hỗ trợ sinh đợt Zombie bao vây khi người chơi xuống xe ở cuối mỗi chặng.
public class ZombieSpawner : MonoBehaviour
{
    [Header("Master Switch (Công tắc bật / tắt tổng)")]
    // Bỏ tick ô này hoặc tắt Component / GameObject thì Zombie sẽ NGỪNG SINH 100% NGAY LẬP TỨC
    [SerializeField] private bool isSpawningEnabled = true;

    [Header("Prefabs & Target")]
    [SerializeField] private GameObject zombiePrefab;
    [SerializeField] private GameObject jumperPrefab;
    [SerializeField] private GameObject chargerPrefab;

    [Range(0, 100)]
    [SerializeField] private int jumperSpawnChance = 20;

    [Range(0, 100)]
    [SerializeField] private int chargerSpawnChance = 0;

    [SerializeField] private Transform playerTransform;

    [Header("Spawn Timing (Sinh quái lẻ thông thường)")]
    [SerializeField] private float spawnInterval = 1.8f;

    [Header("Spawn Direction Weights (Tỉ lệ xuất hiện 3 hướng: Trước, Dưới, Sau)")]
    [Range(0, 100)]
    [SerializeField] private int frontSpawnWeight = 65;
    [Range(0, 100)]
    [SerializeField] private int bottomSpawnWeight = 25;
    [Range(0, 100)]
    [SerializeField] private int backSpawnWeight = 10;

    [Header("Spawn Distances (Khoảng cách trước và sau xe)")]
    [SerializeField] private float minDistanceAhead = 18f;
    [SerializeField] private float maxDistanceAhead = 26f;
    [SerializeField] private float minDistanceBehind = 8f;
    [SerializeField] private float maxDistanceBehind = 15f;

    [Header("Road Boundaries (Khóa cứng mặt đường - Không sinh lên bờ sông)")]
    [SerializeField] private float roadMaxY = 3.0f;
    [SerializeField] private float roadMinY = -0.8f;
    [SerializeField] private float bottomSpawnY = -1.4f;

    [Header("Difficulty / Multi-Spawn")]
    [SerializeField] private int maxZombiesPerWave = 2;

    [Header("Zombie Horde Settings (Bầy quái chốt chặn dàn hàng ngang)")]
    [SerializeField] private bool enableHordeWaves = true;
    [SerializeField] private float hordeInterval = 8.0f;
    [SerializeField] private int minHordeZombies = 4;
    [SerializeField] private int maxHordeZombies = 7;
    [SerializeField] private float hordeSpawnDistance = 24f;

    // Quản lý độ khó và hệ số máu theo chặng
    private bool _isSpawningActive = true;
    private Player3LaneMovement _laneMovement;
    private float _nextHordeTime;
    private float _currentHpMultiplier = 1.0f;
    private float _baseSpawnInterval = 1.8f;

    private void OnEnable()
    {
        if (isSpawningEnabled && _isSpawningActive)
        {
            CancelInvoke(nameof(SpawnZombiesAroundPlayer));
            InvokeRepeating(nameof(SpawnZombiesAroundPlayer), 1.5f, spawnInterval);
        }
    }

    private void OnDisable()
    {
        // Khi bị tắt hoặc bỏ tick component, hủy toàn bộ lệnh gọi tuần hoàn
        CancelInvoke(nameof(SpawnZombiesAroundPlayer));
    }

    private void Start()
    {
        FindPlayer();
        _baseSpawnInterval = spawnInterval;
        _nextHordeTime = Time.time + hordeInterval;

        if (isSpawningEnabled && _isSpawningActive)
        {
            CancelInvoke(nameof(SpawnZombiesAroundPlayer));
            InvokeRepeating(nameof(SpawnZombiesAroundPlayer), 1.5f, spawnInterval);
        }
    }

    private void Update()
    {
        // Nếu bị tắt thì dừng hoàn toàn
        if (!isSpawningEnabled || !_isSpawningActive || !enabled) return;

        if (playerTransform == null || !playerTransform.gameObject.activeInHierarchy)
        {
            FindPlayer();
        }

        if (enableHordeWaves && Time.time >= _nextHordeTime)
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

    // Bật / tắt sinh Zombie từ bên ngoài (StageLevelManager gọi)
    public void SetSpawningActive(bool active)
    {
        _isSpawningActive = active;
        if (!active)
        {
            CancelInvoke(nameof(SpawnZombiesAroundPlayer));
        }
        else if (enabled && gameObject.activeInHierarchy && isSpawningEnabled)
        {
            CancelInvoke(nameof(SpawnZombiesAroundPlayer));
            InvokeRepeating(nameof(SpawnZombiesAroundPlayer), 1.0f, spawnInterval);
            _nextHordeTime = Time.time + hordeInterval;
        }
    }

    // Cập nhật độ khó theo từng chặng (StageLevelManager gọi)
    public void SetStageDifficulty(int stageIndex, float hpMultiplier, float interval, int minHorde, int maxHorde)
    {
        _currentHpMultiplier = hpMultiplier;
        _baseSpawnInterval = interval;
        spawnInterval = interval;
        minHordeZombies = minHorde;
        maxHordeZombies = maxHorde;

        CancelInvoke(nameof(SpawnZombiesAroundPlayer));
        if (enabled && gameObject.activeInHierarchy && isSpawningEnabled && _isSpawningActive)
        {
            InvokeRepeating(nameof(SpawnZombiesAroundPlayer), 0.5f, spawnInterval);
        }
    }

    // Cập nhật tỉ lệ xuất hiện quái dị thường (Hunter / Charger) theo chặng
    public void SetSpecialSpawnChances(int jumperChance, int chargerChance)
    {
        jumperSpawnChance = Mathf.Clamp(jumperChance, 0, 100);
        chargerSpawnChance = Mathf.Clamp(chargerChance, 0, 100);
    }

    // Tăng tốc sinh quái khi gần đến cuối chặng (tạo cao trào)
    public void SetClimaxSpawnRate(float rushMultiplier)
    {
        if (!enabled || !gameObject.activeInHierarchy || !isSpawningEnabled || !_isSpawningActive) return;

        float rushInterval = Mathf.Max(0.5f, _baseSpawnInterval * rushMultiplier);
        if (Mathf.Abs(spawnInterval - rushInterval) > 0.1f)
        {
            spawnInterval = rushInterval;
            CancelInvoke(nameof(SpawnZombiesAroundPlayer));
            InvokeRepeating(nameof(SpawnZombiesAroundPlayer), 0.3f, spawnInterval);
        }
    }

    // Sinh Zombie lẻ ngẫu nhiên theo 3 hướng
    private void SpawnZombiesAroundPlayer()
    {
        // Khóa chặn: Nếu tắt thì TUYỆT ĐỐI không sinh thêm bất kỳ con nào
        if (!enabled || !gameObject.activeInHierarchy || !isSpawningEnabled || !_isSpawningActive) return;
        if (playerTransform == null) return;
        if (zombiePrefab == null && jumperPrefab == null) return;

        int spawnCount = Random.Range(1, maxZombiesPerWave + 1);

        for (int i = 0; i < spawnCount; i++)
        {
            Vector3 spawnPosition = CalculateSpawnPosition();
            GameObject prefabToSpawn = GetRandomZombiePrefab();

            if (prefabToSpawn != null)
            {
                GameObject spawned = Instantiate(prefabToSpawn, spawnPosition, Quaternion.identity);
                ApplyZombieScaling(spawned);
            }
        }
    }

    // Sinh Bầy Quái chốt chặn dàn hàng ngang
    private void SpawnZombieHorde()
    {
        // Khóa chặn
        if (!enabled || !gameObject.activeInHierarchy || !isSpawningEnabled || !_isSpawningActive) return;
        if (playerTransform == null) return;

        float baseSpawnX = playerTransform.position.x + hordeSpawnDistance;
        int hordeCount = Random.Range(minHordeZombies, maxHordeZombies + 1);
        float[] lanes = _laneMovement != null ? _laneMovement.GetAllLanePositions() : null;

        for (int i = 0; i < hordeCount; i++)
        {
            float spawnY;
            if (lanes != null && lanes.Length > 0)
            {
                spawnY = lanes[i % lanes.Length];
            }
            else
            {
                spawnY = Mathf.Lerp(roadMinY, roadMaxY, (float)i / Mathf.Max(1, hordeCount - 1));
            }

            float offsetX = Random.Range(-1.5f, 2.5f);
            Vector3 spawnPos = new Vector3(baseSpawnX + offsetX, spawnY, 0f);

            GameObject prefabToSpawn = GetRandomZombiePrefab();
            if (prefabToSpawn != null)
            {
                GameObject spawned = Instantiate(prefabToSpawn, spawnPos, Quaternion.identity);
                ApplyZombieScaling(spawned);
            }
        }
    }

    // Sinh một đợt Zombie bao vây khi người chơi xuống xe ở cuối chặng
    public List<Zombie> SpawnOnFootWave(Transform target, int totalCount, float leftX, float rightX, float bottomY, float topY, float hpMultiplier, int waveSpecialChance = -1)
    {
        List<Zombie> waveZombies = new List<Zombie>();

        // Nếu Spawner bị người chơi chủ động tắt hoàn toàn thì không ép sinh wave
        if (!isSpawningEnabled) return waveZombies;

        for (int i = 0; i < totalCount; i++)
        {
            Vector3 spawnPos;
            if (Random.value < 0.7f)
            {
                float spawnX = rightX + Random.Range(3f, 12f);
                float spawnY = Random.Range(bottomY, topY);
                spawnPos = new Vector3(spawnX, spawnY, 0f);
            }
            else
            {
                float spawnX = Random.Range(leftX + 2f, rightX - 2f);
                float spawnY = bottomY - Random.Range(1.0f, 2.5f);
                spawnPos = new Vector3(spawnX, spawnY, 0f);
            }

            GameObject prefab = GetZombiePrefabForWave(waveSpecialChance);
            if (prefab != null)
            {
                GameObject zGo = Instantiate(prefab, spawnPos, Quaternion.identity);
                Zombie z = zGo.GetComponent<Zombie>();
                if (z != null)
                {
                    z.SetTarget(target);
                    z.SetClampToRoad(true);
                    z.SetRoadBoundaries(bottomY, topY);
                    if (hpMultiplier > 1.01f)
                    {
                        z.ScaleStats(hpMultiplier, 1f, 1f);
                    }
                    waveZombies.Add(z);
                }
                else
                {
                    // Ho tro ca Charger va Jumper khi sinh dot quai di bo
                    ZombieCharger charger = zGo.GetComponent<ZombieCharger>();
                    if (charger != null)
                    {
                        charger.SetTarget(target);
                        if (hpMultiplier > 1.01f)
                        {
                            charger.ScaleStats(hpMultiplier, 1f, 1f);
                        }
                    }

                    ZombieJumper jumper = zGo.GetComponent<ZombieJumper>();
                    if (jumper != null)
                    {
                        jumper.SetTarget(target);
                        if (hpMultiplier > 1.01f)
                        {
                            jumper.ScaleStats(hpMultiplier, 1f, 1f);
                        }
                    }
                }
            }
        }

        return waveZombies;
    }

    // Lựa chọn prefab Zombie cho đợt sóng On-Foot cuối chặng
    private GameObject GetZombiePrefabForWave(int waveSpecialChance)
    {
        // Nếu có chỉ định riêng tỉ lệ quái dị thường cho đợt phòng thủ đi bộ
        if (waveSpecialChance >= 0)
        {
            int roll = Random.Range(0, 100);
            if (roll < waveSpecialChance)
            {
                // Chọn ngẫu nhiên giữa Charger và Jumper nếu cả hai đều có sẵn
                if (chargerPrefab != null && jumperPrefab != null)
                {
                    return Random.value < 0.5f ? chargerPrefab : jumperPrefab;
                }
                if (chargerPrefab != null) return chargerPrefab;
                if (jumperPrefab != null) return jumperPrefab;
            }
            return zombiePrefab != null ? zombiePrefab : (jumperPrefab != null ? jumperPrefab : chargerPrefab);
        }

        return GetRandomZombiePrefab();
    }

    private void ApplyZombieScaling(GameObject obj)
    {
        Zombie z = obj.GetComponent<Zombie>();
        if (z != null)
        {
            // Khoa dung pham vi mat duong cua Map hien tai
            z.SetRoadBoundaries(roadMinY, roadMaxY);

            if (_currentHpMultiplier > 1.01f)
            {
                z.ScaleStats(_currentHpMultiplier, 1f, 1f);
            }
        }

        ZombieCharger c = obj.GetComponent<ZombieCharger>();
        if (c != null && _currentHpMultiplier > 1.01f)
        {
            c.ScaleStats(_currentHpMultiplier, 1f, 1f);
        }

        ZombieJumper j = obj.GetComponent<ZombieJumper>();
        if (j != null && _currentHpMultiplier > 1.01f)
        {
            j.ScaleStats(_currentHpMultiplier, 1f, 1f);
        }
    }

    private GameObject GetRandomZombiePrefab()
    {
        int roll = Random.Range(0, 100);

        if (chargerPrefab != null && roll < chargerSpawnChance)
        {
            return chargerPrefab;
        }

        if (jumperPrefab != null && roll < (chargerSpawnChance + jumperSpawnChance))
        {
            return jumperPrefab;
        }

        return zombiePrefab != null ? zombiePrefab : (jumperPrefab != null ? jumperPrefab : chargerPrefab);
    }

    private Vector3 CalculateSpawnPosition()
    {
        Vector3 playerPos = playerTransform.position;
        int totalWeight = frontSpawnWeight + bottomSpawnWeight + backSpawnWeight;
        if (totalWeight <= 0) totalWeight = 100;

        int roll = Random.Range(0, totalWeight);

        if (roll < frontSpawnWeight)
        {
            float spawnX = playerPos.x + Random.Range(minDistanceAhead, maxDistanceAhead);
            float spawnY = Random.Range(roadMinY, roadMaxY);
            return new Vector3(spawnX, spawnY, 0f);
        }
        else if (roll < frontSpawnWeight + bottomSpawnWeight)
        {
            // Don dau tu le duong phia truoc xe
            float spawnX = playerPos.x + Random.Range(minDistanceAhead * 0.4f, minDistanceAhead * 0.9f);
            return new Vector3(spawnX, bottomSpawnY, 0f);
        }
        else
        {
            float spawnX = playerPos.x - Random.Range(minDistanceBehind, maxDistanceBehind);
            // Neu o dau man choi thi khong sinh ra ngoai mep ban do ma sinh don dau phia truoc
            if (spawnX < 0f)
            {
                spawnX = playerPos.x + Random.Range(minDistanceAhead, maxDistanceAhead);
            }
            float spawnY = Random.Range(roadMinY, roadMaxY);
            return new Vector3(spawnX, spawnY, 0f);
        }
    }

    public void SetPlayer(Transform player)
    {
        playerTransform = player;
    }

    public bool IsSpawningEnabled() => isSpawningEnabled;
    public float GetRoadMinY() => roadMinY;
    public float GetRoadMaxY() => roadMaxY;
    public int GetJumperSpawnChance() => jumperSpawnChance;
    public int GetChargerSpawnChance() => chargerSpawnChance;
    public void SetSpawningEnabled(bool enabledState)
    {
        isSpawningEnabled = enabledState;
        if (!isSpawningEnabled)
        {
            CancelInvoke(nameof(SpawnZombiesAroundPlayer));
        }
    }
}
