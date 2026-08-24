using UnityEngine;

// Tự động cuộn và nối tiếp hình nền Background vô tận theo xe/camera (Hiệu ứng Parallax Scrolling)
public class InfiniteParallaxBackground : MonoBehaviour
{
    [Header("Camera Reference")]
    // Camera cần bám theo (tự động tìm Main Camera nếu để trống)
    [SerializeField] private Camera targetCamera;

    [Header("Parallax Settings")]
    // Hệ số trôi cảnh nền:
    // 0 = Cảnh dính chặt theo Camera (đứng yên trên màn hình)
    // 0.3 - 0.5 = Cảnh ở xa trôi chậm hơn xe (Tạo chiều sâu 3D Parallax đẹp mắt)
    // 1.0 = Cảnh trôi cùng tốc độ xe/đoạn đường
    [Range(0f, 1f)]
    [SerializeField] private float parallaxEffect = 0.5f;

    [Header("Seamless Looping Elements")]
    // Danh sách các tấm ảnh nối đuôi nhau (nếu có nhiều tấm con)
    [SerializeField] private Transform[] backgroundLayers;

    // Chiều rộng của 1 bức ảnh (Unity Units)
    [SerializeField] private float segmentWidth = 17.74f;

    private float _lastCameraX;
    private float _startPosY;

    private void Start()
    {
        if (targetCamera == null) targetCamera = Camera.main;

        if (targetCamera != null)
        {
            _lastCameraX = targetCamera.transform.position.x;
        }

        _startPosY = transform.position.y;

        // Tự động tìm tất cả SpriteRenderer con nếu chưa gán mảng
        if (backgroundLayers == null || backgroundLayers.Length == 0)
        {
            int childCount = transform.childCount;
            if (childCount > 0)
            {
                backgroundLayers = new Transform[childCount];
                for (int i = 0; i < childCount; i++)
                {
                    backgroundLayers[i] = transform.GetChild(i);
                }
            }
        }

        // Tự động đo kích thước ảnh nếu có SpriteRenderer
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr == null && transform.childCount > 0) sr = transform.GetChild(0).GetComponent<SpriteRenderer>();
        if (sr != null && sr.sprite != null)
        {
            segmentWidth = sr.bounds.size.x;
        }
    }

    private void LateUpdate()
    {
        if (targetCamera == null) return;

        float cameraX = targetCamera.transform.position.x;
        float deltaCameraX = cameraX - _lastCameraX;
        _lastCameraX = cameraX;

        // 1. Di chuyển toàn bộ cụm Background theo hệ số Parallax
        // Với parallaxEffect: nếu bằng 1.0 thì đứng yên so với thế giới (trôi theo xe)
        // Nếu bằng 0.5 thì trôi chậm bằng 50%
        float moveDistance = deltaCameraX * (1f - parallaxEffect);
        transform.position += Vector3.right * moveDistance;

        // Giữ nguyên trục Y
        transform.position = new Vector3(transform.position.x, _startPosY, transform.position.z);

        // 2. Tái chế và luân chuyển các tấm ảnh con khi bị tụt lại phía sau Camera
        if (backgroundLayers != null && backgroundLayers.Length > 1 && segmentWidth > 0f)
        {
            float totalSpan = segmentWidth * backgroundLayers.Length;

            for (int i = 0; i < backgroundLayers.Length; i++)
            {
                Transform seg = backgroundLayers[i];
                // Nếu tấm ảnh này đã bị bỏ lại phía sau Camera quá 1 khoảng segmentWidth
                if (cameraX - seg.position.x > segmentWidth * 1.2f)
                {
                    // Tìm vị trí X của tấm ảnh nằm xa nhất phía trước
                    float maxRightX = seg.position.x;
                    for (int j = 0; j < backgroundLayers.Length; j++)
                    {
                        if (backgroundLayers[j].position.x > maxRightX)
                        {
                            maxRightX = backgroundLayers[j].position.x;
                        }
                    }

                    // Nhảy tấm ảnh cũ lên đầu hàng phía trước
                    seg.position = new Vector3(maxRightX + segmentWidth, seg.position.y, seg.position.z);
                }
            }
        }
    }
}
