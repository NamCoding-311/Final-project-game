using UnityEngine;

// Điều khiển Camera cuộn ngang theo nhân vật cho thể loại Endless Runner 3 làn
// Hỗ trợ Pixel Snapping và tự động đổi màu nền Camera để triệt tiêu hoàn toàn lỗi nứt/xé hình Tilemap
public class CameraFollow : MonoBehaviour
{
    [Header("Target")]
    // Transform của Player/Xe cần đi theo
    [SerializeField] private Transform target;

    [Header("Offsets & Position")]
    // Độ lệch trục X (cho xe nằm hơi lệch về phía trái màn hình để người chơi nhìn thấy đường phía trước)
    [SerializeField] private float xOffset = 5f;

    // Vị trí Y cố định của Camera (thường đặt ở tâm làn giữa: 0)
    [SerializeField] private float fixedY = 0f;

    // Khoảng cách Z của Camera (mặc định -10 trong game 2D)
    [SerializeField] private float fixedZ = -10f;

    [Header("Smoothing & Anti-Tearing")]
    // Tốc độ bám theo của Camera (càng lớn càng bám sát)
    [SerializeField] private float smoothSpeed = 8f;

    // Bật Pixel Snapping để tránh xé hình ở độ phân giải pixel art (PPU = 32)
    [SerializeField] private bool usePixelSnapping = true;
    [SerializeField] private float pixelsPerUnit = 32f;

    // Màu nền Camera tiệp màu cát để không bao giờ bị lộ đường đen
    [SerializeField] private Color backgroundColor = new Color(0.96f, 0.74f, 0.45f, 1f);

    private Camera _cam;

    private void Awake()
    {
        _cam = GetComponent<Camera>();
        if (_cam == null) _cam = Camera.main;

        // Tự động set màu nền Camera tiệp với màu map
        if (_cam != null)
        {
            _cam.clearFlags = CameraClearFlags.SolidColor;
            _cam.backgroundColor = backgroundColor;
        }
    }

    private void Start()
    {
        // Tự động tìm Player nếu chưa được gán trong Inspector
        if (target == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                target = player.transform;
            }
            else
            {
                Debug.LogWarning("CameraFollow: Chưa gán Target và không tìm thấy GameObject có Tag 'Player'!");
            }
        }
    }

    private void LateUpdate()
    {
        if (target == null) return;

        // Vị trí X mục tiêu mà Camera cần hướng tới
        float targetX = target.position.x + xOffset;

        // Di chuyển mượt mà tới vị trí X mục tiêu
        float currentX = Mathf.Lerp(transform.position.x, targetX, smoothSpeed * Time.deltaTime);

        // Khử xé hình sub-pixel nếu bật Pixel Snapping
        if (usePixelSnapping && pixelsPerUnit > 0f)
        {
            currentX = Mathf.Round(currentX * pixelsPerUnit) / pixelsPerUnit;
        }

        // Cập nhật vị trí Camera (X thay đổi theo pixel snap, Y và Z giữ cố định)
        transform.position = new Vector3(currentX, fixedY, fixedZ);
    }

    // Hàm cho phép đổi đối tượng theo dõi lúc runtime nếu cần
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }
}