using System;
using System.Collections.Generic;
using UnityEngine;

// Hệ thống tự động sinh ngẫu nhiên cảnh quan ven đường (nhà cửa, cây xanh, nhà chờ xe buýt, chậu hoa, biển báo...)
// dọc theo bờ kè vỉa hè phía trên của đường đua khi xe di chuyển tiến về phía trước.
public class RoadsideDecorSpawner : MonoBehaviour
{
    // Cấu hình từng loại vật thể trang trí ven đường
    [Serializable]
    public class DecorItemConfig
    {
        [Tooltip("Tên định danh của loại vật thể")]
        public string decorName = "Decor";

        [Tooltip("Sprite hình ảnh pixel art của vật thể")]
        public Sprite sprite;

        [Tooltip("Prefab tùy chọn nếu muốn dùng prefab phức tạp hơn")]
        public GameObject customPrefab;

        [Tooltip("Kích thước hiển thị (Scale)")]
        public Vector2 scale = new Vector2(3.5f, 3.5f);

        [Tooltip("Độ lệch bù thêm theo trục Y (nếu muốn nâng cao hoặc hạ thấp riêng)")]
        public float yOffset = 0f;

        [Tooltip("Khoảng cách tối thiểu cần chừa ra trước khi đặt vật tiếp theo (tránh đè lên nhau)")]
        public float minSpacingAfter = 3.5f;

        [Tooltip("Tỉ lệ trọng số xuất hiện ngẫu nhiên")]
        [Range(0.1f, 10f)]
        public float spawnWeight = 1f;
    }

    [Header("Player Target (Theo dõi xe người chơi)")]
    [SerializeField] private Transform playerTransform;

    [Header("Roadside Curb Position (Tọa độ lề đường)")]
    // Tọa độ Y của mặt lề đường / bờ kè trên (nơi chân các vật thể chạm đất)
    [SerializeField] private float curbY = 3.25f;

    [Header("Spawn Distance Settings (Khoảng cách sinh & thu hồi)")]
    // Khoảng cách phía trước xe cần luôn sinh sẵn vật thể (mét)
    [SerializeField] private float spawnDistanceAhead = 40f;

    // Khoảng cách phía sau xe mà vật thể sẽ bị thu hồi vào Pool (mét)
    [SerializeField] private float despawnDistanceBehind = 25f;

    [Header("Random Spacing Gap (Khoảng hở ngẫu nhiên giữa 2 vật thể)")]
    // Khoảng trống ngẫu nhiên thêm vào giữa các vật (mét)
    [SerializeField] private float minRandomGap = 1.5f;
    [SerializeField] private float maxRandomGap = 5.0f;

    [Header("Visual Sorting (Thứ tự hiển thị)")]
    [SerializeField] private string sortingLayerName = "Default";
    // Order in layer = 2: nằm trên mặt đường (0), nhưng nằm sau xe (10) và zombie (10-15)
    [SerializeField] private int sortingOrder = 2;

    [Header("Decor Configurations (Danh sách mẫu trang trí)")]
    [SerializeField] private List<DecorItemConfig> decorConfigs = new List<DecorItemConfig>();

    // Quản lý các vật thể đang hiển thị trên Scene
    private readonly List<GameObject> _activeDecorObjects = new List<GameObject>();

    // Bộ nhớ đệm (Object Pool) phân loại theo chỉ số cấu hình để tái sử dụng, không gây giật lag (GC)
    private readonly Dictionary<int, Queue<GameObject>> _decorPool = new Dictionary<int, Queue<GameObject>>();

    // Điểm tọa độ X tiếp theo sẽ đặt vật thể
    private float _nextSpawnX = 0f;

    // Transform gốc để gom gọn các GameObject sinh ra trong Hierarchy
    private Transform _decorContainer;

    private void Awake()
    {
        // Tạo container chứa các vật thể để không làm rối Hierarchy
        GameObject containerGo = new GameObject("RoadsideDecor_Container");
        _decorContainer = containerGo.transform;
        _decorContainer.SetParent(transform);

        // Khởi tạo các hàng đợi Object Pool
        for (int i = 0; i < decorConfigs.Count; i++)
        {
            _decorPool[i] = new Queue<GameObject>();
        }
    }

    private void Start()
    {
        // Tự động tìm Player nếu chưa được kéo vào Inspector
        if (playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) playerTransform = player.transform;
        }

        float startPlayerX = playerTransform != null ? playerTransform.position.x : 0f;

        // Bắt đầu sinh từ phía sau xe một đoạn để khi vào game nhìn thấy lề đường đã có cảnh quan sẵn
        _nextSpawnX = startPlayerX - 10f;

        // Sinh lấp đầy khoảng nhìn ban đầu
        PopulateAhead(startPlayerX);
    }

    private void Update()
    {
        if (playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) playerTransform = player.transform;
            return;
        }

        float playerX = playerTransform.position.x;

        // 1. Sinh thêm vật thể mới khi xe tiến về phía trước
        PopulateAhead(playerX);

        // 2. Thu hồi các vật thể đã bị trôi lại quá xa phía sau xe
        RecycleBehind(playerX);
    }

    // Sinh các vật thể mới cho đến khi đủ tầm nhìn phía trước
    private void PopulateAhead(float playerX)
    {
        if (decorConfigs == null || decorConfigs.Count == 0) return;

        float targetAheadX = playerX + spawnDistanceAhead;

        while (_nextSpawnX < targetAheadX)
        {
            int chosenIndex = PickRandomConfigIndex();
            if (chosenIndex >= 0 && chosenIndex < decorConfigs.Count)
            {
                SpawnDecorItem(chosenIndex, _nextSpawnX);
                DecorItemConfig config = decorConfigs[chosenIndex];

                // Khoảng cách đến vật tiếp theo = bề rộng vật hiện tại + khoảng hở ngẫu nhiên
                float gap = UnityEngine.Random.Range(minRandomGap, maxRandomGap);
                _nextSpawnX += config.minSpacingAfter + gap;
            }
            else
            {
                _nextSpawnX += 4f;
            }
        }
    }

    // Chọn ngẫu nhiên loại vật thể dựa trên trọng số spawnWeight
    private int PickRandomConfigIndex()
    {
        float totalWeight = 0f;
        for (int i = 0; i < decorConfigs.Count; i++)
        {
            totalWeight += Mathf.Max(0.01f, decorConfigs[i].spawnWeight);
        }

        float randomValue = UnityEngine.Random.Range(0f, totalWeight);
        float currentSum = 0f;

        for (int i = 0; i < decorConfigs.Count; i++)
        {
            currentSum += Mathf.Max(0.01f, decorConfigs[i].spawnWeight);
            if (randomValue <= currentSum)
            {
                return i;
            }
        }

        return decorConfigs.Count - 1;
    }

    // Sinh hoặc lấy từ Pool một vật thể trang trí tại vị trí X chỉ định
    private void SpawnDecorItem(int configIndex, float spawnX)
    {
        DecorItemConfig config = decorConfigs[configIndex];
        GameObject itemGo = null;

        // Kiểm tra xem trong Pool đã có sẵn đối tượng nhàn rỗi chưa
        if (_decorPool.TryGetValue(configIndex, out Queue<GameObject> pool) && pool.Count > 0)
        {
            itemGo = pool.Dequeue();
            itemGo.SetActive(true);
        }
        else
        {
            // Nếu chưa có, tạo mới đối tượng
            if (config.customPrefab != null)
            {
                itemGo = Instantiate(config.customPrefab, _decorContainer);
            }
            else
            {
                itemGo = new GameObject(config.decorName);
                itemGo.transform.SetParent(_decorContainer);
                SpriteRenderer sr = itemGo.AddComponent<SpriteRenderer>();
                sr.sprite = config.sprite;
                sr.sortingLayerName = sortingLayerName;
                sr.sortingOrder = sortingOrder;
            }

            // Gắn thẻ đánh dấu index loại vật thể để khi thu hồi biết trả về đúng hàng đợi
            DecorItemTag tag = itemGo.GetComponent<DecorItemTag>();
            if (tag == null) tag = itemGo.AddComponent<DecorItemTag>();
            tag.configIndex = configIndex;
        }

        // Cập nhật tỷ lệ scale
        itemGo.transform.localScale = new Vector3(config.scale.x, config.scale.y, 1f);

        // Tính toán tọa độ Y chính xác để đáy vật thể tiếp đất đúng vị trí curbY
        SpriteRenderer spriteRenderer = itemGo.GetComponent<SpriteRenderer>();
        float halfHeight = 0.5f;
        if (spriteRenderer != null && spriteRenderer.sprite != null)
        {
            halfHeight = spriteRenderer.sprite.bounds.extents.y * config.scale.y;
        }

        float finalY = curbY + halfHeight + config.yOffset;
        itemGo.transform.position = new Vector3(spawnX, finalY, 0f);

        _activeDecorObjects.Add(itemGo);
    }

    // Thu hồi các vật thể đã khuất màn hình phía sau xe vào Object Pool
    private void RecycleBehind(float playerX)
    {
        float despawnThresholdX = playerX - despawnDistanceBehind;

        for (int i = _activeDecorObjects.Count - 1; i >= 0; i--)
        {
            GameObject item = _activeDecorObjects[i];
            if (item == null)
            {
                _activeDecorObjects.RemoveAt(i);
                continue;
            }

            if (item.transform.position.x < despawnThresholdX)
            {
                _activeDecorObjects.RemoveAt(i);
                item.SetActive(false);

                DecorItemTag tag = item.GetComponent<DecorItemTag>();
                int cIndex = (tag != null) ? tag.configIndex : 0;

                if (!_decorPool.ContainsKey(cIndex))
                {
                    _decorPool[cIndex] = new Queue<GameObject>();
                }
                _decorPool[cIndex].Enqueue(item);
            }
        }
    }

    // Hỗ trợ cấu hình nhanh danh sách Decor từ Script hoặc Inspector
    public void SetDecorConfigs(List<DecorItemConfig> newConfigs)
    {
        decorConfigs = newConfigs;
        _decorPool.Clear();
        for (int i = 0; i < decorConfigs.Count; i++)
        {
            _decorPool[i] = new Queue<GameObject>();
        }
    }
}

// Component phụ lưu trữ chỉ số loại cấu hình để tái sử dụng trong Pool
public class DecorItemTag : MonoBehaviour
{
    public int configIndex;
}
