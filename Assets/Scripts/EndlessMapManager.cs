using System.Collections.Generic;
using UnityEngine;

// Quản lý hệ thống bản đồ vô tận dạng cuộn ngang (Horizontal Endless Map / 3-Lane Runner).
// Hỗ trợ xem trước (Preview) ngay trong Scene/Game view khi chưa ấn Play, và nhận diện các Chunk đặt sẵn trong Scene.
public class EndlessMapManager : MonoBehaviour
{
    [Header("Target & Prefabs")]
    // Transform của Player hoặc Xe cần theo dõi
    [SerializeField] private Transform playerTransform;

    // Danh sách các mẫu đoạn đường (Tilemap Chunk Prefabs) với chướng ngại vật/zombie khác nhau
    [SerializeField] private GameObject[] chunkPrefabs;

    [Header("Chunk Configuration")]
    // Chiều rộng (trục X) của một đoạn đường (đơn vị Unity)
    [SerializeField] private float chunkWidth = 19f;

    // Số lượng đoạn đường được tạo sẵn khi bắt đầu game
    [SerializeField] private int initialChunksCount = 4;

    // Khoảng cách phía sau Player mà đoạn đường cũ sẽ bị thu hồi
    [SerializeField] private float despawnDistanceBehind = 35f;

    [Header("Optimization")]
    // Bật chế độ Object Pooling để tái sử dụng Chunk thay vì Instantiate/Destroy liên tục
    [SerializeField] private bool useObjectPooling = true;

    // Danh sách các chunk đang hiển thị trên Scene
    private readonly List<GameObject> _activeChunks = new List<GameObject>();

    // Bộ nhớ đệm (Pool) lưu trữ các chunk đã ẩn để tái sử dụng
    private readonly Dictionary<int, Queue<GameObject>> _chunkPool = new Dictionary<int, Queue<GameObject>>();

    // Vị trí X tiếp theo để đặt đoạn đường mới
    private float _nextSpawnX = 0f;

    private void Start()
    {
        // Tự động tìm Player nếu chưa được gán trong Inspector
        if (playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerTransform = player.transform;
            }
            else
            {
                Debug.LogWarning("EndlessMapManager: Chưa gán PlayerTransform và không tìm thấy GameObject có Tag 'Player'!");
            }
        }

        // Khởi tạo các đoạn đường ban đầu
        InitializeMap();
    }

    private void Update()
    {
        if (playerTransform == null || _activeChunks.Count == 0) return;

        // Kiểm tra xem đoạn đường cũ nhất đã vượt qua phía sau người chơi chưa
        GameObject oldestChunk = _activeChunks[0];
        if (playerTransform.position.x - oldestChunk.transform.position.x > despawnDistanceBehind)
        {
            RecycleChunk(oldestChunk);
            _activeChunks.RemoveAt(0);

            // Sinh tiếp một đoạn đường mới ở phía trước
            SpawnChunk(false);
        }
    }

    // Khởi tạo bản đồ: Tận dụng các chunk đã đặt sẵn trong Scene (nếu có) và sinh thêm cho đủ số lượng
    private void InitializeMap()
    {
        _activeChunks.Clear();
        _nextSpawnX = 0f;

        // 1. Quét xem đã có Chunk nào được đặt sẵn làm con của EndlessMapManager trong Scene chưa
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            if (child.gameObject.activeSelf)
            {
                _activeChunks.Add(child.gameObject);
                
                // Đảm bảo có ChunkIdentifier
                ChunkIdentifier identifier = child.GetComponent<ChunkIdentifier>();
                if (identifier == null)
                {
                    identifier = child.gameObject.AddComponent<ChunkIdentifier>();
                    identifier.PrefabIndex = 0;
                }

                // Cập nhật vị trí X tiếp theo dựa trên chunk xa nhất
                float chunkEndX = child.position.x + chunkWidth;
                if (chunkEndX > _nextSpawnX)
                {
                    _nextSpawnX = chunkEndX;
                }
            }
        }

        // Sắp xếp các chunk đặt sẵn theo thứ tự trục X từ trái sang phải
        _activeChunks.Sort((a, b) => a.transform.position.x.CompareTo(b.transform.position.x));

        // 2. Sinh thêm các chunk tiếp theo nếu số lượng đặt sẵn chưa đủ
        int neededChunks = initialChunksCount - _activeChunks.Count;
        for (int i = 0; i < neededChunks; i++)
        {
            SpawnChunk(_activeChunks.Count == 0);
        }
    }

    // Sinh một đoạn đường mới ở vị trí tiếp theo trên trục X
    private void SpawnChunk(bool isFirstChunk)
    {
        if (chunkPrefabs == null || chunkPrefabs.Length == 0) return;

        // Chọn index của Prefab (đoạn đầu chọn mẫu 0, các đoạn sau chọn ngẫu nhiên)
        int prefabIndex = isFirstChunk ? 0 : Random.Range(0, chunkPrefabs.Length);
        Vector3 spawnPosition = new Vector3(_nextSpawnX, 0f, 0f);

        GameObject chunk = GetChunkInstance(prefabIndex, spawnPosition);
        _activeChunks.Add(chunk);

        // Cập nhật vị trí X cho đoạn đường kế tiếp
        _nextSpawnX += chunkWidth;
    }

    // Lấy một instance của chunk từ Pool (nếu có) hoặc Instantiate mới
    private GameObject GetChunkInstance(int prefabIndex, Vector3 position)
    {
        if (useObjectPooling && _chunkPool.ContainsKey(prefabIndex) && _chunkPool[prefabIndex].Count > 0)
        {
            GameObject pooledChunk = _chunkPool[prefabIndex].Dequeue();
            pooledChunk.transform.position = position;
            pooledChunk.SetActive(true);
            return pooledChunk;
        }

        // Nếu không có trong Pool hoặc không dùng Pooling thì Instantiate mới
        GameObject newChunk = Instantiate(chunkPrefabs[prefabIndex], position, Quaternion.identity, transform);
        
        // Gắn kèm thông tin PrefabIndex để khi thu hồi biết thuộc pool nào
        ChunkIdentifier identifier = newChunk.GetComponent<ChunkIdentifier>();
        if (identifier == null)
        {
            identifier = newChunk.AddComponent<ChunkIdentifier>();
        }
        identifier.PrefabIndex = prefabIndex;

        return newChunk;
    }

    // Thu hồi hoặc xóa chunk khi người chơi đã chạy qua
    private void RecycleChunk(GameObject chunk)
    {
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

    // ==========================================
    // CÔNG CỤ XEM TRƯỚC BẢN ĐỒ KHI CHƯA ẤN PLAY (EDITOR PREVIEW)
    // ==========================================

#if UNITY_EDITOR
    // Tạo sẵn các Chunk nối tiếp nhau trên Scene để nhìn thấy và vẽ thử Tilemap
    [ContextMenu("Tạo Bản Đồ Xem Trước (Preview Map)")]
    public void GeneratePreviewMap()
    {
        ClearPreviewMap();

        if (chunkPrefabs == null || chunkPrefabs.Length == 0)
        {
            Debug.LogWarning("EndlessMapManager: Chưa có Chunk Prefab nào để tạo Preview!");
            return;
        }

        float currentX = 0f;
        for (int i = 0; i < initialChunksCount; i++)
        {
            int prefabIndex = i % chunkPrefabs.Length;
            if (chunkPrefabs[prefabIndex] != null)
            {
                GameObject chunk = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(chunkPrefabs[prefabIndex], transform);
                chunk.transform.position = new Vector3(currentX, 0f, 0f);

                ChunkIdentifier id = chunk.GetComponent<ChunkIdentifier>();
                if (id == null) id = chunk.AddComponent<ChunkIdentifier>();
                id.PrefabIndex = prefabIndex;
            }
            currentX += chunkWidth;
        }

        Debug.Log($"EndlessMapManager: Đã tạo {initialChunksCount} đoạn Preview trong Scene!");
    }

    // Xóa các Chunk xem trước trong Scene
    [ContextMenu("Xóa Bản Đồ Xem Trước (Clear Preview)")]
    public void ClearPreviewMap()
    {
        while (transform.childCount > 0)
        {
            DestroyImmediate(transform.GetChild(0).gameObject);
        }
    }
#endif

    // Vẽ đường biên Gizmos trong Scene view để dễ căn chỉnh độ dài Chunk
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Vector3 center = transform.position + new Vector3(_nextSpawnX - (chunkWidth / 2f), 0f, 0f);
        Gizmos.DrawWireCube(center, new Vector3(chunkWidth, 6f, 0.1f));
    }
}

// Component phụ trợ để lưu trữ Index của Prefab khi sử dụng Object Pooling
public class ChunkIdentifier : MonoBehaviour
{
    public int PrefabIndex { get; set; }
}