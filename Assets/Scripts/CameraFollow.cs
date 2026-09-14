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
    // Khóa trực tiếp Camera theo xe theo trục X (Khuyên dùng: BẬT để xe cố định 100% trên màn hình, triệt tiêu hoàn toàn giật rung)
    [SerializeField] private bool lockDirectToTarget = true;

    // Tốc độ bám theo của Camera nếu không khóa trực tiếp
    [SerializeField] private float smoothSpeed = 15f;

    // Bật Pixel Snapping (LƯU Ý: nếu bật sẽ làm camera nhảy giật từng 1/32 pixel, nên TẮT để trôi mượt mà)
    [SerializeField] private bool usePixelSnapping = false;
    [SerializeField] private float pixelsPerUnit = 32f;

    // Màu nền Camera tiệp màu cát để không bao giờ bị lộ đường đen
    [SerializeField] private Color backgroundColor = new Color(0.96f, 0.74f, 0.45f, 1f);

    [Header("Arena Lock Settings")]
    [SerializeField] private bool isArenaLocked = false;
    [SerializeField] private float minCamX = 0f;
    [SerializeField] private float maxCamX = 0f;

    private Camera _cam;
    private float _originalXOffset;

    private void Awake()
    {
        _originalXOffset = xOffset;
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

        // Nếu đang trong chế độ khóa đấu trường (Arena Lock), kẹp X lại
        if (isArenaLocked)
        {
            targetX = Mathf.Clamp(targetX, minCamX, maxCamX);
        }

        // Di chuyển tới vị trí X mục tiêu (Nếu khóa trực tiếp và không ở trong đấu trường thì bám 100% không delay)
        float currentX;
        if (lockDirectToTarget && !isArenaLocked)
        {
            currentX = targetX;
        }
        else
        {
            currentX = Mathf.Lerp(transform.position.x, targetX, smoothSpeed * Time.deltaTime);
        }

        // Khử xé hình sub-pixel nếu người dùng bật Pixel Snapping
        if (usePixelSnapping && pixelsPerUnit > 0f)
        {
            currentX = Mathf.Round(currentX * pixelsPerUnit) / pixelsPerUnit;
        }

        // Cập nhật vị trí Camera (X thay đổi theo xe, Y và Z giữ cố định)
        transform.position = new Vector3(currentX, fixedY, fixedZ);
    }

    // Hàm cho phép đổi đối tượng theo dõi lúc runtime nếu cần
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }

    // Khóa hoặc mở khóa Camera trong đấu trường Boss
    public void SetArenaLock(bool locked, float minX = 0f, float maxX = 0f, float customXOffset = 0f)
    {
        isArenaLocked = locked;
        minCamX = minX;
        maxCamX = maxX;
        xOffset = locked ? customXOffset : _originalXOffset;
    }
}