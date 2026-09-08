using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

// Component phụ gắn vào từng Chunk để đánh dấu loại Prefab và chiều rộng thực tế khi thu hồi
public class ChunkIdentifier : MonoBehaviour
{
    public int PrefabIndex { get; set; }
    public float ActualWidth { get; set; } = 50f;
}

// Quản lý hệ thống bản đồ vô tận dạng cuộn ngang (Horizontal Endless Map / 3-Lane Runner).
// Đảm bảo đường sinh liên tục vô tận phía trước xe, TUYỆT ĐỐI KHÔNG BAO GIỜ BỊ MẤT ĐƯỜNG.
public class EndlessMapManager : MonoBehaviour
{
    [Header("Target & Prefabs")]
    // Transform của Player hoặc Xe cần theo dõi
    [SerializeField] private Transform playerTransform;

    // Danh sách các mẫu đoạn đường (Tilemap Chunk Prefabs trong Assets/PreFab/)
    [SerializeField] private GameObject[] chunkPrefabs;

    [Header("Chunk Configuration")]
    // Chiều rộng (trục X) của một đoạn đường (đơn vị Unity)
    [SerializeField] private float chunkWidth = 50f;

    // Tọa độ Y cố định của mặt đường
    [SerializeField] private float roadPosY = 2.5f;

    // Tầm nhìn phía trước cần luôn có sẵn đường (mét)
    [SerializeField] private float spawnDistanceAhead = 80f;

    // Khoảng cách phía sau Player mà đoạn đường cũ sẽ bị thu hồi (mét)
    [SerializeField] private float despawnDistanceBehind = 40f;

    [Header("Optimization")]
    // Bật chế độ Object Pooling để tái sử dụng Chunk (Tránh hủy nhầm Prefab)
    [SerializeField] private bool useObjectPooling = true;

    // Danh sách các chunk đang hiển thị trên Scene
    private readonly List<GameObject> _activeChunks = new List<GameObject>();

    // Bộ nhớ đệm (Pool) lưu trữ các chunk đã ẩn để tái sử dụng
    private readonly Dictionary<int, Queue<GameObject>> _chunkPool = new Dictionary<int, Queue<GameObject>>();

    // Danh sách các Prefab hợp lệ
    private readonly List<GameObject> _validPrefabs = new List<GameObject>();

    // Vị trí X tiếp theo để đặt đoạn đường mới
    private float _nextSpawnX = 0f;

    // Tạm dừng sinh / thu hồi đường (dùng khi khóa đấu trường Boss)
    private bool _isSpawningPaused = false;

    private void Awake()
    {
        // Luôn bật Object Pooling để bảo vệ Prefab không bao giờ bị Destroy mất
        useObjectPooling = true;
    }

    private void Start()
    {
        // 1. Tự động tìm Player nếu chưa được gán trong Inspector
        if (playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerTransform = player.transform;
            }
        }

        // 2. Lọc danh sách Prefab hợp lệ
        RefreshValidPrefabs();

        // 3. Khởi tạo bản đồ ban đầu
        InitializeMap();
    }

    private void Update()
    {
        if (playerTransform == null || _isSpawningPaused) return;

        // Kiểm tra lại nếu mảng Prefab bị trống thì tự động khôi phục
        if (_validPrefabs.Count == 0)
        {
            RefreshValidPrefabs();
        }

        // 1. Luôn đảm bảo phía trước Player có đủ đường nối tiếp nhau
        int safetyCounter = 0;
        float targetAheadX = playerTransform.position.x + spawnDistanceAhead;

        while (_nextSpawnX < targetAheadX && safetyCounter < 5)
        {
            safetyCounter++;
            SpawnChunk(false);
        }

        // 2. Thu hồi các đoạn đường cũ đã bị xe bỏ lại quá xa phía sau
        for (int i = _activeChunks.Count - 1; i >= 0; i--)
        {
            GameObject chunk = _activeChunks[i];
            if (chunk != null)
            {
                ChunkIdentifier id = chunk.GetComponent<ChunkIdentifier>();
                float width = id != null ? id.ActualWidth : chunkWidth;
                float chunkRightEdge = chunk.transform.position.x + width;

                if (playerTransform.position.x - chunkRightEdge > despawnDistanceBehind)
                {
                    RecycleChunk(chunk);
                    _activeChunks.RemoveAt(i);
                }
            }
            else
            {
                _activeChunks.RemoveAt(i);
            }
        }
    }

    // Lọc và bảo vệ danh sách Prefab
    private void RefreshValidPrefabs()
    {
        _validPrefabs.Clear();
        if (chunkPrefabs != null)
        {
            foreach (GameObject prefab in chunkPrefabs)
            {
                if (prefab != null) _validPrefabs.Add(prefab);
            }
        }

        // Nếu trong Inspector bị kéo nhầm hoặc trống, tự động tìm Chunk_Road trong Scene/Project
        if (_validPrefabs.Count == 0)
        {
            GameObject sceneChunk = GameObject.Find("Chunk_Road");
            if (sceneChunk != null)
            {
                _validPrefabs.Add(sceneChunk);
            }
        }
    }

    // Khởi tạo bản đồ ban đầu
    private void InitializeMap()
    {
        _activeChunks.Clear();
        _nextSpawnX = 0f;

        // Tìm tất cả các đoạn đường có sẵn trong Scene
        GameObject[] sceneObjects = GameObject.FindObjectsByType<GameObject>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        List<GameObject> existingRoads = new List<GameObject>();

        foreach (GameObject go in sceneObjects)
        {
            if (go.name.Contains("Chunk") || go.name.Contains("Road") || go.name.Contains("Preview"))
            {
                if (go.GetComponent<Grid>() != null || go.GetComponentInChildren<Tilemap>() != null)
                {
                    // Bỏ qua chính GameObject mẫu nếu nó là prefab gốc
                    if (go.scene.name != null)
                    {
                        existingRoads.Add(go);
                    }
                }
            }
        }

        if (existingRoads.Count > 0)
        {
            existingRoads.Sort((a, b) => a.transform.position.x.CompareTo(b.transform.position.x));

            // Tự động tìm Tilemap của đoạn đường đầu tiên để đồng bộ chính xác cao độ và điểm nối
            Tilemap firstTm = existingRoads[0].GetComponentInChildren<Tilemap>();
            if (firstTm != null)
            {
                roadPosY = firstTm.transform.position.y;
            }
            else
            {
                roadPosY = existingRoads[0].transform.position.y;
            }

            foreach (GameObject road in existingRoads)
            {
                _activeChunks.Add(road);

                ChunkIdentifier id = road.GetComponent<ChunkIdentifier>();
                if (id == null) id = road.AddComponent<ChunkIdentifier>();
                id.PrefabIndex = -1; // -1: Đường thủ công trong Scene, không đưa vào Pool tái sử dụng của chunk 50m

                float actualWidth = chunkWidth;
                Tilemap tm = road.GetComponentInChildren<Tilemap>();
                if (tm != null)
                {
                    tm.CompressBounds();
                    // Tính tọa độ mép phải tuyệt đối trong không gian World để nối tiếp đoạn mới KHÔNG BAO GIỜ HỞ KHE
                    Vector3 worldRightEdge = tm.CellToWorld(new Vector3Int(tm.cellBounds.xMax, 0, 0));
                    if (worldRightEdge.x > _nextSpawnX)
                    {
                        _nextSpawnX = worldRightEdge.x;
                    }

                    if (tm.cellBounds.size.x > 0)
                    {
                        actualWidth = tm.cellBounds.size.x;
                    }
                    // Cập nhật roadPosY theo Tilemap đang có
                    roadPosY = tm.transform.position.y;
                }
                else
                {
                    float endX = road.transform.position.x + actualWidth;
                    if (endX > _nextSpawnX)
                    {
                        _nextSpawnX = endX;
                    }
                }

                id.ActualWidth = actualWidth;
            }
        }

        // Sinh tiếp các đoạn đường đón đầu phía trước
        float initialTargetX = playerTransform != null ? playerTransform.position.x + spawnDistanceAhead : spawnDistanceAhead;
        int safetyCounter = 0;
        while (_nextSpawnX < initialTargetX && safetyCounter < 10)
        {
            safetyCounter++;
            SpawnChunk(_activeChunks.Count == 0);
        }
    }

    // Sinh một đoạn đường mới ở vị trí tiếp theo trên trục X
    private void SpawnChunk(bool isFirstChunk)
    {
        float currentSpawnX = _nextSpawnX;
        _nextSpawnX += chunkWidth;

        if (_validPrefabs.Count == 0) return;

        int prefabIndex = isFirstChunk ? 0 : Random.Range(0, _validPrefabs.Count);

        // Bù trừ độ lệch cục bộ của Tilemap con bên trong Prefab (nếu có) để mặt đường luôn khớp 100%
        float prefabTilemapLocalY = 0f;
        if (prefabIndex < _validPrefabs.Count && _validPrefabs[prefabIndex] != null)
        {
            Tilemap prefabTm = _validPrefabs[prefabIndex].GetComponentInChildren<Tilemap>();
            if (prefabTm != null)
            {
                prefabTilemapLocalY = prefabTm.transform.localPosition.y;
            }
        }

        Vector3 spawnPosition = new Vector3(currentSpawnX, roadPosY - prefabTilemapLocalY, 0f);

        GameObject chunk = GetChunkInstance(prefabIndex, spawnPosition);
        if (chunk != null)
        {
            _activeChunks.Add(chunk);
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

        GameObject newChunk = Instantiate(_validPrefabs[prefabIndex], position, Quaternion.identity);

        ChunkIdentifier identifier = newChunk.GetComponent<ChunkIdentifier>();
        if (identifier == null)
        {
            identifier = newChunk.AddComponent<ChunkIdentifier>();
        }
        identifier.PrefabIndex = prefabIndex;
        identifier.ActualWidth = chunkWidth;

        return newChunk;
    }

    // Thu hồi đoạn đường cũ vào Pool (TUYỆT ĐỐI KHÔNG HỦY NHẦM PREFAB GỐC)
    private void RecycleChunk(GameObject chunk)
    {
        if (chunk == null) return;

        // Nếu vô tình là chính Prefab gốc thì không được ẩn hay hủy
        if (_validPrefabs.Contains(chunk)) return;

        if (useObjectPooling)
        {
            ChunkIdentifier identifier = chunk.GetComponent<ChunkIdentifier>();
            int prefabIndex = identifier != null ? identifier.PrefabIndex : 0;

            // Nếu là đoạn đường thủ công vẽ sẵn trong Scene, chỉ ẩn đi chứ không đưa vào Pool chunk 50m
            if (prefabIndex < 0)
            {
                chunk.SetActive(false);
                return;
            }

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

    // Tạm dừng hoặc tiếp tục sinh / thu hồi đường khi khóa đấu trường
    public void SetSpawningPaused(bool paused)
    {
        _isSpawningPaused = paused;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Vector3 start = new Vector3(-20f, roadPosY, 0f);
        Vector3 end = new Vector3(200f, roadPosY, 0f);
        Gizmos.DrawLine(start, end);
    }
}