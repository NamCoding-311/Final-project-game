using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

// Component phụ gắn vào từng Chunk để đánh dấu loại Prefab khi thu hồi vào Object Pool
public class ChunkIdentifier : MonoBehaviour
{
    public int PrefabIndex { get; set; }
}

// Quản lý hệ thống bản đồ vô tận dạng cuộn ngang (Horizontal Endless Map / 3-Lane Runner).
// Tự động nhận diện độ cao Y của đường trong Scene và duy trì đường sinh vô tận thẳng tắp.
public class EndlessMapManager : MonoBehaviour
{
    [Header("Target & Prefabs")]
    // Transform của Player hoặc Xe cần theo dõi
    [SerializeField] private Transform playerTransform;

    // Danh sách các mẫu đoạn đường (Tilemap Chunk Prefabs)
    [SerializeField] private GameObject[] chunkPrefabs;

    [Header("Chunk Configuration")]
    // Chiều rộng (trục X) của một đoạn đường (đơn vị Unity)
    [SerializeField] private float chunkWidth = 50f;

    // Tọa độ Y của mặt đường (Tự động cập nhật theo đoạn đường bạn đặt trong Scene)
    [SerializeField] private float roadPosY = 2.5f;

    // Tầm nhìn phía trước cần luôn có sẵn đường (mét)
    [SerializeField] private float spawnDistanceAhead = 100f;

    // Khoảng cách phía sau Player mà đoạn đường cũ sẽ bị thu hồi (mét)
    [SerializeField] private float despawnDistanceBehind = 50f;

    [Header("Optimization")]
    // Bật chế độ Object Pooling để tái sử dụng Chunk
    [SerializeField] private bool useObjectPooling = true;

    // Danh sách các chunk đang hiển thị trên Scene
    private readonly List<GameObject> _activeChunks = new List<GameObject>();

    // Bộ nhớ đệm (Pool) lưu trữ các chunk đã ẩn để tái sử dụng
    private readonly Dictionary<int, Queue<GameObject>> _chunkPool = new Dictionary<int, Queue<GameObject>>();

    // Danh sách các Prefab hợp lệ đã lọc bỏ Missing
    private readonly List<GameObject> _validPrefabs = new List<GameObject>();

    // Vị trí X tiếp theo để đặt đoạn đường mới
    private float _nextSpawnX = 0f;

    private void Start()
    {
        // 1. Tự động tìm Player nếu chưa được gán trong Inspector
        if (playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) playerTransform = player.transform;
        }

        // 2. Lọc danh sách Chunk Prefabs hợp lệ
        _validPrefabs.Clear();
        if (chunkPrefabs != null)
        {
            foreach (GameObject prefab in chunkPrefabs)
            {
                if (prefab != null) _validPrefabs.Add(prefab);
            }
        }

        // 3. Khởi tạo và tự động nhận diện độ cao Y từ Scene
        InitializeMap();
    }

    private void Update()
    {
        if (playerTransform == null) return;

        // 1. Luôn đảm bảo phía trước Player có đủ đường nối tiếp nhau
        while (_nextSpawnX < playerTransform.position.x + spawnDistanceAhead)
        {
            SpawnChunk(false);
        }

        // 2. Thu hồi các đoạn đường cũ đã bị xe bỏ lại quá xa phía sau
        for (int i = _activeChunks.Count - 1; i >= 0; i--)
        {
            GameObject chunk = _activeChunks[i];
            if (chunk != null)
            {
                float chunkRightEdge = chunk.transform.position.x + chunkWidth;

                if (playerTransform.position.x - chunkRightEdge > despawnDistanceBehind)
                {
                    RecycleChunk(chunk);
                    _activeChunks.RemoveAt(i);
                }
            }
        }
    }

    // Khởi tạo bản đồ: Tự động tìm tất cả các đoạn đường đang có trong Scene để lấy chuẩn độ cao Y và vị trí X
    private void InitializeMap()
    {
        _activeChunks.Clear();
        _nextSpawnX = 0f;

        // Tìm tất cả các đoạn đường có sẵn trong Scene (kể cả là con của GameManagers hay nằm ngoài Scene)
        GameObject[] sceneChunks = GameObject.FindObjectsByType<GameObject>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        List<GameObject> existingRoads = new List<GameObject>();

        foreach (GameObject go in sceneChunks)
        {
            if (go.name.Contains("Chunk") || go.name.Contains("Road") || go.name.Contains("Preview"))
            {
                // Đảm bảo đối tượng có chứa Grid hoặc Tilemap
                if (go.GetComponent<Grid>() != null || go.GetComponentInChildren<Tilemap>() != null)
                {
                    existingRoads.Add(go);
                }
            }
        }

        if (existingRoads.Count > 0)
        {
            // Sắp xếp các đoạn đường từ trái sang phải theo trục X
            existingRoads.Sort((a, b) => a.transform.position.x.CompareTo(b.transform.position.x));

            // Lấy độ cao Y chính xác từ đoạn đường bạn đã căn chỉnh
            roadPosY = existingRoads[0].transform.position.y;

            // Đo chiều rộng Tilemap
            Tilemap tm = existingRoads[0].GetComponentInChildren<Tilemap>();
            if (tm != null)
            {
                tm.CompressBounds();
                if (tm.cellBounds.size.x > 0)
                {
                    chunkWidth = tm.cellBounds.size.x;
                }
            }

            foreach (GameObject road in existingRoads)
            {
                _activeChunks.Add(road);

                ChunkIdentifier id = road.GetComponent<ChunkIdentifier>();
                if (id == null) id = road.AddComponent<ChunkIdentifier>();
                id.PrefabIndex = 0;

                float endX = road.transform.position.x + chunkWidth;
                if (endX > _nextSpawnX)
                {
                    _nextSpawnX = endX;
                }
            }
        }

        // Sinh tiếp các đoạn đường đón đầu phía trước
        float initialTargetX = playerTransform != null ? playerTransform.position.x + spawnDistanceAhead : spawnDistanceAhead;
        while (_nextSpawnX < initialTargetX)
        {
            SpawnChunk(_activeChunks.Count == 0);
        }
    }

    // Sinh một đoạn đường mới ở vị trí tiếp theo trên trục X
    private void SpawnChunk(bool isFirstChunk)
    {
        if (_validPrefabs.Count == 0) return;

        int prefabIndex = isFirstChunk ? 0 : Random.Range(0, _validPrefabs.Count);
        Vector3 spawnPosition = new Vector3(_nextSpawnX, roadPosY, 0f);

        GameObject chunk = GetChunkInstance(prefabIndex, spawnPosition);
        if (chunk != null)
        {
            _activeChunks.Add(chunk);
            _nextSpawnX += chunkWidth;
        }
    }

    // Lấy instance từ Pool hoặc tạo mới
    private GameObject GetChunkInstance(int prefabIndex, Vector3 position)
    {
        if (useObjectPooling && _chunkPool.ContainsKey(prefabIndex) && _chunkPool[prefabIndex].Count > 0)
        {
            GameObject pooledChunk = _chunkPool[prefabIndex].Dequeue();
            if (pooledChunk != null)
            {
                pooledChunk.transform.position = position;
                pooledChunk.SetActive(true);
                return pooledChunk;
            }
        }

        if (prefabIndex >= _validPrefabs.Count || _validPrefabs[prefabIndex] == null) return null;

        // Sinh trực tiếp ở World Space (không bị ảnh hưởng bởi transform của cha)
        GameObject newChunk = Instantiate(_validPrefabs[prefabIndex], position, Quaternion.identity);

        ChunkIdentifier identifier = newChunk.GetComponent<ChunkIdentifier>();
        if (identifier == null)
        {
            identifier = newChunk.AddComponent<ChunkIdentifier>();
        }
        identifier.PrefabIndex = prefabIndex;

        return newChunk;
    }

    // Thu hồi đoạn đường cũ vào Pool
    private void RecycleChunk(GameObject chunk)
    {
        if (chunk == null) return;

        if (useObjectPooling)
        {
            ChunkIdentifier identifier = chunk.GetComponent<ChunkIdentifier>();
            int prefabIndex = identifier != null ? identifier.PrefabIndex : 0;

            if (!_chunkPool.ContainsKey(prefabIndex))
            {
                _chunkPool[prefabIndex] = new Queue<GameObject>();
            }

            chunk.SetActive(false);
            _chunkPool[prefabIndex].Enqueue(chunk);
        }
        else
        {
            Destroy(chunk);
        }
    }

    // Xóa toàn bộ và giải phóng Pool
    public void ClearMap()
    {
        foreach (GameObject chunk in _activeChunks)
        {
            if (chunk != null) Destroy(chunk);
        }
        _activeChunks.Clear();

        foreach (var queue in _chunkPool.Values)
        {
            while (queue.Count > 0)
            {
                GameObject obj = queue.Dequeue();
                if (obj != null) Destroy(obj);
            }
        }
        _chunkPool.Clear();

        _nextSpawnX = 0f;
    }

    // Hỗ trợ chế độ xem trước (Preview) trong Editor
    public void GeneratePreviewMap()
    {
        ClearPreviewMap();

        if (chunkPrefabs == null || chunkPrefabs.Length == 0) return;

        float previewX = 0f;
        int count = 4;
        for (int i = 0; i < count; i++)
        {
            int index = i % chunkPrefabs.Length;
            if (chunkPrefabs[index] == null) continue;

            GameObject chunk = Instantiate(chunkPrefabs[index], new Vector3(previewX, roadPosY, 0f), Quaternion.identity);
            chunk.name = $"Preview_Chunk_{i}";
            previewX += chunkWidth;
        }
    }

    // Xóa các đoạn đường xem trước trong Editor
    public void ClearPreviewMap()
    {
        GameObject[] previewChunks = GameObject.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (GameObject go in previewChunks)
        {
            if (go.name.StartsWith("Preview_Chunk"))
            {
                DestroyImmediate(go);
            }
        }
    }
}